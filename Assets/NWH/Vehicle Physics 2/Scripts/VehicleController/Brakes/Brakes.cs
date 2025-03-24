using System;
using System.Collections.Generic;
using NWH.VehiclePhysics2.Powertrain;
using NWH.VehiclePhysics2.Powertrain.Wheel;
using UnityEngine;
using UnityEngine.Events;
using NWH.Common.Vehicles;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NWH.VehiclePhysics2
{
    /// <summary>
    ///     Assigns brake torque to individual wheels. Actual braking happens inside WheelController.
    /// </summary>
    [Serializable]
    public partial class Brakes : VehicleComponent
    {
        public delegate float BrakeTorqueModifier();

        /// <summary>
        ///     Handbrake type.
        ///     - Standard - handbrake is active while held.
        ///     - Latching - first press of the button activates the handbrake while the second one releases it.
        ///     Latching handbrake also works with analog input and the strength while latched will correspond
        ///     to the highest input (i.e. it will latch on the highest notch that was reached through analog input).
        /// </summary>
        public enum HandbrakeType
        {
            Standard,
            Latching,
        }


        /// <summary>
        ///     Strength of off-throttle braking in percentage [0 to 1] of max braking torque.
        ///     Set to 0 to disable braking off-throttle.
        /// </summary>
        [Range(0, 1)]
        [Tooltip("    Strength of off-throttle braking in percentage [0 to 1] of max braking torque.")]
        public float brakeOffThrottleIntensity = 0f;


        /// <summary>
        ///     Collection of functions that modify the braking performance of the vehicle. Used for modules such as ABS where
        ///     brakes need to be overriden or their effect reduced/increase. Return 1 for neutral modifier while returning 0 will
        ///     disable the brakes completely. All brake torque modifiers will be multiplied in order to get the final brake torque
        ///     coefficient.
        /// </summary>
        [Tooltip(
            "Collection of functions that modify the braking performance of the vehicle. Used for modules such as ABS where brakes need to be overriden or their effect reduced/increase. Return 1 for neutral modifier while returning 0 will disable the brakes completely. All brake torque modifiers will be multiplied in order to get the final brake torque coefficient.")]
        public List<BrakeTorqueModifier> brakeTorqueModifiers = new List<BrakeTorqueModifier>();


        /// <summary>
        ///     Should brakes be applied when vehicle is disabled?
        /// </summary>
        [Tooltip("    Should brakes be applied when vehicle is disabled?")]
        [FormerlySerializedAs("brakeWhileAsleep")]
        [ShowInSettings("Brake While Disabled")]
        public bool brakeWhileDisabled = true;


        /// <summary>
        ///     If true vehicle will break when in neutral and no throttle is applied.
        /// </summary>
        [Tooltip("    If true vehicle will break when in neutral and no throttle is applied.")]
        [ShowInSettings("Brake While Idle")]
        public bool brakeWhileIdle = true;


        /// <summary>
        /// Should the vehicle apply brakes when the movement direction is opposite of input direction?
        /// </summary>
        [UnityEngine.Tooltip("Should the vehicle apply brakes when the movement direction is opposite of input direction?")]
        public bool brakeOnReverseDirection;

        /// <summary>
        ///     Max brake torque that can be applied to each wheel. To adjust braking on per-axle basis change brake coefficients
        ///     under Axle settings.
        /// </summary>
        [Tooltip("Max brake torque that can be applied to each wheel. " +
                 "To adjust braking on per-axle basis change brake coefficients under Axle settings")]
        [ShowInSettings("Max. Torque")]
        public float maxTorque = 7000f;


        [ShowInSettings("Handbrake Type")]
        public HandbrakeType handbrakeType = HandbrakeType.Standard;


        /// <summary>
        ///     Current value of the handbrake. 0 = inactive, 1 = maximum strength.
        ///     Handbrake strength will also be affected by per wheel group handbrake settings.
        /// </summary>
        [Tooltip(
            "    Current value of the handbrake. 0 = inactive, 1 = maximum strength.\r\n    Handbrake strength will also be affected by per wheel group handbrake settings.")]
        public float handbrakeValue;


        /// <summary>
        ///     Higher smoothing will result in brakes being applied more gradually.
        /// </summary>
        [Range(0f, 1f)]
        [Tooltip("    Higher smoothing will result in brakes being applied more gradually.")]
        public float actuationTime = 0.1f;


        /// <summary>
        ///     Called each time brakes are activated.
        /// </summary>
        [Tooltip("    Called each time brakes are activated.")]
        public UnityEvent onBrakesActivate = new UnityEvent();


        /// <summary>
        ///     Called each time brakes are released.
        /// </summary>
        [Tooltip("    Called each time brakes are released.")]
        public UnityEvent onBrakesDeactivate = new UnityEvent();


        /// <summary>
        ///     Is the vehicle currently braking?
        /// </summary>
        private bool _isBraking;


        /// <summary>
        ///     Was the vehicle braking the previous frame.
        /// </summary>
        private bool _wasBraking;
        private float _handbrakeInput;
        private bool _handbrakeActive;
        private bool _handbrakeWasReset;
        private float _brakeInput;
        private float _throttleInput;


        /// <summary>
        ///     Returns true if vehicle is currently braking. Will return true if there is ANY brake torque applied to the wheels.
        /// </summary>
        public bool IsBraking
        {
            get { return _isBraking; }
            set { _isBraking = value; }
        }


        public override void VC_FixedUpdate()
        {
            base.VC_FixedUpdate();
            _isBraking = false;

            // Reset brakes for this frame
            for (int i = 0; i < vehicleController.powertrain.wheelCount; i++)
            {
                WheelComponent wc = vehicleController.powertrain.wheels[i];
                wc.wheelUAPI.BrakeTorque = 0;
            }

            float brakeTorqueModifier = SumBrakeTorqueModifiers();
            if (actuationTime < Vehicle.SMALL_NUMBER)
            {
                actuationTime = Vehicle.SMALL_NUMBER;
            }

            _brakeInput = Mathf.MoveTowards(_brakeInput, vehicleController.input.InputSwappedBrakes, (1f / actuationTime) * vehicleController.fixedDeltaTime);
            _throttleInput = vehicleController.input.InputSwappedThrottle;

            if (brakeTorqueModifier <= Vehicle.INPUT_DEADZONE && _brakeInput <= Vehicle.INPUT_DEADZONE)
            {
                return;
            }

            // Handbrake 
            _handbrakeInput = vehicleController.input.Handbrake;
            if (handbrakeType == HandbrakeType.Standard)
            {
                handbrakeValue = _handbrakeInput;
                _handbrakeActive = _handbrakeInput > Vehicle.INPUT_DEADZONE;
            }
            else
            {
                if (_handbrakeInput < Vehicle.INPUT_DEADZONE)
                {
                    _handbrakeWasReset = true;
                }

                if (_handbrakeInput > Vehicle.INPUT_DEADZONE && !_handbrakeActive && _handbrakeWasReset)
                {
                    _handbrakeActive = true;
                    _handbrakeWasReset = false;
                }

                if (_handbrakeInput > Vehicle.INPUT_DEADZONE && _handbrakeActive && _handbrakeWasReset)
                {
                    _handbrakeActive = false;
                    _handbrakeWasReset = false;
                }

                if (_handbrakeActive)
                {
                    handbrakeValue = _handbrakeInput > handbrakeValue ? _handbrakeInput : handbrakeValue;
                }
                else
                {
                    handbrakeValue = 0;
                }
            }

            if (handbrakeValue > Vehicle.INPUT_DEADZONE)
            {
                AddBrakeTorqueAllWheels(handbrakeValue * brakeTorqueModifier * maxTorque, true);
            }


            float brakeTorqueSum = 0;
            int currentGear = vehicleController.powertrain.transmission.Gear;

            // Brake off throttle
            if (brakeOffThrottleIntensity != 0 && _throttleInput < Vehicle.INPUT_DEADZONE)
            {
                brakeTorqueSum += brakeOffThrottleIntensity * maxTorque;
                _isBraking = true;
            }

            // Brake on wrong direction
            if (brakeOnReverseDirection)
            {
                float gearSign = currentGear >= 0 ? 1f : -1f;
                if ((_throttleInput * gearSign > 0.2f && vehicleController.SpeedSigned < -0.2f) || _throttleInput * gearSign < -0.2f && vehicleController.SpeedSigned > 0.2f)
                {
                    brakeTorqueSum += maxTorque;
                }
            }


            // Brake while idle 
            bool idleBrake = brakeWhileIdle 
                && _throttleInput < Vehicle.INPUT_DEADZONE 
                && currentGear == 0 
                && vehicleController.Speed < Vehicle.SPEED_DEADZONE;

            if (idleBrake)
            {
                brakeTorqueSum += brakeTorqueModifier * maxTorque;
                _isBraking = true;
            }

            if (_brakeInput > Vehicle.INPUT_DEADZONE)
            {
                brakeTorqueSum += _brakeInput * brakeTorqueModifier * maxTorque;
                _isBraking = true;
            }

            AddBrakeTorqueAllWheels(brakeTorqueSum);

            if (_isBraking && !_wasBraking)
            {
                onBrakesActivate.Invoke();
            }
            else if (!_isBraking && _wasBraking)
            {
                onBrakesDeactivate.Invoke();
            }

            _wasBraking = _isBraking;
        }


        public override bool VC_Disable(bool calledByParent)
        {
            if (base.VC_Disable(calledByParent))
            {
                // Brake while disabled
                if (brakeWhileDisabled)
                {
                    float brakeTorqueModifier = SumBrakeTorqueModifiers();
                    _isBraking = true;
                    AddBrakeTorqueAllWheels(brakeTorqueModifier * maxTorque);
                }

                return true;
            }
            return false;
        }


        private void ResetBrakeTorque()
        {
            for (int i = 0; i < vehicleController.powertrain.wheelCount; i++)
            {
                vehicleController.powertrain.wheels[i].wheelUAPI.BrakeTorque = 0f;
            }
        }


        public void AddBrakeTorqueAllWheels(float brakeTorque, bool isHandbrake = false)
        {
            brakeTorque = brakeTorque < 0f ? 0f : brakeTorque > maxTorque ? maxTorque : brakeTorque;

            for (int i = 0; i < vehicleController.powertrain.wheelCount; i++)
            {
                WheelComponent wheelComponent = vehicleController.powertrain.wheels[i];
                wheelComponent.AddBrakeTorque(brakeTorque, isHandbrake);
            }

            if (brakeTorque > 1f && !isHandbrake)
            {
                _isBraking = true;
            }
        }


        private float SumBrakeTorqueModifiers()
        {
            if (brakeTorqueModifiers.Count == 0)
            {
                return 1f;
            }

            float coefficient = 1;
            int n = brakeTorqueModifiers.Count;
            for (int i = 0; i < n; i++)
            {
                coefficient *= brakeTorqueModifiers[i].Invoke();
            }

            return Mathf.Clamp(coefficient, 0f, Mathf.Infinity);
        }
    }
}


#if UNITY_EDITOR

namespace NWH.VehiclePhysics2.GroundDetection
{
    /// <summary>
    ///     Property drawer for Brakes.
    /// </summary>
    [CustomPropertyDrawer(typeof(Brakes))]
    public partial class BrakesDrawer : ComponentNUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.BeginSubsection("Braking");
            drawer.Field("maxTorque", true, "Nm");
            drawer.Field("actuationTime");
            drawer.Field("brakeWhileIdle");
            drawer.Field("brakeWhileDisabled");
            drawer.Field("brakeOnReverseDirection");
            drawer.EndSubsection();

            drawer.BeginSubsection("Handbrake");
            drawer.Field("handbrakeType");
            drawer.Field("handbrakeValue", false);
            drawer.EndSubsection();

            drawer.BeginSubsection("Off-Throttle Braking");
            drawer.Field("brakeOffThrottleIntensity");
            drawer.EndSubsection();

            drawer.EndProperty();
            return true;
        }
    }
}
#endif