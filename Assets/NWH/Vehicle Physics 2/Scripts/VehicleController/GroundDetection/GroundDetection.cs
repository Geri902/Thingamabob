using System;
using NWH.Common.Vehicles;
using NWH.VehiclePhysics2.Powertrain;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;


#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif



namespace NWH.VehiclePhysics2.GroundDetection
{
    /// <summary>
    ///     Handles surface/ground detection for the vehicle.
    /// </summary>
    [Serializable]
    public partial class GroundDetection : VehicleComponent
    {
        public GroundDetectionPreset groundDetectionPreset;
        public float groundDetectionInterval = 0.1f;

        private Terrain _activeTerrain;
        private Transform _hitTransform;
        private float[] _mix;
        private float[,,] _splatmapData;
        private TerrainData _terrainData;
        private Vector3 _terrainPos;

        private List<int> _dominanceWeighs;
        private SurfacePreset _dominantSurfacePreset;
        private Coroutine _groundDetectionCoroutine;


        public SurfacePreset DominantSurfacePreset
        {
            get { return _dominantSurfacePreset; }
        }


        protected override void VC_Initialize()
        {
            if (groundDetectionPreset == null)
            {
                Debug.LogWarning("Ground detection preset is null. Will not use GroundDetection.");
                return;
            }

            groundDetectionInterval = UnityEngine.Random.Range(groundDetectionInterval * 0.8f, groundDetectionInterval * 1.2f);
            _dominanceWeighs = new List<int>();

            base.VC_Initialize();
        }


        public override bool VC_Enable(bool calledByParent)
        {
            if (base.VC_Enable(calledByParent))
            {
                _groundDetectionCoroutine = vehicleController.StartCoroutine(GroundDetectionCoroutine());
                return true;
            }
            return false;
        }


        public override bool VC_Disable(bool calledByParent)
        {
            if (base.VC_Disable(calledByParent))
            {
                if (_groundDetectionCoroutine != null)
                {
                    vehicleController.StopCoroutine(_groundDetectionCoroutine);
                }
                return true;
            }
            return false;
        }


        private IEnumerator GroundDetectionCoroutine()
        {
            while (true)
            {
                if (groundDetectionPreset == null)
                {
                    yield return null;
                }

                int surfaceMapCount = groundDetectionPreset.surfaceMaps.Count;
                if (_dominanceWeighs.Count != surfaceMapCount)
                {
                    _dominanceWeighs.Clear();
                    for (int i = 0; i < surfaceMapCount; i++)
                    {
                        _dominanceWeighs.Add(0);
                    }
                }
                else
                {
                    for (int i = 0; i < surfaceMapCount; i++)
                    {
                        _dominanceWeighs[i] = 0;
                    }
                }

                for (int i = 0; i < vehicleController.powertrain.wheelCount; i++)
                {
                    WheelComponent wheelComponent = vehicleController.powertrain.wheels[i];
                    vehicleController.groundDetection.GetCurrentSurfaceMap(wheelComponent.wheelUAPI, ref wheelComponent.surfaceMapIndex, ref wheelComponent.surfacePreset);

                    // surfaceMapIndex is -1 if GetCurrentSurfaceMap failed
                    if (wheelComponent.surfaceMapIndex >= 0)
                    {
                        _dominanceWeighs[wheelComponent.surfaceMapIndex]++;

                        if (wheelComponent.surfacePreset.frictionPreset != null)
                        {
                            wheelComponent.wheelUAPI.FrictionPreset = wheelComponent.surfacePreset.frictionPreset;
                            wheelComponent.ApplyRollingResistanceMultiplier(wheelComponent.surfacePreset.rollingResistanceMaxMultiplier);
                        }
                    }
                    else
                    {
                        wheelComponent.wheelUAPI.FrictionPreset = groundDetectionPreset.fallbackSurfacePreset.frictionPreset;
                        wheelComponent.ApplyRollingResistanceMultiplier(groundDetectionPreset.fallbackSurfacePreset.rollingResistanceMaxMultiplier);
                    }
                }

                int maxDominanceValue = int.MinValue;
                for (int index = 0; index < _dominanceWeighs.Count; index++)
                {
                    int weigh = _dominanceWeighs[index];
                    maxDominanceValue = Math.Max(maxDominanceValue, weigh);
                }

                if (maxDominanceValue == 0)
                {
                    _dominantSurfacePreset = null;
                }
                else
                {
                    int dominantSurfaceIndex = _dominanceWeighs.IndexOf(maxDominanceValue);
                    if (dominantSurfaceIndex >= 0)
                    {
                        _dominantSurfacePreset = groundDetectionPreset.surfaceMaps[dominantSurfaceIndex].surfacePreset;
                    }
                    else
                    {
                        _dominantSurfacePreset = null;
                    }
                }

                yield return new WaitForSeconds(groundDetectionInterval);
            }
        }


