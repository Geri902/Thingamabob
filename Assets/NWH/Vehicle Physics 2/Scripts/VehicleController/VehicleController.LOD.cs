using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace NWH.VehiclePhysics2
{
    /// <summary>
    ///     LOD-related VehicleController code.
    /// </summary>
    public partial class VehicleController
    {
        /// <summary>
        ///     Distance between camera and vehicle used for determining LOD.
        /// </summary>
        [NonSerialized]
        [Tooltip("    Distance between camera and vehicle used for determining LOD.")]
        public float vehicleToCamDistance;

        /// <summary>
        ///     Currently active LOD.
        /// </summary>
        [NonSerialized]
        [Tooltip("    Currently active LOD.")]
        public LOD activeLOD;

        /// <summary>
        ///     Currently active LOD index.
        /// </summary>
        [NonSerialized]
        [Tooltip("    Currently active LOD index.")]
        public int activeLODIndex = -2;

        /// <summary>
        ///     Camera from which the LOD distance will be measured.
        ///     To use Camera.main instead, leave empty/null
        /// </summary>
        [FormerlySerializedAs("LODCamera")]
        [Tooltip(
            "Camera from which the LOD distance will be measured.\r\n" +
            "To use Camera.main instead, leave empty/null.")]
        public Camera lodCamera;

        /// <summary>
        /// Called when active LOD is changed.
        /// </summary>
        [NonSerialized]
        [Tooltip("Called when active LOD is changed.")]
        public UnityEvent onLODChanged = new UnityEvent();

        /// <summary>
        /// Updates the currently active LOD.
        /// </summary>
        private IEnumerator LODCheckCoroutine()
        {
            while (true)
            {
                if (!LODCheck())
                {
                    break;
                }

                // Check for failed initialization
                if (activeLODIndex == -2)
                {
                    Debug.LogWarning($"LOD is -2 meaning that the vehicle {name} initialization failed!");
                    break;
                }

                UpdateComponentLODs();

                yield return new WaitForSeconds(0.2f);
            }

            yield return null;
        }


        /// <summary>
        /// Runs the state check on all the components, determining
        /// if they should be enabled or disabled.
        /// </summary>
        protected virtual void UpdateComponentLODs()
        {
            for (int i = 0; i < Components.Count; i++)
            {
                VehicleComponent component = Components[i];
                component.UpdateLOD();
            }
        }



        private bool LODCheck()
        {
            if (stateSettings == null)
            {
                Debug.LogError("StateSettings are null. Exiting LOD check!");
                return false;
            }

            int initLODIndex = activeLODIndex;
            int lodCount = stateSettings.LODs.Count;
            if (lodCount == 0)
            {
                Debug.LogError("Lod count is 0!");
                return false;
            }

            // Vehicle is enabled, determine LOD based on distance
            Camera currentCamera = lodCamera;
            if (currentCamera == null)
            {
                currentCamera = Camera.main;
            }

            // Still null, exit.
            if (currentCamera == null)
            {
                Debug.LogWarning("LOD camera is null. Make sure that there is a camera with tag " +
                                 "'MainCamera' in the scene and/or that the vehicle cameras have this tag.");
                return false;
            }
            _cameraTransform = currentCamera.transform;

            vehicleToCamDistance = Vector3.Distance(vehicleTransform.position, _cameraTransform.position);
            for (int i = 0; i < lodCount; i++)
            {
                if (stateSettings.LODs[i].distance > vehicleToCamDistance)
                {
                    activeLODIndex = i;
                    activeLOD = stateSettings.LODs[i];
                    break;
                }
            }

            if (activeLODIndex != initLODIndex)
            {
                onLODChanged.Invoke();
            }

            return true;
        }
    }
}
