﻿#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Forge.Core.Airship.Data;
using Forge.Framework.Draw;
using Forge.Framework.Resources;
using Microsoft.Xna.Framework.Graphics;
using MonoGameUtility;

#endregion

namespace Forge.Core.ObjectEditor{
    /// <summary>
    /// provides an interface through which objects can be added and removed from the airship.
    /// </summary>
    public class GameObjectEnvironment : IDisposable{
        #region SideEffect enum

        public enum SideEffect{
            None,
            CutsIntoCeiling,
            CutsIntoStarboardHull,
            CutsIntoPortHull
        }

        #endregion

        const string _objectModelShader = "Config/Shaders/TintedModel.config";
        const int _maxObjectsPerLayer = 100;

        /// <summary>
        /// used by the wall-construction component to make sure none of the constructed walls bisect placed objects
        /// </summary>
        public readonly Dictionary<ObjectIdentifier, XZPoint>[] ObjectFootprints;

        readonly DeckSectionContainer _deckSectionContainer;

        /// <summary>
        /// Represents the limits of the silhouette that make up the outline of the airship deck.
        /// This is used to prevent objects from being placed in the "rectangle" that makes up the
        /// occupationGrid while being outside the actual limits of  the hull.
        /// </summary>
        readonly int[][] _gridLimitMax;

        readonly int[][] _gridLimitMin;

        /// <summary>
        /// These offsets are used to convert from model space to grid space.
        /// </summary>
        readonly XZPoint _gridOffset;

        readonly HullEnvironment _hullEnvironment;

        readonly ObjectModelBuffer<ObjectIdentifier>[] _objectModelBuffer;

        /// <summary>
        /// used to keep track of side effects such as removing deck plates or removing hull sections
        /// </summary>
        readonly List<GameObject>[] _objectSideEffects;

        /// <summary>
        /// Tables of booleans that represent whether the "area" the table maps to is occupied
        /// by an object or not. In order to index an occupation grid, the model space value of
        /// area to be queried must be converted to grid space, which can be obtained using
        /// ConvertToGridspace()
        /// </summary>
        readonly bool[][,] _occupationGrids;

        public GameObjectEnvironment(HullEnvironment hullEnv){
            _hullEnvironment = hullEnv;
            _deckSectionContainer = hullEnv.DeckSectionContainer;
            _objectSideEffects = new List<GameObject>[hullEnv.NumDecks];
            for (int i = 0; i < hullEnv.NumDecks; i++){
                _objectSideEffects[i] = new List<GameObject>();
            }
            _occupationGrids = new bool[hullEnv.NumDecks][,];

            var vertexes = hullEnv.DeckSectionContainer.DeckVertexesByDeck;

            _gridOffset = SetupObjectOccupationGrids(vertexes);
            CalculateGridLimits(vertexes, out _gridLimitMax, out _gridLimitMin);

            _hullEnvironment.OnCurDeckChange += OnVisibleDeckChange;

            _objectModelBuffer = new ObjectModelBuffer<ObjectIdentifier>[hullEnv.NumDecks];
            ObjectFootprints = new Dictionary<ObjectIdentifier, XZPoint>[hullEnv.NumDecks];
            for (int i = 0; i < hullEnv.NumDecks; i++){
                _objectModelBuffer[i] = new ObjectModelBuffer<ObjectIdentifier>(_maxObjectsPerLayer, _objectModelShader);
                ObjectFootprints[i] = new Dictionary<ObjectIdentifier, XZPoint>(_maxObjectsPerLayer);
            }

            OnVisibleDeckChange(0, 0);
        }

        public InternalWallEnvironment InternalWallEnvironment { private get; set; }

        #region IDisposable Members

        public void Dispose(){
            foreach (var buffer in _objectModelBuffer){
                buffer.Dispose();
            }
        }

        #endregion

        OccupationGridPos ConvertToGridspace(Vector3 modelSpacePos){
            modelSpacePos *= 2;
            int gridX = (int) (_gridOffset.X + modelSpacePos.X);
            int gridZ = (int) (_gridOffset.Z + modelSpacePos.Z);
            return new OccupationGridPos(gridX, gridZ);
        }

        XZPoint SetupObjectOccupationGrids(List<Vector3>[] vertexes){
            var layerVerts = vertexes[0];
            float maxX = float.MinValue;
            float maxZ = float.MinValue;
            float minX = float.MaxValue;

            foreach (var vert in layerVerts){
                if (vert.X > maxX)
                    maxX = vert.X;
                if (vert.Z > maxZ)
                    maxZ = vert.Z;
                if (vert.X < minX)
                    minX = vert.X;
            }

            var layerLength = (int) ((maxX - minX)*2);
            var layerWidth = (int) ((maxZ)*4);

            for (int i = 0; i < vertexes.Length; i++){
                var grid = new bool[layerLength + 1,layerWidth];
                _occupationGrids[i] = grid;
            }

            var ret = new XZPoint(-(int) (minX*2) + 1, (int) (maxZ*2));
            return ret;
        }