        public override void VC_SetDefaults()
        {
            base.VC_SetDefaults();

            if (groundDetectionPreset == null)
            {
                groundDetectionPreset =
                    Resources.Load(VehicleController.DEFAULT_RESOURCES_PATH + "DefaultGroundDetectionPreset")
                        as GroundDetectionPreset;
            }
        }


        public override void VC_Validate(VehicleController vc)
        {
            base.VC_Validate(vc);

            Debug.Assert(groundDetectionPreset != null, $"{vc.name}: GroundDetectionPreset is required but is null. " +
                                                        "Go to VehicleController > FX > Grnd. Det. and " +
                                                        "assign a GroundDetectionPreset.");

            if (groundDetectionPreset != null)
            {
                Debug.Assert(groundDetectionPreset.fallbackSurfacePreset != null,
                             $"{vc.name}: Fallback Surface Preset is not assigned " +
                             $"for {groundDetectionPreset.name}. Fallback Surface Preset is the only required" +
                             " SurfacePreset. Go to VehicleController > FX > Grnd. Det. and " +
                             "assign a Fallback Surface Preset.");
            }
        }


        /// <summary>
        ///     Gets the surface map the wheel is currently on.
        /// </summary>
        public void GetCurrentSurfaceMap(WheelUAPI wheelController, ref int surfaceIndex, ref SurfacePreset outSurfacePreset)
        {
            outSurfacePreset = groundDetectionPreset?.fallbackSurfacePreset;
            surfaceIndex = -1;
            outSurfacePreset = null;

            if (!state.isEnabled)
            {
                return;
            }

            if (groundDetectionPreset == null)
            {
                Debug.LogError(
                    "GroundDetectionPreset is required but is null. Go to VehicleController > FX > Grnd. Det. and " +
                    "assign a GroundDetectionPreset.");
                return;
            }

            if (wheelController.HitCollider == null)
            {
                return;
            }

            _hitTransform = wheelController.HitCollider.transform;
            if (wheelController.IsGrounded && _hitTransform != null)
            {
                // Check for tags
                int mapCount = groundDetectionPreset.surfaceMaps.Count;
                for (int e = 0; e < mapCount; e++)
                {
                    SurfaceMap map = groundDetectionPreset.surfaceMaps[e];
                    int tagCount = map.tags.Count;

                    for (int i = 0; i < tagCount; i++)
                    {
                        if (_hitTransform.tag == map.tags[i])
                        {
                            outSurfacePreset = map.surfacePreset;
                            surfaceIndex = e;
                            return;
                        }
                    }
                }

                // Find active terrain
                _activeTerrain = _hitTransform.GetComponent<Terrain>();
                if (_activeTerrain)
                {
                    // Check for terrain textures
                    int dominantTerrainIndex = GetDominantTerrainTexture(wheelController.HitPoint, _activeTerrain);
                    if (dominantTerrainIndex != -1)
                    {
                        int surfaceMapCount = groundDetectionPreset.surfaceMaps.Count;
                        for (int e = 0; e < surfaceMapCount; e++)
                        {
                            SurfaceMap map = groundDetectionPreset.surfaceMaps[e];

                            int n = map.terrainTextureIndices.Count;
                            for (int i = 0; i < n; i++)
                            {
                                if (map.terrainTextureIndices[i] == dominantTerrainIndex)
                                {
                                    outSurfacePreset = map.surfacePreset;
                                    surfaceIndex = e;
                                    return;
                                }
                            }
                        }
                    }
                }
            }

            if (groundDetectionPreset.fallbackSurfacePreset != null)
            {
                outSurfacePreset = groundDetectionPreset.fallbackSurfacePreset;
                surfaceIndex = -1;
                return;
            }

            Debug.LogError(
                $"Fallback surface map of ground detection preset {groundDetectionPreset.name} not assigned.");
            outSurfacePreset = null;
            surfaceIndex = -1;
            return;
        }


