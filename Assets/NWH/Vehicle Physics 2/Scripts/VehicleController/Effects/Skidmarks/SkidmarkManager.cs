using System;
using System.Collections.Generic;
using NWH.VehiclePhysics2.GroundDetection;
using NWH.VehiclePhysics2.Powertrain;
using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Effects
{
    [Serializable]
    public partial class SkidmarkManager : Effect
    {
        /// <summary>
        ///     Higher value will give darker skidmarks for the same slip. Check corresponding SurfacePreset (GroundDetection ->
        ///     Presets)
        ///     for per-surface settings.
        /// </summary>
        [Range(0, 5)]
        [Tooltip(
            "Higher value will give darker skidmarks for the same slip. Check corresponding SurfacePreset (GroundDetection -> Presets)\r\nfor per-surface settings.")]
        public float globalSkidmarkIntensity = 0.6f;

        /// <summary>
        ///     Height above ground at which skidmarks will be drawn. If too low clipping between skidmark and ground surface will
        ///     occur.
        /// </summary>
        [Tooltip(
            "Height above ground at which skidmarks will be drawn. If too low clipping between skidmark and ground surface will\r\noccur.")]
        public float groundOffset = 0.025f;

        /// <summary>
        ///     When skidmark alpha value is below this value skidmark mesh will not be generated.
        /// </summary>
        [Tooltip("    When skidmark alpha value is below this value skidmark mesh will not be generated.")]
        public float lowerIntensityThreshold = 0.05f;

        /// <summary>
        ///     Number of triangles that will be drawn per one section, before mesh is saved and new one is generated.
        /// </summary>
        [Tooltip(
            "Number of triangles that will be drawn per one section, before mesh is saved and new one is generated.")]
        public int maxTrisPerSection = 300;

        /// <summary>
        /// Total number of skidmark mesh triangles per wheel before the oldest skidmark section gets destroyed.
        /// </summary>
        [UnityEngine.Tooltip("Total number of skidmark mesh triangles per wheel before the oldest skidmark section gets destroyed.")]
        public int maxTotalTris = 1440;

        /// <summary>
        ///     Max skidmark texture alpha.
        /// </summary>
        [Range(0, 1)]
        [Tooltip("    Max skidmark texture alpha.")]
        public float maxSkidmarkAlpha = 0.6f;

        /// <summary>
        ///     Distance from the last skidmark section needed to generate a new one.
        /// </summary>
        [Tooltip("    Distance from the last skidmark section needed to generate a new one.")]
        public float minDistance = 0.12f;

        /// <summary>
        ///     Skidmarks get deleted when distance from the parent vehicle is higher than this.
        /// </summary>
        [UnityEngine.Tooltip("    Skidmarks get deleted when distance from the parent vehicle is higher than this.")]
        public float skidmarkDestroyDistance = 100f;

        /// <summary>
        ///     Time after which the skidmark will get destroyed. Set to 0 to disable.
        /// </summary>
        [UnityEngine.Tooltip("    Time after which the skidmark will get destroyed. Set to 0 to disable.")]
        public float skidmarkDestroyTime = 0f;

        /// <summary>
        /// Game object that contains all the skidmark objects.
        /// </summary>
        [UnityEngine.Tooltip("Game object that contains all the skidmark objects.")]
        public GameObject skidmarkContainer;

        /// <summary>
        /// Material that will be used if no material is assigned to current surface or current surface is null.
        /// </summary>
        [UnityEngine.Tooltip("Material that will be used if no material is assigned to current surface or current surface is null.")]
        public Material fallbackMaterial;

        private int _prevWheelCount;
        private List<SkidmarkGenerator> _skidmarkGenerators = new List<SkidmarkGenerator>();
        private Coroutine _updateCoroutine;


        public override bool VC_Enable(bool calledByParent)
        {
            if (base.VC_Enable(calledByParent))
            {
                InitializeSkidmarks();
                _updateCoroutine = vehicleController.StartCoroutine(SkidmarkUpdateCoroutine());
                return true;
            }

            return false;
        }


        public override bool VC_Disable(bool calledByParent)
        {
            if (base.VC_Disable(calledByParent))
            {
                if (_updateCoroutine != null) vehicleController.StopCoroutine(_updateCoroutine);
                return true;
            }

            return false;
        }


        private void InitializeSkidmarks()
        {
            if (vehicleController.groundDetection.groundDetectionPreset == null)
            {
                Debug.LogWarning($"Trying to use SkidmarkManager without a GroundDetectionPreset assigned to the " +
                                 $"vehicle {vehicleController.name}");
                return;
            }

            skidmarkContainer = GameObject.Find("SkidContainer");
            if (skidmarkContainer == null)
            {
                skidmarkContainer = new GameObject("SkidContainer");
                skidmarkContainer.isStatic = true;
            }

            fallbackMaterial = vehicleController.groundDetection.groundDetectionPreset.fallbackSurfacePreset
                                          .skidmarkMaterial;
            List<Material> materials = new List<Material>();

            int mapCount = vehicleController.groundDetection.groundDetectionPreset.surfaceMaps.Count;
            for (int i = 0; i < mapCount; i++)
            {
                SurfaceMap surfaceMap = vehicleController.groundDetection.groundDetectionPreset.surfaceMaps[i];
                materials.Add(surfaceMap.surfacePreset.skidmarkMaterial);
            }

            _skidmarkGenerators = new List<SkidmarkGenerator>();
            for (int i = 0; i < vehicleController.powertrain.wheelCount; i++)
            {
                WheelComponent wheelComponent = vehicleController.powertrain.wheels[i];
                SkidmarkGenerator skidmarkGenerator = new SkidmarkGenerator();
                skidmarkGenerator.Initialize(this, wheelComponent);
                _skidmarkGenerators.Add(skidmarkGenerator);
            }

            float minPersistentDistance = maxTrisPerSection * minDistance * 0.75f;
            if (skidmarkDestroyDistance < minPersistentDistance)
            {
                skidmarkDestroyDistance = minPersistentDistance;
            }

            if (maxTrisPerSection * 2 > maxTotalTris)
            {
                maxTotalTris = maxTrisPerSection * 2 + 1;
                Debug.LogWarning("MaxTotalTris must be at least double the value of MaxTrisPerSection. Adjusting.");
            }

            _prevWheelCount = vehicleController.powertrain.wheelCount;
        }



        public IEnumerator SkidmarkUpdateCoroutine()
        {
            float dt = 0.05f;

            while (true)
            {
                yield return new WaitForSeconds(dt);

                if (!IsActive)
                {
                    continue;
                }

                // Check if can be updated
                if (!vehicleController.groundDetection.state.isEnabled)
                {
                    continue;
                }

                // Check for added/removed wheels and re-init if needed
                if (_prevWheelCount != vehicleController.powertrain.wheelCount ||
                    _skidmarkGenerators.Count != vehicleController.powertrain.wheelCount)
                {
                    InitializeSkidmarks();
                }
                _prevWheelCount = vehicleController.powertrain.wheelCount;

                Debug.Assert(_skidmarkGenerators.Count == vehicleController.powertrain.wheels.Count,
                    $"Wheel count does not match generator count. Wheels {vehicleController.powertrain.wheels.Count} |" +
                    $" Generators {_skidmarkGenerators.Count} on {vehicleController.name}");

                // Update the skidmark generators
                int n = _skidmarkGenerators.Count;
                for (int i = 0; i < n; i++)
                {
                    WheelComponent wheelComponent = vehicleController.powertrain.wheels[i];
                    SurfacePreset surfacePreset = wheelComponent.surfacePreset;
                    float intensity = 0f;
                    if (surfacePreset != null && surfacePreset.drawSkidmarks)
                    {
                        float latFactor = Mathf.Max(0, wheelComponent.wheelUAPI.NormalizedLateralSlip - vehicleController.lateralSlipThreshold);
                        float lonFactor = Mathf.Max(0, wheelComponent.wheelUAPI.NormalizedLongitudinalSlip - vehicleController.longitudinalSlipThreshold);

                        float slipIntensity = latFactor + lonFactor;
                        float weightCoeff = Mathf.Clamp((wheelComponent.wheelUAPI.Load * 3f) / wheelComponent.wheelUAPI.MaxLoad, 0f, 1f);
                        slipIntensity *= wheelComponent.surfacePreset.slipFactor * weightCoeff;

                        intensity = Mathf.Clamp(wheelComponent.surfacePreset.skidmarkBaseIntensity + slipIntensity, 0f, 1f);
                        intensity *= globalSkidmarkIntensity;
                        intensity = Mathf.Clamp(intensity, 0f, maxSkidmarkAlpha);
                    }

                    _skidmarkGenerators[i].Update(wheelComponent.surfaceMapIndex, intensity, dt);
                }
            }
        }
    }
}


#if UNITY_EDITOR

namespace NWH.VehiclePhysics2.Effects
{
    [CustomPropertyDrawer(typeof(SkidmarkManager))]
    public partial class SkidmarkManagerDrawer : ComponentNUIPropertyDrawer
    {
        private float infoHeight;


        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.BeginSubsection("Geometry");
            drawer.Field("minDistance", true, "m");
            drawer.Field("groundOffset", true, "m");
            drawer.EndSubsection();

            drawer.BeginSubsection("Lifetime");
            drawer.Field("maxTrisPerSection");
            drawer.Field("maxTotalTris");
            drawer.Field("skidmarkDestroyTime");
            drawer.Field("skidmarkDestroyDistance");
            drawer.EndSubsection();

            drawer.BeginSubsection("Appearance");
            drawer.Field("globalSkidmarkIntensity");
            drawer.Field("maxSkidmarkAlpha");
            drawer.Field("lowerIntensityThreshold");
            drawer.Info("To change appearance of skidmarks on different surfaces, check GroundDetection preset.");
            drawer.EndSubsection();

            drawer.EndProperty();
            return true;
        }
    }
}
#endif