﻿#region

using System.Collections.Generic;
using System.Diagnostics;
using Forge.Framework.Control;
using Forge.Framework.Draw;
using MonoGameUtility;

#endregion

namespace Forge.Framework.UI.Elements{
    public class ClickMask : IUIElement{
        const float _fadedInAlpha = 0.2f;
        readonly MaskingSprite _mask;
        float _alpha;
        Rectangle _boundingBox;

        public ClickMask(Rectangle boundingBox, UIElementCollection parent){
            FrameStrata = new FrameStrata(FrameStrata.Level.Highlight, parent.FrameStrata, "ClickMask");
            _boundingBox = boundingBox;
            MouseController = new MouseController(this);
            parent.OnMouseExit += OnMouseExit;
            parent.OnLeftDown += OnMouseLeftDown;
            parent.OnLeftRelease += OnMouseLeftUp;

            _mask = new MaskingSprite
                (
                "Materials/SolidBlack",
                boundingBox
                );

            Alpha = 0;
        }

        #region IUIElement Members

        public FrameStrata FrameStrata { get; private set; }

        public int X{
            get { return _boundingBox.X; }
            set{
                _boundingBox.X = value;
                _mask.X = value;
            }
        }

        public int Y{
            get { return _boundingBox.Y; }
            set{
                _boundingBox.Y = value;
                _mask.Y = value;
            }
        }

        public int Width{
            get { return _boundingBox.Width; }
        }

        public int Height{
            get { return _boundingBox.Height; }
        }

        public float Alpha{
            get { return _alpha; }
            set{
                _alpha = value;
                _mask.Alpha = value;
            }
        }

        public MouseController MouseController { get; private set; }

        public bool IsTransparent{
            get { return true; }
            set { Debug.Assert(value); }
        }

        public bool HitTest(int x, int y){
            return false;
        }

        public List<IUIElement> GetElementStackAtPoint(int x, int y){
            return new List<IUIElement>();
        }

        public void Update(float timeDelta){
        }

        public void Dispose(){
            _mask.Dispose();
        }

        #endregion

        void OnMouseExit(ForgeMouseState state, float timeDelta, UIElementCollection caller){
            Alpha = 0;
        }

        void OnMouseLeftDown(ForgeMouseState state, float timeDelta, UIElementCollection caller){
            if (caller.ContainsMouse){
                Alpha = _fadedInAlpha;
            }
        }

        void OnMouseLeftUp(ForgeMouseState state, float timeDelta, UIElementCollection caller){
            Alpha = 0;
        }
    }
}