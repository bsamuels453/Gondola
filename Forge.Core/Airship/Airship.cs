﻿#define ENABLE_DAMAGEMESH

#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Forge.Core.Airship.Controllers;
using Forge.Core.Airship.Controllers.AutoPilot;
using Forge.Core.Airship.Data;
using Forge.Core.Physics;
using Forge.Framework;
using MonoGameUtility;

#endregion

namespace Forge.Core.Airship{
    public class Airship : IDisposable{
        public readonly int FactionId;
        public readonly int Uid;
        readonly Battlefield _battlefield;
        public readonly AirshipController Controller;
        readonly List<Hardpoint> _hardPoints;
#if ENABLE_DAMAGEMESH
        readonly HullIntegrityMesh _hullIntegrityMesh;
#endif
        bool _disposed;

        public Airship(
            ModelAttributes airshipModel,
            DeckSectionContainer deckSectionContainer,
            HullSectionContainer hullSectionContainer,
            AirshipStateData stateData,
            Battlefield battlefield
            ){
            var sw = new Stopwatch();
            sw.Start();
            ModelAttributes = airshipModel;
            HullSectionContainer = hullSectionContainer;
            DeckSectionContainer = deckSectionContainer;

            _battlefield = battlefield;

            _hardPoints = new List<Hardpoint>();
            var emitter = new ProjectileEmitter("Config/Projectiles/TestShot.config", 10000, 0, _battlefield.ProjectileEngine);
            _hardPoints.Add(new Hardpoint(new Vector3(5, 0, 0), new Vector3(1, 0, 0), emitter));

            FactionId = stateData.FactionId;
            Uid = stateData.AirshipId;

            switch (stateData.ControllerType){
                case AirshipControllerType.AI:
                    Controller = new AIAirshipController
                        (
                        ModelAttributes,
                        stateData,
                        _hardPoints,
                        _battlefield.ShipsOnField
                        );
                    break;

                case AirshipControllerType.Player:
                    Controller = new PlayerAirshipController
                        (
                        ModelAttributes,
                        stateData,
                        _hardPoints,
                        _battlefield.ShipsOnField
                        );
                    break;
            }


#if ENABLE_DAMAGEMESH
            _hullIntegrityMesh = new HullIntegrityMesh(HullSectionContainer, _battlefield.ProjectileEngine, Controller.Position, ModelAttributes.Length);
#endif

            //DebugText.CreateText("x:", 0, 0);
            //DebugText.CreateText("y:", 0, 15);
            //DebugText.CreateText("z:", 0, 30);

            sw.Stop();

            DebugConsole.WriteLine("Airship class assembled in " + sw.ElapsedMilliseconds + " ms");
        }

        public AirshipStateData StateData{
            get { return Controller.StateData; }
        }

        public ModelAttributes ModelAttributes { get; private set; }

        public ModelAttributes BuffedModelAttributes{
            get { return Controller.GetBuffedAttributes(); }
        }

        //public Vector3 Centroid { get; private set; }
        public HullSectionContainer HullSectionContainer { get; private set; }
        public DeckSectionContainer DeckSectionContainer { get; private set; }

        #region IDisposable Members

        public void Dispose(){
            Debug.Assert(!_disposed);
#if ENABLE_DAMAGEMESH
            _hullIntegrityMesh.Dispose();
#endif

            DeckSectionContainer.Dispose();
            HullSectionContainer.Dispose();

            foreach (var hardPoint in _hardPoints){
                hardPoint.Dispose();
            }
            _disposed = true;
        }

        #endregion

        public void SetAutoPilot(AirshipAutoPilot autoPilot){
            Controller.SetAutoPilot(autoPilot);
        }

        public void Update(double timeDelta){
            //DebugText.SetText("x:", "x:" + _controller.StateData.Position.X);
            //DebugText.SetText("y:", "y:" + _controller.StateData.Position.Y);
            //DebugText.SetText("z:", "z:" + _controller.StateData.Position.Z);
            Controller.Update(timeDelta);
            SetAirshipWMatrix(Controller.WorldTransform);
        }

        public void AddVisibleLayer(int _){
            HullSectionContainer.SetTopVisibleDeck(HullSectionContainer.TopExpIdx - 1);
            DeckSectionContainer.SetTopVisibleDeck(DeckSectionContainer.TopExpIdx - 1);
        }

        public void RemoveVisibleLayer(int _){
            HullSectionContainer.SetTopVisibleDeck(HullSectionContainer.TopExpIdx + 1);
            DeckSectionContainer.SetTopVisibleDeck(DeckSectionContainer.TopExpIdx + 1);
        }

        void SetAirshipWMatrix(Matrix worldTransform){
#if ENABLE_DAMAGEMESH
            _hullIntegrityMesh.WorldTransform = worldTransform;
#endif

            foreach (var hullLayer in HullSectionContainer.HullBuffersByDeck){
                hullLayer.WorldTransform = worldTransform;
            }

            foreach (var deckLayer in DeckSectionContainer.DeckBufferByDeck){
                deckLayer.WorldTransform = worldTransform;
            }

            foreach (var hardPoint in _hardPoints){
                hardPoint.ShipTranslationMtx = worldTransform;
            }
        }

        ~Airship(){
            Debug.Assert(_disposed);
        }
    }
}