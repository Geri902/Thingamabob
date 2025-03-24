using System;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif



namespace NWH.VehiclePhysics2.SetupWizard
{
    /// <summary>
    ///     A ScriptableObject representing a set of SurfaceMaps. Usually one per scene or project.
    /// </summary>
    [Serializable]
    [CreateAssetMenu(fileName = "NWH Vehicle Physics 2", menuName = "NWH/Vehicle Physics 2/Vehicle Setup Wizard Preset",
                     order = 1)]
    public partial class VehicleSetupWizardPreset : ScriptableObject
    {
        public enum VehicleType
        {
            Car,
            SportsCar,
            OffRoad,
            MonsterTruck,
            SemiTruck,
            Trailer,
            Motorcycle
        }

        public enum DrivetrainConfiguration
        {
            FWD,
            AWD,
            RWD
        }

        // General
        [UnityEngine.Tooltip("General")]
        public VehicleType vehicleType = VehicleType.Car;

        // Physical properties
        [UnityEngine.Tooltip("Physical properties")]
        public float mass = 1500f;
        public float width = 1.8f;
        public float length = 4.5f;
        public float height = 1.4f;

        // Engine
        [Range(10, 600)]
        [UnityEngine.Tooltip("Engine")]
        public float enginePower = 110f;
        public float engineMaxRPM = 6000f;

        // Transmission
        [UnityEngine.Tooltip("Transmission")]
        public float transmissionGearing = 1f;

        // Drivetrain
        [UnityEngine.Tooltip("Drivetrain")]
        public DrivetrainConfiguration drivetrainConfiguration = DrivetrainConfiguration.RWD;

        // Suspension
        [FormerlySerializedAs("suspensionTravel")] public float suspensionTravelCoeff = 1f;
        [FormerlySerializedAs("suspensionStiffness")] public float suspensionStiffnessCoeff = 1f;
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.SetupWizard
{
    [CustomEditor(typeof(VehicleSetupWizardPreset))]
    [CanEditMultipleObjects]
    public partial class VehicleSetupWizardPresetEditor : NVP_NUIEditor
    {
        public override bool OnInspectorNUI()
        {
            if (!base.OnInspectorNUI())
            {
                return false;
            }

            drawer.BeginSubsection("General");
            drawer.Field("vehicleType");
            drawer.EndSubsection();

            drawer.BeginSubsection("Physical Properties");
            drawer.Field("mass", true, "kg");
            drawer.Field("width", true, "m");
            drawer.Field("length", true, "m");
            drawer.Field("height", true, "m");
            drawer.EndSubsection();

            drawer.BeginSubsection("Powertrain");
            drawer.Field("enginePower", true, "kW");
            drawer.Field("engineMaxRPM");
            drawer.FloatSlider("transmissionGearing", 0.5f, 1.5f, "Long", "Short", true);
            drawer.Field("drivetrainConfiguration");
            drawer.EndSubsection();

            drawer.BeginSubsection("Suspension");
            drawer.FloatSlider("suspensionTravelCoeff", 0.5f, 2f, "Short", "Long", true);
            drawer.FloatSlider("suspensionStiffnessCoeff", 0.6f, 1.4f, "Soft", "Stiff", true);
            drawer.EndSubsection();

            drawer.EndEditor(this);
            return true;
        }


        public override bool UseDefaultMargins()
        {
            return false;
        }
    }
}

#endif
