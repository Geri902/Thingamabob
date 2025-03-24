using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using NWH.Common.Vehicles;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Powertrain.Wheel
{
    [Serializable]
    public partial class WheelGroup
    {
        /// <summary>
        /// Name of the WheelGroup.
        /// </summary>
        public string name;


        /// <summary>
        ///     Should Ackerman steering angle be added to the axle?
        ///     angle is auto-calculated.
        /// </summary>
        [UnityEngine.Tooltip("    Should Ackerman steering angle be added to the axle?\r\n    angle is auto-calculated.")]
        [ShowInSettings("Add Ackerman")]
        public bool addAckerman = true;


        /// <summary>
        /// Used to reduce roll in the vehicle. Too high values combined with low dt can cause jitter. 
        /// Another way to reduce roll is to
        /// adjust center of mass to be lower, or adjust the force application point to be higher in WheelController.
        /// </summary>
        [ShowInSettings("ARB Force", 0, 12000, 1000)]
        public float antiRollBarForce = 0f;


        /// <summary>
        ///     If set to 1 group will receive full brake torque as set by Max Torque parameter under Brake section while 0
        ///     means no breaking at all.
        /// </summary>
        [Tooltip(
            "If set to 1 axle will receive full brake torque as set by Max Torque parameter under Brake section while " +
            "0 means no breaking at all.")]
        [Range(0f, 1f)]
        [ShowInSettings("Brake Coeff.", 0f, 1f, 0.1f)]
        public float brakeCoefficient = 1f;


        /// <summary>
        ///     If set to 1 axle will receive full brake torque when handbrake is used.
        /// </summary>
        [Range(0f, 2f)]
        [Tooltip("    If set to 1 axle will receive full brake torque when handbrake is used.")]
        [ShowInSettings("Brake Coeff.", 0f, 1f, 0.1f)]
        public float handbrakeCoefficient;


        [Tooltip(
            "Setting to true will override camber settings and camber will be calculated from position of the (imaginary) axle object instead.")]
        [ShowInSettings]
        public bool isSolid;


        /// <summary>
        ///     Track width of the axle. 0 if wheel count is not 2.
        /// </summary>
        [Tooltip("    Track width of the axle. 0 if wheel count is not 2.")]
        public float trackWidth;


        [Tooltip(
            "Determines what percentage of the steer angle will be applied to the wheel. If set to negative value" +
            " wheels will turn in direction opposite of input.")]
        [Range(-1f, 1f)]
        [ShowInSettings("Steer Coeff.", -1f, 1f, 0.1f)]
        public float steerCoefficient;


        /// <summary>
        /// Set to true if the caster angle should be applied to the wheel group.
        /// </summary>
        public bool applyCasterAngle = false;


        [Tooltip(
            "Positive caster means that whe wheel will be angled towards the front of the vehicle while negative " +
            " caster will angle the wheel in opposite direction (shopping cart wheel).")]
        [Range(-8f, 8f)]
        [ShowInTelemetry()]
        [SerializeField]
        private float _casterAngle;


        /// <summary>
        /// Set to true if the toe angle should be applied to the wheel group.
        /// </summary>
        public bool applyToeAngle = false;


        [Tooltip(
            "Positive toe angle means that the wheels will face inwards (front of the wheel angled toward longitudinal center of the vehicle).")]
        [Range(-8f, 8f)]
        [SerializeField]
        [ShowInTelemetry]
        private float _toeAngle;

        [SerializeField]
        private List<WheelComponent> wheels = new List<WheelComponent>();

        private float _camber;
        private float _arbForce;

        public VehicleController vc;


        public float ToeAngle
        {
            get { return _toeAngle; }
            set
            {
                _toeAngle = value;
                ApplyGeometryValues();
            }
        }


        public float CasterAngle
        {
            get { return _casterAngle; }
            set
            {
                _casterAngle = value;
                ApplyGeometryValues();
            }
        }


        public WheelComponent LeftWheel
        {
            get { return wheels.Count == 0 ? null : wheels[0]; }
        }


        public WheelComponent RightWheel
        {
            get { return wheels.Count <= 1 ? null : wheels[1]; }
        }


        public WheelComponent Wheel
        {
            get { return wheels.Count == 0 ? null : wheels[0]; }
        }


        public List<WheelComponent> Wheels
        {
            get { return wheels; }
        }


        public void Initialize()
        {
            FindBelongingWheels();
            ApplyGeometryValues();
        }


        public void FindBelongingWheels()
        {
            Debug.Assert(vc != null);
            int groupIndex = vc.powertrain.wheelGroups.IndexOf(this);
            wheels.Clear();

            foreach (WheelComponent wheel in FindWheelsBelongingToGroup(ref vc.powertrain.wheels, groupIndex))
            {
                AddWheel(wheel);
            }

            if (wheels.Count == 2)
            {
                trackWidth = Vector3.Distance(
                    LeftWheel.wheelUAPI.transform.position,
                    RightWheel.wheelUAPI.transform.position);
            }
        }


        public void Update()
        {
            int wheelCount = wheels.Count;

            // Calculate anti-roll bar
            if (antiRollBarForce > 0f && wheelCount == 2)
            {
                CalculateARB();
            }

            // Calculate and set solid axle camber
            if (isSolid && wheelCount == 2 && trackWidth != 0)
            {
                WheelComponent leftWheel = wheels[0];
                WheelComponent rightWheel = wheels[1];

                float s0 = leftWheel.wheelUAPI.SpringLength;
                float s1 = rightWheel.wheelUAPI.SpringLength;
                float travelDelta = s1 - s0;

                _camber = Mathf.Atan2(travelDelta, trackWidth) * Mathf.Rad2Deg;

                leftWheel.wheelUAPI.Camber = -_camber;
                rightWheel.wheelUAPI.Camber = _camber;
            }
        }


        public void CalculateARB()
        {
            WheelUAPI leftWheel = Wheels[0].wheelUAPI;
            WheelUAPI rightWheel = Wheels[1].wheelUAPI;

            // Apply anti roll bar
            if (leftWheel.IsGrounded && rightWheel.IsGrounded)
            {
                float leftTravel = leftWheel.SpringLength;
                float rightTravel = rightWheel.SpringLength;

                // Cylindrical steel shaft of which ARBs are usually made have ~linear torque vs. angle of twist characteristic
                // for the low twist angle values.
                float springLengthDiff = leftTravel - rightTravel;
                _arbForce = springLengthDiff * antiRollBarForce;

                if (leftWheel.IsGrounded || rightWheel.IsGrounded)
                {
                    // Apply the ARB force at the shock anchor point.
                    leftWheel.TargetRigidbody.AddForceAtPosition(leftWheel.transform.up * -_arbForce,
                                                            leftWheel.transform.position);
                    rightWheel.TargetRigidbody.AddForceAtPosition(rightWheel.transform.up * _arbForce,
                                                            rightWheel.transform.position);
                }
            }
        }



        public void ApplyGeometryValues()
        {
            foreach (WheelComponent wheel in Wheels)
            {
                if (applyCasterAngle || applyToeAngle)
                {
                    Vector3 currentAngles = wheel.wheelUAPI.transform.localEulerAngles;
                    if (wheel.wheelUAPI.transform.localPosition.x >= 0)
                    {
                        wheel.wheelUAPI.transform.localEulerAngles = new Vector3(
                            applyCasterAngle ? -_casterAngle : currentAngles.x,
                            applyToeAngle ? -_toeAngle : currentAngles.y,
                            currentAngles.z);
                    }
                    else
                    {
                        wheel.wheelUAPI.transform.localEulerAngles = new Vector3(
                            applyCasterAngle ? -_casterAngle : currentAngles.x,
                            applyToeAngle ? _toeAngle : currentAngles.y,
                            currentAngles.z);
                    }
                }
            }
        }


        public void AddWheel(WheelComponent wheel)
        {
            Wheels.Add(wheel);
            wheel.wheelGroup = this;
        }


        public List<WheelComponent> FindWheelsBelongingToGroup(ref List<WheelComponent> wheels, int thisGroupIndex)
        {
            List<WheelComponent> belongingWheels = new List<WheelComponent>();
            foreach (WheelComponent wheelComponent in wheels)
            {
                if (wheelComponent.wheelGroupSelector.index == thisGroupIndex)
                {
                    belongingWheels.Add(wheelComponent);
                }
            }

            return belongingWheels;
        }


        public void RemoveWheel(WheelComponent wheel)
        {
            Wheels.Remove(wheel);
        }


        public void SetWheels(List<WheelComponent> wheels)
        {
            this.wheels = wheels;
            foreach (WheelComponent wheelComponent in wheels)
            {
                wheelComponent.wheelGroup = this;
            }
        }

    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Powertrain.Wheel
{
    [CustomPropertyDrawer(typeof(WheelGroup))]
    public partial class WheelGroupDrawer : NVP_NUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            WheelGroup wheelGroup = SerializedPropertyHelper.GetTargetObjectOfProperty(property) as WheelGroup;

            drawer.BeginSubsection("General");
            drawer.Field("name");
            drawer.EndSubsection();

            drawer.BeginSubsection("Steering");
            drawer.Field("steerCoefficient");
            drawer.Field("addAckerman");
            drawer.EndSubsection();

            drawer.BeginSubsection("Brakes");
            drawer.Field("brakeCoefficient");
            drawer.Field("handbrakeCoefficient");
            drawer.EndSubsection();

            drawer.BeginSubsection("Geometry");
            drawer.Field("antiRollBarForce", true, "N/m");
            drawer.Info("High ARB values can cause jitter!", MessageType.Warning);
            if (drawer.Field("applyToeAngle").boolValue)
            {
                drawer.Field("_toeAngle", true, "deg");
            }

            if (drawer.Field("applyCasterAngle").boolValue)
            {
                drawer.Field("_casterAngle", true, "deg");
            }

            if (Application.isPlaying)
            {
                if (drawer.Button("Apply Geometry"))
                {
                    wheelGroup.ApplyGeometryValues();
                }
            }
            drawer.EndSubsection();

            drawer.BeginSubsection("Axle");
            drawer.Info("Anti-roll Bar force was removed in favor of WheelController 'Force App. Point Distance'. The effect is very similar but the latter " +
                "is better for lower physics update rate applications and does not cause jitter.");
            drawer.Space(10);
            drawer.Field("isSolid");
            drawer.Info(
                "Field 'Axle Is Solid' will only work if wheel group has two wheels - a left and a right one.");
            drawer.EndSubsection();


            drawer.EndProperty();
            return true;
        }
    }
}

#endif
