﻿#region

using System;
using System.Collections.Generic;
using System.Diagnostics;

#endregion

namespace Gondola.Logic{
    internal static class GamestateManager{
        static readonly InputHandler _inputHandler;

        static readonly List<IGameState> _activeStates;
        static readonly Dictionary<SharedStateData, object> _sharedData;

        static GamestateManager(){
            _activeStates = new List<IGameState>();
            _inputHandler = new InputHandler();
            _sharedData = new Dictionary<SharedStateData, object>();//todo-optimize: might be able to make this into a list instead
        }

        public static void ClearAllStates() {
            foreach (var state in _activeStates){
                state.Dispose();
            }
            _activeStates.Clear();
        }

        public static void ClearState(IGameState state) {
            _activeStates.Remove(state);
            state.Dispose();
        }

        public static object QuerySharedData(SharedStateData identifier) {
            return _sharedData[identifier];
        }

        public static void AddSharedData(SharedStateData identifier, object data) {
            _sharedData.Add(identifier, data);
        }

        public static void ModifySharedData(SharedStateData identifier, object data) {
            _sharedData[identifier] = data;
        }

        public static void DeleteSharedData(SharedStateData identifier) {
            _sharedData.Remove(identifier);
        }

        public static void AddGameState(IGameState newState) {
            _activeStates.Add(newState);
        }

        public static void Update() {
            _inputHandler.Update();
            foreach (var state in _activeStates){
                state.Update(_inputHandler.CurrentInputState, 0);
            }
        }

        public static void Draw() {
            foreach (var state in _activeStates){
                state.Draw();
            }
        }
    }
}