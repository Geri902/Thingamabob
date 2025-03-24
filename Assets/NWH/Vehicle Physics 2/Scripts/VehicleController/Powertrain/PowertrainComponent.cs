using System;
using System.Collections.Generic;
using NWH.Common.Utility;
using UnityEngine;
using NWH.VehiclePhysics2.Powertrain;
using NWH.Common.Vehicles;


#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Powertrain
{
    [Serializable]
    public abstract class PowertrainComponent : VehicleComponent
    {
        /// <summary>
        ///     Name of the component. Only unique names should be used on the same vehicle.
        /// </summary>
        [Tooltip("    Name of the component. Only unique names should be used on the same vehicle.")]
        [SerializeField]
        public string name = "";

        /// <summary>
        ///     Angular inertia of the component. Higher inertia value will result in a powertrain that is slower to spin up, but
        ///     also slower to spin down. Too high values will result in (apparent) sluggish response while too low values will
        ///     result in vehicle being easy to stall and possible powertrain instability / glitches.
        /// </summary>
        [Range(0.0002f, 2f)]
        [Tooltip(
            "Angular inertia of the component. Higher inertia value will result in a powertrain that is slower to spin up, but\r\nalso slower to spin down. Too high values will result in (apparent) sluggish response while too low values will\r\nresult in vehicle being easy to stall.")]
        public float inertia = 0.05f;

        public float inputTorque;

        public float outputTorque;

        public float inputAngularVelocity;

        public float outputAngularVelocity;

        public float inputInertia;

        public float outputInertia;


        /// <summary>
        ///     Input component. Set automatically.
        /// </summary>
        public PowertrainComponent Input
        {
            get { return _input; }
            set
            {
                if (value == null || value == this)
                {
                    _input = null;
                    inputNameHash = 0;
                }
                else
                {
                    _input = value;
                }

            }
        }

        [NonSerialized]
        protected PowertrainComponent _input;
        public int inputNameHash;


        /// <summary>
        ///     The PowertrainComponent this component will output to.
        /// </summary>
        public PowertrainComponent Output
        {
            get { return _output; }
            set
            {
                if (value == this)
                {
                    Debug.LogWarning($"{name}: PowertrainComponent Output can not be self.");
                    outputNameHash = 0;
                    _output = null;
                }
                else
                {
                    if (_output != null)
                    {
                        _output.inputNameHash = 0;
                        _output._input = null;
                    }

                    _output = value;

                    if (_output != null)
                    {
                        outputNameHash = _output.name.GetHashCode();
                        _output._input = this;
                        _output.inputNameHash = name.GetHashCode();
                    }
                    else
                    {
                        outputNameHash = 0;
                    }
                }
            }
        }

        [NonSerialized]
        protected PowertrainComponent _output;
        public int outputNameHash;


        /// <summary>
        ///     Powertrain component damage in range of 0 to 1.
        /// </summary>
        public float Damage
        {
            get { return _damage; }
            set { _damage = Mathf.Clamp01(value); }
        }

        protected float _damage;



        /// <summary>
        ///    Input shaft RPM of component.
        /// </summary>
        public float InputRPM
        {
            get { return UnitConverter.AngularVelocityToRPM(inputAngularVelocity); }
        }


        /// <summary>
        ///    Output shaft RPM of component.
        /// </summary>
        public float OutputRPM
        {
            get { return UnitConverter.AngularVelocityToRPM(outputAngularVelocity); }
        }


        /// <summary>
        ///     Initializes PowertrainComponent.
        /// </summary>
        protected override void VC_Initialize()
        {
            if (inertia < Vehicle.SMALL_NUMBER)
            {
                inertia = Vehicle.SMALL_NUMBER;
            }

            LoadComponentFromHash(vehicleController, ref _output, outputNameHash);
            LoadComponentFromHash(vehicleController, ref _input, inputNameHash);

            base.VC_Initialize();
        }


        public override bool VC_Disable(bool calledByParent)
        {
            if (base.VC_Disable(calledByParent))
            {
                inputAngularVelocity = 0f;
                outputAngularVelocity = 0f;
                inputTorque = 0f;
                outputTorque = 0f;
                return true;
            }

            return false;
        }


        public override void VC_SetDefaults()
        {
            base.VC_SetDefaults();

            inertia = 0.02f;
        }


        public override void VC_Validate(VehicleController vc)
        {
            base.VC_Validate(vc);

            if (inertia < 0.0001f)
            {
                inertia = 0.0001f;
                Debug.LogWarning($"{vc.name} {name}: Inertia must be larger than 0.0.0001f. Setting to 0.0.0001f.");
            }

            if (outputNameHash == 0)
            {
                PC_LogWarning(vc, "Output not set. This might be a result of the 10.20f update, in which case the " +
                    $"powertrain outputs need to be re-assigned.");
            }
        }


        public virtual float QueryAngularVelocity(float angularVelocity, float dt)
        {
            this.inputAngularVelocity = angularVelocity;

            if (outputNameHash == 0)
            {
                return angularVelocity;
            }

            outputAngularVelocity = angularVelocity;
            return _output.QueryAngularVelocity(outputAngularVelocity, dt);
        }


        public virtual float QueryInertia()
        {
            if (outputNameHash == 0)
            {
                return inertia;
            }

            float Ii = inertia;
            float Ia = _output.QueryInertia();
            float I = Ii + Ia;
            return I;
        }


        public virtual float ForwardStep(float torque, float inertiaSum, float dt)
        {
            inputTorque = torque;
            inputInertia = inertiaSum;

            if (outputNameHash == 0)
            {
                return torque;
            }

            outputTorque = inputTorque;
            outputInertia = inertiaSum + inertia;
            return _output.ForwardStep(outputTorque, outputInertia, dt);
        }


        public void PC_LogWarning(VehicleController vc, string message)
        {
            vc.VC_LogWarning($"{name} [{GetType()?.Name}] > {message}");
        }


        public static float TorqueToPowerInKW(in float angularVelocity, in float torque)
        {
            // Power (W) = Torque (Nm) * Angular Velocity (rad/s)
            float powerInWatts = torque * angularVelocity;

            // Convert power from watts to kilowatts
            float powerInKW = powerInWatts / 1000f;

            return powerInKW;
        }


        public static float PowerInKWToTorque(in float angularVelocity, in float powerInKW)
        {
            // Convert power from kilowatts to watts
            float powerInWatts = powerInKW * 1000f;

            // Torque (Nm) = Power (W) / Angular Velocity (rad/s)
            float absAngVel = Mathf.Abs(angularVelocity);
            float clampedAngularVelocity = absAngVel > -1f && absAngVel < 1f ? 1f : angularVelocity;
            float torque = powerInWatts / clampedAngularVelocity;
            return torque;
        }


        public float CalculateOutputPowerInKW()
        {
            return GetPowerInKW(outputTorque, outputAngularVelocity);
        }


        public static float GetPowerInKW(in float torque, in float angularVelocity)
        {
            // Power (W) = Torque (Nm) * Angular Velocity (rad/s)
            float powerInWatts = torque * angularVelocity;

            // Convert power from watts to kilowatts
            float powerInKW = powerInWatts / 1000f;

            return powerInKW;
        }


        protected static void LoadComponentFromHash(in VehicleController vc, ref PowertrainComponent component, in int hashCode)
        {
            if (component == null && hashCode != 0)
            {
                component = vc.powertrain.Inspector_GetPowertrainComponentFromNameHash(hashCode);
            }
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2
{
    public partial class PowertrainComponentDrawer : NVP_NUIPropertyDrawer
    {
        protected List<PowertrainComponent> components;
        protected List<string> componentNames;
        private int outputDropdownSelector = 0;


        public void DrawCommonProperties()
        {
            PowertrainComponent pc = SerializedPropertyHelper.GetTargetObjectOfProperty(drawer.serializedProperty) as PowertrainComponent;
            if (pc == null)
            {
                Debug.LogError("Not a PowertrainComponent");
                return;
            }

            if (Application.isPlaying)
            {
                if (pc != null)
                {
                    float inRPM = UnitConverter.AngularVelocityToRPM(pc.inputAngularVelocity);
                    float outRPM = UnitConverter.AngularVelocityToRPM(pc.outputAngularVelocity);

                    float inPower = pc.CalculateOutputPowerInKW();
                    float outPower = pc.CalculateOutputPowerInKW();

                    // Draw input values
                    drawer.Label($"IN:   {inRPM:0.0} RPM | {pc.inputTorque:0.0} Nm | " +
                        $"{inPower:0.0} kW | {pc.inputInertia:0.000} kg.m2");

                    // Draw output values
                    drawer.Label($"OUT:  {outRPM:0.0} RPM | {pc.outputTorque:0.0} Nm | " +
                        $"{outPower:0.0} kW | {pc.outputInertia:0.000} kg.m2");
                }
            }

            VehicleController vc = drawer.serializedObject.targetObject as VehicleController;
            if (vc == null)
            {
                Debug.LogError("Not a child of VehicleController");
                return;
            }

            drawer.BeginSubsection("Common Properties");
            drawer.Field("name");
            drawer.Field("inertia");

            components = vc.powertrain.Inspector_GetPowertrainComponents();
            componentNames = vc.powertrain.Inspector_GetPowertrainComponentNames();

            DrawPowertrainOutputSection(ref drawer.positionRect, vc, pc);
            drawer.EndSubsection();
        }


        public virtual void DrawPowertrainOutputSection(ref Rect rect, VehicleController vc, PowertrainComponent pc)
        {
            // Get the current input's name or set it to "none" if no input is assigned
            string currentInput = pc.Input == null ? "none" : $"{pc.Input.name}";
            drawer.Label($"Input: {currentInput}");

            // Initialize the output dropdown selector and find the index of the current output
            int initialDropdownSelector = outputDropdownSelector;
            outputDropdownSelector = componentNames.FindIndex(n => n.GetHashCode() == pc.outputNameHash);

            // Ensure the output dropdown selector has a valid index
            if (outputDropdownSelector < 0)
            {
                outputDropdownSelector = 0;
            }

            // Create the output dropdown with the list of component names
            outputDropdownSelector = EditorGUI.Popup(drawer.positionRect, "Output", outputDropdownSelector, componentNames.ToArray());

            drawer.Space(21);

            // Check if the output dropdown selection has changed
            if (outputDropdownSelector != initialDropdownSelector)
            {
                // Record the change in output for undo functionality
                Undo.RecordObject(drawer.serializedObject.targetObject, "Output change");

                // Set the new output and mark the VehicleController as dirty to save changes
                pc.Output = components[outputDropdownSelector];
                EditorUtility.SetDirty(vc);
            }
        }
    }
}

#endif
