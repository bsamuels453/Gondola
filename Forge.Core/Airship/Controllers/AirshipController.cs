﻿#region

using System.Collections.Generic;
using System.Linq;
using Forge.Core.Airship.Data;
using Forge.Framework;
using MonoGameUtility;

#endregion

namespace Forge.Core.Airship.Controllers{
    internal abstract class AirshipController{
        const float _degreesPerRadian = 0.0174532925f;
        readonly List<Hardpoint> _hardPoints;
        ModelAttributes _airshipModelData;
        float _angleVel;
        float _ascentRate;
        float _velocity;
        float _velocityTarget;

        protected AirshipController(ModelAttributes modelData, AirshipStateData stateData, List<Hardpoint> hardPoints){
            _airshipModelData = modelData;
            _hardPoints = hardPoints;

            Position = stateData.Position;
            Angle = stateData.Angle;
            _velocity = stateData.Velocity;
            _ascentRate = stateData.AscentRate;
            VelocityTarget = stateData.Velocity;

            MaxVelocityMod = 1;
            MaxTurnRateMod = 1;
            MaxAscentRateMod = 1;
            MaxAccelerationMod = 1;
            MaxTurnAccelerationMod = 1;
            MaxAscentAccelerationMod = 1;

            ActiveBuffs = stateData.ActiveBuffs;
            RecalculateBuffs();
        }

        #region statistical properties

        /// <summary>
        ///   Position of the airship.
        /// </summary>
        public Vector3 Position { get; private set; }

        /// <summary>
        ///   3D angle of the airship in radians.
        /// </summary>
        public Vector3 Angle { get; private set; }

        /// <summary>
        ///   The worldmatrix used to translate the position of the airship into model space.
        /// </summary>
        public Matrix WorldMatrix { get; private set; }

        public List<AirshipBuff> ActiveBuffs { get; private set; }

        public float MaxVelocityMod { get; private set; }
        public float MaxTurnRateMod { get; private set; }
        public float MaxAscentRateMod { get; private set; }
        public float MaxAccelerationMod { get; private set; }
        public float MaxTurnAccelerationMod { get; private set; }
        public float MaxAscentAccelerationMod { get; private set; }

        /// <summary>
        ///   Meters per second
        /// </summary>
        public float MaxVelocity{
            get { return _airshipModelData.MaxForwardVelocity*MaxVelocityMod; }
        }

        /// <summary>
        ///   Radians per second
        /// </summary>
        public float MaxTurnRate{
            get { return _airshipModelData.MaxTurnSpeed*MaxTurnRateMod; }
        }

        /// <summary>
        ///   Meters per second
        /// </summary>
        public float MaxAscentRate{
            get { return _airshipModelData.MaxAscentRate*MaxAscentRateMod; }
        }

        /// <summary>
        ///   Meters per second squared
        /// </summary>
        public float MaxAcceleration{
            get { return _airshipModelData.MaxAcceleration*MaxAccelerationMod; }
        }

        /// <summary>
        ///   Radians per second squared
        /// </summary>
        public float MaxTurnAcceleration{
            get { return _airshipModelData.MaxTurnAcceleration*MaxTurnAccelerationMod; }
        }

        /// <summary>
        ///   Meters per second squared
        /// </summary>
        public float MaxAscentAcceleration{
            get { return _airshipModelData.MaxAscentAcceleration*MaxAscentAccelerationMod; }
        }

        #endregion

        #region Movement Properties

        /// <summary>
        ///   Sets angular velocity of the ship on the XZ plane. Scales from -MaxTurnSpeed to MaxTurnSpeed, where negative indicates a turn to port. Measured in degrees/second.
        /// </summary>
        public float TurnVelocity{
            get { return _angleVel; }
            protected set{
                float turnSpeed = value;
                if (value > _airshipModelData.MaxTurnSpeed*MaxTurnRateMod)
                    turnSpeed = _airshipModelData.MaxTurnSpeed*MaxTurnRateMod;
                if (value < -_airshipModelData.MaxTurnSpeed*MaxTurnRateMod)
                    turnSpeed = -_airshipModelData.MaxTurnSpeed*MaxTurnRateMod;
                _angleVel = turnSpeed;
            }
        }