        void CalculateGridLimits(List<Vector3>[] vertexes, out int[][] gridLimitMax, out int[][] gridLimitMin){
            gridLimitMax = new int[vertexes.Length][];
            gridLimitMin = new int[vertexes.Length][];

            float minX = vertexes[0].Min(v => v.X);
            float maxX = vertexes[0].Max(v => v.X);
            for (int deck = 0; deck < vertexes.Length; deck++){
                var layerVerts = vertexes[deck];

                var vertsByRow =
                    from vert in layerVerts
                    where vert.Z >= 0
                    group vert by vert.X;

                var sortedVertsByRow = (
                    from pairing in vertsByRow
                    orderby pairing.Key ascending
                    select pairing
                    ).ToList();

                int southPadding = (int) ((sortedVertsByRow[0].Key - minX)*2);
                int northPadding = (int) ((maxX - sortedVertsByRow.Last().Key)*2);
                int length = southPadding + northPadding + sortedVertsByRow.Count;

                var layerLimitMin = new int[length];
                var layerLimitMax = new int[length];

                var rowArrayMax = _occupationGrids[deck].GetLength(1);
                //fill padding area
                for (int row = 0; row < southPadding; row++){
                    layerLimitMin[row] = int.MaxValue;
                    layerLimitMax[row] = int.MinValue;
                }
                for (int row = southPadding + sortedVertsByRow.Count; row < length; row++){
                    layerLimitMin[row] = int.MaxValue;
                    layerLimitMax[row] = int.MinValue;
                }

                for (int row = southPadding; row < southPadding + sortedVertsByRow.Count; row++){
                    float max = sortedVertsByRow[row - southPadding].Max(v => v.Z);
                    var converted = ConvertToGridspace(new Vector3(0, 0, -max)).Z;
                    layerLimitMin[row] = converted;
                    layerLimitMax[row] = rowArrayMax - converted;
                }
                gridLimitMax[deck] = layerLimitMax;
                gridLimitMin[deck] = layerLimitMin;
            }
        }

        void SetOccupationGridState(OccupationGridPos origin, XZPoint dims, int deck, bool value){
            var occupationGrid = _occupationGrids[deck];
            for (int x = origin.X; x < origin.X + dims.X; x++){
                for (int z = origin.Z; z < origin.Z + dims.Z; z++){
                    occupationGrid[x, z] = value;
                }
            }
        }

        void ModifyDeckPlates(OccupationGridPos origin, XZPoint dims, int deck, bool value){
            var buffer = _deckSectionContainer.DeckBufferByDeck[deck];
            //convert origin point so z axis bisects the ship instead of being left justified to it
            origin.Z -= _gridOffset.Z;

            for (int x = origin.X; x < origin.X + dims.X; x++){
                for (int z = origin.Z; z < origin.Z + dims.Z; z++){
                    var identifier = new DeckPlateIdentifier(new Point(x, z), deck);
                    bool b;
                    if (value){
                        b = buffer.EnableObject(identifier);
                    }
                    else{
                        b = buffer.DisableObject(identifier);
                    }
                    Debug.Assert(b);
                }
            }
        }

        void ApplyObjectSideEffect(GameObject sideEffect){
            if (sideEffect.SideEffect == SideEffect.CutsIntoCeiling){
                int deck = sideEffect.Identifier.Deck;
                if (deck != 0){
                    SetOccupationGridState(sideEffect.GridPosition, sideEffect.GridDimensions, sideEffect.Identifier.Deck - 1, true);
                    ModifyDeckPlates(sideEffect.GridPosition, sideEffect.GridDimensions, deck - 1, false);
                }
            }
            if (sideEffect.SideEffect == SideEffect.CutsIntoPortHull){
                //throw new NotImplementedException();
            }
            if (sideEffect.SideEffect == SideEffect.CutsIntoStarboardHull){
                throw new NotImplementedException();
            }
        }

        void RemoveObjectSideEffect(){
            throw new NotImplementedException();
        }

        void OnVisibleDeckChange(int old, int newDeck){
            foreach (var buffer in _objectModelBuffer){
                buffer.Enabled = false;
            }
            for (int i = _hullEnvironment.NumDecks - 1; i >= newDeck; i--){
                _objectModelBuffer[i].Enabled = true;
            }
        }

