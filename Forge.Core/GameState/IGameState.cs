﻿#region

using System;
using Forge.Framework;

#endregion

namespace Forge.Core.GameState{
    internal interface IGameState : IDisposable{
        void Update(InputState state, double timeDelta);
        void Draw();
    }
}