        /// <summary>
        ///   Sets ascent velocity of the ship on the XZ plane. Scales from -MaxAscentRate to MaxAscentRate, where negative indicates moving down. Measured in meters per second.
        /// </summary>
        public float AscentRate{
            get { return _ascentRate; }
            protected set{
                float ascentRate = value;
                if (value > _airshipModelData.MaxAscentRate*MaxAscentRateMod)
                    ascentRate = _airshipModelData.MaxAscentRate*MaxAscentRateMod;
                if (value < -_airshipModelData.MaxAscentRate*MaxAscentRateMod)
                    ascentRate = -_airshipModelData.MaxAscentRate*MaxAscentRateMod;
                _ascentRate = ascentRate;
            }
        }

        /// <summary>
        ///   Sets airship velocity target scalar. Scales from -MaxReverseVelocity to MaxForwardVelocity Measured in meters per second.
        /// </summary>
        public float VelocityTarget{
            get { return _velocityTarget; }
            protected set{
                float velocityTarget = value;
                if (value > _airshipModelData.MaxForwardVelocity*MaxVelocityMod)
                    velocityTarget = _airshipModelData.MaxForwardVelocity*MaxVelocityMod;
                if (value < -_airshipModelData.MaxReverseVelocity*MaxVelocityMod)
                    velocityTarget = -_airshipModelData.MaxReverseVelocity*MaxVelocityMod;
                _velocityTarget = velocityTarget;
            }
        }


        //todo: depreciate this
        /// <summary>
        ///   Sets airship velocity scalar. Scales from -MaxReverseVelocity to MaxForwardSpeed Measured in meters per second.
        /// </summary>
        public float Velocity{
            get { return _velocity; }
            protected set { _velocity = value; }
        }

        #endregion

        public void SetAutoPilot(){
        }

        /// <summary>
        ///   Applies a specified buff to the airship to modify how it moves.
        /// </summary>
        public void ApplyBuff(AirshipBuff newBuff){
            ActiveBuffs.Add(newBuff);
            RecalculateBuffs();
        }

        /// <summary>
        ///   Recalculates the attribute modifiers based off of the buffs in ActiveBuffs.
        /// </summary>
        void RecalculateBuffs(){
            var activeBuffsGrouped =
                from buff in ActiveBuffs
                group buff by buff.Type;

            foreach (var buffType in activeBuffsGrouped){
                float statModifier = 1;

                foreach (var buff in buffType){
                    statModifier = statModifier*buff.Modifier;
                }
                switch (buffType.Key){
                    case AirshipBuff.BuffType.MaxAscentAcceleration:
                        MaxAscentAccelerationMod = statModifier;
                        break;
                    case AirshipBuff.BuffType.MaxTurnAcceleration:
                        MaxTurnAccelerationMod = statModifier;
                        break;
                    case AirshipBuff.BuffType.MaxAcceleration:
                        MaxAccelerationMod = statModifier;
                        break;
                    case AirshipBuff.BuffType.MaxAscentRate:
                        MaxAscentRateMod = statModifier;
                        break;
                    case AirshipBuff.BuffType.MaxTurnRate:
                        MaxTurnRateMod = statModifier;
                        break;
                    case AirshipBuff.BuffType.MaxVelocity:
                        MaxVelocityMod = statModifier;
                        break;
                    default:
                        DebugConsole.WriteLine("WARNING: Unhandled buff type detected: " + buffType);
                        break;
                }
            }
        }

        protected void Fire(){
            foreach (var hardPoint in _hardPoints){
                hardPoint.Fire();
            }
        }

        public void Update(ref InputState state, double timeDelta){
            UpdateController(ref state, timeDelta);
            float timeDeltaSeconds = (float) timeDelta/1000;

            var ang = Angle;
            ang.Y += _angleVel*_degreesPerRadian*timeDeltaSeconds;
            var unitVec = Common.GetComponentFromAngle(ang.Y, 1);
            Angle = ang;

            var position = Position;
            position.X += unitVec.X*_velocity*timeDeltaSeconds;
            position.Z += -unitVec.Y*_velocity*timeDeltaSeconds;
            position.Y += _ascentRate*timeDeltaSeconds;
            Position = position;

            WorldMatrix = Common.GetWorldTranslation(Position, Angle, _airshipModelData.Length);
        }

        protected abstract void UpdateController(ref InputState state, double timeDelta);
    }
}