﻿#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Forge.Core.Airship.Data;
using Forge.Core.Airship.Export;
using Forge.Framework.Draw;
using MonoGameUtility;

#endregion

namespace Forge.Core.ObjectEditor{
    public class HullEnvironment : IDisposable{
        #region Delegates

        public delegate void CurDeckChanged(int oldDeck, int newDeck);

        #endregion

        public readonly Vector3 CenterPoint;
        public readonly float DeckHeight;
        public readonly DeckSectionContainer DeckSectionContainer;
        public readonly HullSectionContainer HullSectionContainer;
        public readonly int NumDecks;
        public readonly ObjectBuffer<WallSegmentIdentifier>[] WallBuffers;
        public readonly List<WallSegmentIdentifier>[] WallIdentifiers;
        public readonly float WallResolution;
        int _curDeck;
        bool _disposed;

        public HullEnvironment(AirshipPackager.AirshipSerializationStruct data){
            NumDecks = data.ModelAttributes.NumDecks;
            VisibleDecks = NumDecks;
            DeckSectionContainer = new DeckSectionContainer(data.DeckSections);
            DeckHeight = data.ModelAttributes.DeckHeight;
            WallResolution = 0.5f;
            CenterPoint = data.ModelAttributes.Centroid;
            HullSectionContainer = new HullSectionContainer(data.HullSections);

            WallBuffers = new ObjectBuffer<WallSegmentIdentifier>[NumDecks];
            for (int i = 0; i < WallBuffers.Count(); i++){
                int potentialWalls = DeckSectionContainer.DeckVertexesByDeck[i].Count()*2;
                WallBuffers[i] = new ObjectBuffer<WallSegmentIdentifier>(potentialWalls, 10, 20, 30, "Config/Shaders/Airship_InternalWalls.config");
            }

            WallIdentifiers = new List<WallSegmentIdentifier>[NumDecks];
            for (int i = 0; i < WallIdentifiers.Length; i++){
                WallIdentifiers[i] = new List<WallSegmentIdentifier>();
            }
            CurDeck = 0;
        }

        public ObjectBuffer<WallSegmentIdentifier> CurWallBuffer { get; private set; }
        public List<WallSegmentIdentifier> CurWallIdentifiers { get; private set; }

        public int VisibleDecks { get; private set; }

        public int CurDeck{
            get { return _curDeck; }
            set{
                //higher curdeck means a lower deck is displayed
                //low curdeck means higher deck displayed
                //highest deck is 0

                if (value < 0)
                    value = 0;
                if (value >= NumDecks)
                    value = NumDecks - 1;

                int oldDeck = _curDeck;
                int diff = -(value - _curDeck);
                VisibleDecks += diff;
                _curDeck = value;

                HullSectionContainer.SetTopVisibleDeck(_curDeck);
                DeckSectionContainer.SetTopVisibleDeck(_curDeck);

                CurWallBuffer = WallBuffers[_curDeck];
                CurWallIdentifiers = WallIdentifiers[_curDeck];

                if (OnCurDeckChange != null){
                    OnCurDeckChange.Invoke(oldDeck, _curDeck);
                }
            }
        }

        #region IDisposable Members

        public void Dispose(){
            Debug.Assert(!_disposed);

            foreach (var buffer in WallBuffers){
                buffer.Dispose();
            }
            DeckSectionContainer.Dispose();
            HullSectionContainer.Dispose();
            _disposed = true;
        }

        #endregion

        public event CurDeckChanged OnCurDeckChange;

        public void MoveUpOneDeck(){
            CurDeck--;
        }

        public void MoveDownOneDeck(){
            CurDeck++;
        }

        ~HullEnvironment(){
            Debug.Assert(_disposed);
        }
    }
}