        #region public interfaces

        /// <summary>
        /// 
        /// </summary>
        /// <param name="position">The model space position of the object to be placed</param>
        /// <param name="gridDimensions">The unit-grid dimensions of the object. (1,1) cooresponds to a size of (0.5, 0.5) meters.</param>
        /// <param name="deck"></param>
        /// <param name="placementSideEffects"> </param>
        public bool IsObjectPlacementValid(Vector3 position, XZPoint gridDimensions, int deck, SideEffect placementSideEffects){
            var gridPosition = ConvertToGridspace(position);
            var gridLimitMax = _gridLimitMax[deck];
            var gridLimitMin = _gridLimitMin[deck];
            var occupationGrid = _occupationGrids[deck];
            for (int x = gridPosition.X; x < gridDimensions.X + gridPosition.X; x++){
                if (x < 0 || x >= gridLimitMax.Length){
                    return false;
                }
                for (int z = gridPosition.Z; z < gridDimensions.Z + gridPosition.Z; z++){
                    if (z < gridLimitMin[x] || z >= gridLimitMax[x]){
                        return false;
                    }
                    //confirms there is no object in the section of grid
                    if (occupationGrid[x, z]){
                        return false;
                    }
                }
            }
            if (placementSideEffects == SideEffect.CutsIntoCeiling){
                if (deck != 0){
                    if (!IsObjectPlacementValid(position, gridDimensions, deck - 1, SideEffect.None))
                        return false;
                }
            }

            if (!InternalWallEnvironment.IsObjectPlacementValid(position, gridDimensions, deck)){
                return false;
            }
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="modelName"></param>
        /// <param name="position">Model space position</param>
        /// <param name="dimensions">Dimensions of the object in grid-space units </param>
        /// <param name="deck"></param>
        /// <param name="transform"> </param>
        /// <param name="sideEffect"> </param>
        /// <returns></returns>
        public ObjectIdentifier AddObject(
            string modelName,
            Vector3 position,
            XZPoint dimensions,
            int deck,
            Matrix? transform = null,
            SideEffect sideEffect = SideEffect.None){
            var identifier = new ObjectIdentifier(position, deck);

            Matrix posTransform = Matrix.CreateTranslation(position);
            var model = Resource.LoadContent<Model>(modelName);
            if (transform != null){
                posTransform = (Matrix)transform * posTransform;
            }
            _objectModelBuffer[deck].AddObject(identifier, model, posTransform);

            var gridPos = ConvertToGridspace(position);
            SetOccupationGridState(gridPos, dimensions, deck, true);
            var objSideEffect = new GameObject
                (
                identifier,
                dimensions,
                gridPos,
                sideEffect
                );
            _objectSideEffects[deck].Add(objSideEffect);
            ApplyObjectSideEffect(objSideEffect);
            ObjectFootprints[deck].Add(identifier, dimensions);

            return identifier;
        }

        public void RemoveObject(ObjectIdentifier obj){
            throw new NotImplementedException();
        }

        #endregion

        #region Nested type: GameObject

        struct GameObject : IEquatable<ObjectIdentifier>{
            public readonly XZPoint GridDimensions;
            public readonly OccupationGridPos GridPosition;
            public readonly ObjectIdentifier Identifier;
            public readonly SideEffect SideEffect;

            public GameObject(ObjectIdentifier identifier, XZPoint gridDimensions, OccupationGridPos gridPosition, SideEffect sideEffect){
                Identifier = identifier;
                GridDimensions = gridDimensions;
                GridPosition = gridPosition;
                SideEffect = sideEffect;
            }

            #region IEquatable<ObjectIdentifier> Members

            public bool Equals(ObjectIdentifier other){
                return Identifier == other;
            }

            #endregion
        }

        #endregion

        #region Nested type: OccupationGridPos

        /// <summary>
        /// Point pseudo-class used for type richness to prevent errors with the conversions common to this class.
        /// </summary>
        struct OccupationGridPos{
            public readonly int X;
            public int Z;

            public OccupationGridPos(int x, int z){
                X = x;
                Z = z;
            }

            public static OccupationGridPos operator +(OccupationGridPos value1, XZPoint value2){
                return new OccupationGridPos(value2.X + value1.X, value2.Z + value1.Z);
            }

            public static OccupationGridPos operator -(OccupationGridPos value1, XZPoint value2){
                return new OccupationGridPos(value1.X - value2.X, value1.Z - value2.Z);
            }

            public override string ToString(){
                return string.Format("{{X:{0} Z:{1}}}", X, Z);
            }
        }

        #endregion
    }
}