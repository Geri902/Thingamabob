using System;
using NWH.VehiclePhysics2.GroundDetection;
using NWH.VehiclePhysics2.Powertrain.Wheel;
using UnityEngine;
using NWH.Common.Vehicles;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Powertrain
{
    [Serializable]
    public partial class WheelComponent : PowertrainComponent
    {
        [NonSerialized]
        [ShowInTelemetry]
        public int surfaceMapIndex = -1;

        [ShowInTelemetry]
        public SurfacePreset surfacePreset;

        public WheelUAPI wheelUAPI;

        public WheelGroupSelector wheelGroupSelector = new WheelGroupSelector();

        [NonSerialized]
        public WheelGroup wheelGroup;

        private float _initialRollingResistance;


        protected override void VC_Initialize()
        {
            _initialRollingResistance = wheelUAPI.RollingResistanceTorque;

            base.VC_Initialize();
        }


        public override void VC_Validate(VehicleController vc)
        {
            // Do not run base, not applicable to WheelComponent

            if (wheelUAPI == null)
            {
                PC_LogWarning(vc, $"WheelUAPI is null. Make sure to assign a wheel under VehicleController > PWR > Wheels. " +
                    "If the vehicle has been set up pre 10.0b, wheels will need re-assigning.");
            }
        }


        /// <summary>
        ///     Adds brake torque to the wheel on top of the existing torque. Value is clamped to max brake torque.
        /// </summary>
        /// <param name="torque">Torque in Nm that will be applied to the wheel to slow it down.</param>
        /// <param name="registerAsBraking">If true brakes.IsBraking flag will be set. This triggers brake lights.</param>
        public void AddBrakeTorque(float torque, bool isHandbrake = false)
        {
            float brakeTorque = wheelUAPI.BrakeTorque;

            if (wheelGroup != null)
            {
                torque *= isHandbrake ? wheelGroup.handbrakeCoefficient : wheelGroup.brakeCoefficient;
            }

            if (torque < 0)
            {
                brakeTorque += 0f;
            }
            else
            {
                brakeTorque += torque;
            }

            if (brakeTorque > vehicleController.brakes.maxTorque)
            {
                brakeTorque = vehicleController.brakes.maxTorque;
            }

            if (brakeTorque < 0)
            {
                brakeTorque = 0;
            }

            wheelUAPI.BrakeTorque = brakeTorque;
        }


        public override float QueryAngularVelocity(float angularVelocity, float dt)
        {
            inputAngularVelocity = outputAngularVelocity = wheelUAPI.AngularVelocity;
            return outputAngularVelocity;
        }


        public override float QueryInertia()
        {
            // Calculate the base inertia of the wheel and scale it by the inverse of the dt.
            float dtScale = Mathf.Clamp(vehicleController.fixedDeltaTime, 0.01f, 0.05f) / 0.005f;
            return 0.5f * wheelUAPI.Mass * wheelUAPI.Radius * wheelUAPI.Radius * dtScale;
        }


        public void ApplyRollingResistanceMultiplier(float multiplier)
        {
            wheelUAPI.RollingResistanceTorque = _initialRollingResistance * multiplier;
        }


        public override float ForwardStep(float torque, float inertiaSum, float dt)
        {
            inputTorque = torque;
            inputInertia = inertiaSum;
            outputTorque = inputTorque;
            outputInertia = wheelUAPI.Mass * wheelUAPI.Radius * wheelUAPI.Radius + inertiaSum;
            wheelUAPI.MotorTorque = outputTorque;
            wheelUAPI.Inertia = outputInertia;
            wheelUAPI.AutoSimulate = false;
            wheelUAPI.Step();
            return wheelUAPI.CounterTorque;
        }


        public override bool VC_Disable(bool calledByParent)
        {
            if (base.VC_Disable(calledByParent))
            {
                wheelUAPI.BrakeTorque = 0f;
                wheelUAPI.MotorTorque = 0f;
                wheelUAPI.AutoSimulate = true;

                return true;
            }

            return false;
        }


        public void SetWheelGroup(int wheelGroupIndex)
        {
            wheelGroupSelector.index = wheelGroupIndex;
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Powertrain
{
    [CustomPropertyDrawer(typeof(WheelComponent))]
    public partial class WheelComponentDrawer : PowertrainComponentDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            VehicleController vehicleController = property.serializedObject.targetObject as VehicleController;
            WheelComponent wheelComponent = SerializedPropertyHelper.GetTargetObjectOfProperty(property) as WheelComponent;

            DrawCommonProperties();

            drawer.BeginSubsection("Wheel Settings");

#if NWH_WC3D
            drawer.Info("Detected WheelController3D.\r\nIf this is not correct remove NWH_WC3D from the Scripting Define Symbols.");
#else
            drawer.Info("WheelController3D not present.\r\nSince v2.0 it is shipped as a separate package.");
#endif

            drawer.Field("wheelUAPI");

            drawer.Field("wheelGroupSelector");

            //SerializedProperty wheelControllerProperty = drawer.FindProperty("wheelController");
            //drawer.EmbeddedObjectEditor(SerializedPropertyHelper.GetTargetObjectOfProperty(wheelControllerProperty) as Object,
            //    drawer.positionRect);
            drawer.EndSubsection();

            drawer.EndProperty();
            return true;
        }

        public override void DrawPowertrainOutputSection(ref Rect rect, VehicleController vc, PowertrainComponent pc)
        {
            // WheelComponent does not support outputs, do nothing.
        }
    }
}

#endif
