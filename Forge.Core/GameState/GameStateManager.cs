﻿#region

using System.Diagnostics;
using Forge.Core.Camera;
using Forge.Core.Input;

#endregion

namespace Forge.Core.GameState{
    internal delegate void OnCameraControllerChange(ICamera prevCamera, ICamera newCamera);

    internal static class GamestateManager{
        static readonly InputHandler _inputHandler;

        static IGameState _activeState;
        static readonly Stopwatch _stopwatch;

        static GamestateManager(){
            _activeState = null;
            _inputHandler = new InputHandler();
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
        }

        public static ICamera CameraController { get; set; }


        public static void ClearState(){
            _activeState.Dispose();
            _activeState = null;
        }

        public static void AddGameState(IGameState newState){
            _activeState = newState;
        }

        public static void Update(){
            _inputHandler.Update();

            _stopwatch.Stop();
            double d = _stopwatch.ElapsedMilliseconds;
            _activeState.Update(_inputHandler.CurrentInputState, d);
            _stopwatch.Restart();
        }

        public static void Draw(){
            _activeState.Draw();

            /*if (_useGlobalRenderTarget){
                _globalRenderTarget.Draw(CameraController.ViewMatrix, Color.Transparent);
            }*/
        }
    }
}