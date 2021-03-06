﻿#region

using System.Diagnostics;

#endregion

namespace Forge.Framework.Control{
    /// <summary>
    /// Used to obtain and release control of the mouse. This class dispatches mouse updates to every
    /// controller subscribed to it. However, if a controller requests exclusive focus of the mouse,
    /// then only that controller will receive updates until that focus is released.
    /// </summary>
    public class MouseManager{
        readonly PriorityQueue<MouseController> _globalControllers;

        /// <summary>
        ///   The current exclusive controller that the MouseManager class is dispatching events to.
        /// </summary>
        MouseController _curController;

        /// <summary>
        ///   The current state of the mouse. This is recorded here so that it can be used as the "prevstate" at next tick.
        /// </summary>
        ForgeMouseState _curState;

        public MouseManager(){
            _curState = new ForgeMouseState();
            _globalControllers = new PriorityQueue<MouseController>();
        }

        public void AddGlobalController(MouseController controller, float priority){
            _globalControllers.Add(controller, priority);
        }

        /// <summary>
        ///   Main method used for obtaining the mouse's focus so that events can be dispatched.
        /// </summary>
        /// <param name="controller"> The structure containing all of the events that the mouse will invoke. </param>
        public void ObtainExclusiveFocus(MouseController controller){
            Debug.Assert(_curController == null);
            _curController = controller;
        }

        /// <summary>
        ///   Releases focus of the mouse so that the current controller will no longer recieve exclusive event updates.
        ///   To ensure the correct mousecontroller is losing focus, the controller to be released must be provided.
        /// </summary>
        public void ReleaseExclusiveFocus(MouseController controller){
            Debug.Assert(controller == _curController);
            _curController = null;
        }

        /// <summary>
        ///   Updates the mouse and invokes any of the controller's events that are relevant to the update.
        /// </summary>
        /// <param name="timeDelta"> Time since the last tick, in milliseconds. </param>
        public void UpdateMouse(double timeDelta){
            var prevState = _curState;
            _curState = new ForgeMouseState(prevState, timeDelta);
            if (_curController != null){
                if (_curState.MouseMoved){
                    _curController.SafeInvokeOnMouseMovement(_curState, (float) timeDelta);
                }
                if (_curState.LeftButtonChange || _curState.RightButtonChange){
                    if (_curController != null){
                        _curController.SafeInvokeOnMouseButton(_curState, (float) timeDelta);
                    }
                }
                if (_curState.MouseScrollChange != 0){
                    if (_curController != null){
                        _curController.SafeInvokeOnMouseScroll(_curState, (float) timeDelta);
                    }
                }
            }
            else{
                foreach (var controller in _globalControllers){
                    if (!_curState.BlockMPosition){
                        if (_curState.MouseMoved){
                            controller.SafeInvokeOnMouseMovement(_curState, (float) timeDelta);
                        }
                    }
                    if (!_curState.BlockLeftMButton && !_curState.BlockRightMButton){
                        if (_curState.LeftButtonChange || _curState.RightButtonChange){
                            controller.SafeInvokeOnMouseButton(_curState, (float) timeDelta);
                        }
                    }
                    if (!_curState.BlockScrollWheel){
                        if (_curState.MouseScrollChange != 0){
                            controller.SafeInvokeOnMouseScroll(_curState, (float) timeDelta);
                        }
                    }
                }
            }
        }
    }
}