        /// <summary>
        ///     Returns most prominent texture at the point in a terrain.
        /// </summary>
        public int GetDominantTerrainTexture(Vector3 worldPos, Terrain terrain)
        {
            // returns the zero-based surfaceIndex of the most dominant texture
            // on the main terrain at this world position.
            GetTerrainTextureComposition(worldPos, terrain, ref _mix);
            if (_mix != null)
            {
                float maxMix = 0;
                int maxIndex = 0;
                // loop through each mix value and find the maximum
                for (int n = 0; n < _mix.Length; ++n)
                {
                    if (_mix[n] > maxMix)
                    {
                        maxIndex = n;
                        maxMix = _mix[n];
                    }
                }

                return maxIndex;
            }

            return -1;
        }


        public void GetTerrainTextureComposition(Vector3 worldPos, Terrain terrain, ref float[] cellMix)
        {
            _terrainData = terrain.terrainData;
            _terrainPos = terrain.transform.position;

            int alphamapWidth = _terrainData.alphamapWidth;
            int alphamapHeight = _terrainData.alphamapHeight;

            // Calculate which splat map cell the worldPos falls within (ignoring y)
            int mapX = (int)((worldPos.x - _terrainPos.x) / _terrainData.size.x * alphamapWidth);
            int mapZ = (int)((worldPos.z - _terrainPos.z) / _terrainData.size.z * alphamapHeight);

            mapX = Mathf.Clamp(mapX, 0, alphamapWidth - 1);
            mapZ = Mathf.Clamp(mapZ, 0, alphamapHeight - 1);

            // Get the splat data for this cell as a 1x1xN 3d array (where N = number of textures)
            _splatmapData = _terrainData.GetAlphamaps(mapX, mapZ, 1, 1);
            // Extract the 3D array data to a 1D array:
            cellMix = new float[_splatmapData.GetUpperBound(2) + 1];
            for (int n = 0; n < cellMix.Length; ++n)
            {
                cellMix[n] = _splatmapData[0, 0, n];
            }
        }


        public override void VC_DrawGizmos()
        {
            base.VC_DrawGizmos();

#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                Handles.color = Color.yellow;

                for (int i = 0; i < vehicleController.powertrain.wheelCount; i++)
                {
                    WheelComponent wheelComponent = vehicleController.powertrain.wheels[i];
                    Handles.Label(wheelComponent.wheelUAPI.transform.position, $"  SP: {wheelComponent.surfacePreset?.name}");
                }
            }
#endif
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.GroundDetection
{
    [CustomPropertyDrawer(typeof(GroundDetection))]
    public partial class GroundDetectionDrawer : ComponentNUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.BeginSubsection("Debug Info");

            GroundDetection groundDetection =
                SerializedPropertyHelper.GetTargetObjectOfProperty(drawer.serializedProperty) as GroundDetection;
            if (groundDetection != null && groundDetection.vehicleController != null)
            {
                drawer.Label($"Dominant surface: {groundDetection.DominantSurfacePreset?.name}");
                drawer.Space();

                for (int i = 0; i < groundDetection.vehicleController.powertrain.wheelCount; i++)
                {
                    WheelComponent wheelComponent = groundDetection.vehicleController.powertrain.wheels[i];
                    if (wheelComponent != null)
                    {
                        drawer.Label($"{wheelComponent.name}: {wheelComponent.surfacePreset?.name}");
                    }
                }
            }
            else
            {
                drawer.Info("Debug info is available only in play mode.");
            }
            drawer.EndSubsection();

            drawer.BeginSubsection("Settings");
            drawer.Field("groundDetectionInterval", true, "s");
            drawer.Field("groundDetectionPreset");

            GroundDetectionPreset gdPreset =
                ((GroundDetection)(SerializedPropertyHelper.GetTargetObjectOfProperty(property)
                                        as VehicleComponent))?.groundDetectionPreset;

            if (gdPreset != null)
            {
                drawer.EmbeddedObjectEditor<NVP_NUIEditor>(gdPreset, drawer.positionRect);
            }
            drawer.EndSubsection();


            drawer.EndProperty();
            return true;
        }
    }
}

#endif
