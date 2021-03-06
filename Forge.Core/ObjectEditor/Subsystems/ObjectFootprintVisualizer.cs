﻿#region

using System;
using Forge.Core.Airship.Data;
using Forge.Core.GameObjects;
using Forge.Core.Util;
using Forge.Framework.Draw;
using Microsoft.Xna.Framework.Graphics;
using MonoGameUtility;

#endregion

namespace Forge.Core.ObjectEditor.Subsystems{
    public class ObjectFootprintVisualizer : IDisposable{
        const float _deckHeight = 2.13f;
        readonly ObjectBuffer<GameObject>[] _accessBuffers;
        readonly ObjectBuffer<GameObject>[] _footprintBuffers;
        readonly int _numDecks;
        int _curDeck;

        public ObjectFootprintVisualizer(GameObjectEnvironment gameObjEnv, HullEnvironment hullEnv){
            _footprintBuffers = new ObjectBuffer<GameObject>[hullEnv.NumDecks];
            _accessBuffers = new ObjectBuffer<GameObject>[hullEnv.NumDecks];
            _numDecks = hullEnv.NumDecks;

            for (int i = 0; i < hullEnv.NumDecks; i++){
                _footprintBuffers[i] = new ObjectBuffer<GameObject>(300, 2, 4, 6, "Config/Shaders/ObjectPostPlacementFootprint.config");
                _accessBuffers[i] = new ObjectBuffer<GameObject>(300, 2, 4, 6, "Config/Shaders/ObjectPostPlacementAccess.config");
            }
            hullEnv.OnCurDeckChange += OnVisibleDeckChange;

            gameObjEnv.AddOnObjectPlacement(OnObjectAdded);
            gameObjEnv.AddOnObjectRemove(OnObjectRemoved);
        }

        #region IDisposable Members

        public void Dispose(){
            foreach (var buffer in _footprintBuffers){
                buffer.Dispose();
            }
            foreach (var buffer in _accessBuffers){
                buffer.Dispose();
            }
        }

        #endregion

        void OnObjectAdded(GameObject obj){
            var dims = obj.Type.Attribute<XZPoint>(GameObjectAttr.Dimensions);
            float length = dims.X/2f;
            float width = dims.Z/2f;
            VertexPositionNormalTexture[] verts;
            int[] inds;

            var vertOffset = new Vector3(0, 0.01f, 0);
            MeshHelper.GenerateFlatQuad(out verts, out inds, obj.ModelspacePosition + vertOffset, length, width);
            _footprintBuffers[obj.Deck].AddObject(obj, inds, verts);

            var accessArea = obj.Type.Attribute<XZRectangle>(GameObjectAttr.InteractionArea);
            var accessAreaOffset = new Vector3(accessArea.X/2f, 0, accessArea.Z/2f) + obj.ModelspacePosition;
            vertOffset = new Vector3(0, 0.02f, 0);
            MeshHelper.GenerateFlatQuad(out verts, out inds, accessAreaOffset + vertOffset, accessArea.Width/2f, accessArea.Length/2f);
            var orientation = obj.Type.Attribute<Quadrant.Direction>(GameObjectAttr.InteractionOrientation);
            MeshHelper.GenerateRotatedQuadTexcoords(orientation, verts);
            _accessBuffers[obj.Deck].AddObject(obj, inds, verts);

            if (obj.Type.Attribute<bool>(GameObjectAttr.HasMultifloorAABB) && _curDeck != 0){
                var deckOffset = new Vector3(0, _deckHeight, 0);
                MeshHelper.GenerateFlatQuad(out verts, out inds, obj.ModelspacePosition + vertOffset + deckOffset, length, width);
                _footprintBuffers[obj.Deck - 1].AddObject(obj, inds, verts);
            }
            if (obj.Type.Attribute<bool>(GameObjectAttr.IsMultifloorInteractable) && _curDeck != 0){
                var deckOffset = new Vector3(0, _deckHeight, 0);
                MeshHelper.GenerateFlatQuad(out verts, out inds, accessAreaOffset + vertOffset + deckOffset, accessArea.Width/2f, accessArea.Length/2f);
                MeshHelper.GenerateRotatedQuadTexcoords(orientation, verts);
                _accessBuffers[obj.Deck - 1].AddObject(obj, inds, verts);
            }
        }


        void OnObjectRemoved(GameObject obj){
            throw new NotImplementedException();
        }

        void OnVisibleDeckChange(int old, int newDeck){
            foreach (var buffer in _footprintBuffers){
                buffer.Enabled = false;
            }
            foreach (var buffer in _accessBuffers){
                buffer.Enabled = false;
            }
            for (int i = _numDecks - 1; i >= newDeck; i--){
                _footprintBuffers[i].Enabled = true;
                _accessBuffers[i].Enabled = true;
            }
            _curDeck = newDeck;
        }
    }
}