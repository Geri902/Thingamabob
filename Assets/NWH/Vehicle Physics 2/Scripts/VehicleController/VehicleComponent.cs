using System;
using NWH.VehiclePhysics2.Sound.SoundComponents;
using UnityEngine;

namespace NWH.VehiclePhysics2
{
    /// <summary>
    ///     Base class for all VehicleComponents.
    /// </summary>
    [Serializable]
    public abstract partial class VehicleComponent
    {
        [NonSerialized]
        public VehicleController vehicleController;

        /// <summary>
        ///     Contains info about component's state.
        /// </summary>
        [Tooltip("    Contains info about component's state.")]
        [NonSerialized]
        public StateDefinition state = new StateDefinition();

        private bool _wasDisabledByParent = false;

        /// <summary>
        /// Same as IsOnAndEnabled, but checks for initialized too.
        /// Can be replaced by state.isEnabled.
        /// </summary> // TODO
        public bool IsActive
        {
            get { return state.isEnabled && state.initialized; }
        }


        public virtual void VC_SetVehicleController(VehicleController vc)
        {
            this.vehicleController = vc;

#if NVP2_DEBUG
            //Debug.Log("VC_SetVehicleController() " + GetType().Name);
            Debug.Assert(vc != null, $"VehicleController is null for component {GetType().Name}");
#endif
        }


        public virtual void VC_LoadStateFromStateSettings()
        {
#if NVP2_DEBUG
            //Debug.Log("VC_LoadState() " + GetType().Name);
            Debug.Assert(vehicleController != null, $"VehicleController is null for component {GetType().Name}");
#endif

            string fullName = GetType().FullName;
            LoadStateFromDefinitionsFile(fullName, ref state);
        }


        /// <summary>
        ///     Called when the component is first enabled.
        /// </summary>
        protected virtual void VC_Initialize()
        {
#if NVP2_DEBUG
            Debug.Log("VC_Initialize() " + GetType().Name);
            Debug.Assert(vehicleController != null, $"VehicleController is null for component {GetType().Name}");
#endif

            state.initialized = true;
        }


        /// <summary>
        ///     Equivalent to MonoBehaviour's Update() function.
        /// </summary>
        public virtual void VC_Update()
        {
#if NVP2_DEBUG
            Debug.Assert(state.initialized, "VC_Update() called before VC_Initialize()");
            Debug.Assert(state.isEnabled, "VC_Update called on a VehicleComponent that is disabled");
#endif
        }


        /// <summary>
        ///     Equivalent to MonoBehavior's FixedUpdate().
        /// </summary>
        public virtual void VC_FixedUpdate()
        {
#if NVP2_DEBUG
            Debug.Assert(state.initialized, "VC_FixedUpdate() called before VC_Initialize()");
            Debug.Assert(state.isEnabled, "VC_FixedUpdate called on a VehicleComponent that is disabled");
#endif
        }


        /// <summary>
        ///     Call to enable the vehicle component.
        ///     Not called outside of play mode.
        /// </summary>
        /// <returns>True if successful.</returns>
        public virtual bool VC_Enable(bool calledByParent)
        {
#if NVP2_DEBUG
            Debug.Log($"Enable {GetType().Name}");
            Debug.Assert(vehicleController != null, "VehicleController is null!");
#endif
            if (!state.isEnabled && calledByParent && !_wasDisabledByParent)
            {
                return false;
            }

            // Trying to enabled non-initialized component. Initialize it first.
            if (!state.initialized)
            {
                // Try to initialize.
                VC_Initialize();

                // Initialization failed. Turn off the component and exit.
                if (!state.initialized)
                {
                    state.isEnabled = false;
                    return false;
                }
            }

            state.isEnabled = true;
            return true;
        }


        /// <summary>
        ///     Call to disable the vehicle component.
        ///     Will get called when component's Enabled/Disabled toggle button gets clicked in editor.
        /// </summary>
        /// <returns>True if successful.</returns>
        public virtual bool VC_Disable(bool calledByParent)
        {
#if NVP2_DEBUG
            Debug.Log($"Disable {GetType().Name}");
            Debug.Assert(vehicleController != null, "VehicleController is null!");
#endif
            if (!state.initialized)
            {
                return false;
            }

            // Do not disable if already disabled.
            if (!state.isEnabled)
            {
                return false;
            }

            _wasDisabledByParent = calledByParent;
            state.isEnabled = false;
            return true;
        }


        /// <summary>
        ///     Equivalent to MonoBehavior's DrawGizmos().
        /// </summary>
        public virtual void VC_DrawGizmos()
        {
#if NVP2_DEBUG
            Debug.Assert(state.isEnabled, "VC_DrawGizmos called on a VehicleComponent that is disabled");
#endif
        }


        /// <summary>
        ///     Resets component's values to defaults. Also called when Reset is called from inspector.
        /// </summary>
        public virtual void VC_SetDefaults()
        {
#if NVP2_DEBUG
            //Debug.Log("VC_SetDefaults() " + GetType().Name);
            Debug.Assert(vehicleController != null);
#endif
        }


        /// <summary>
        ///     Ran when VehicleController.VC_Validate is called.
        ///     Checks if the component setup is valid and alerts the developer if there are any issues.
        /// </summary>
        public virtual void VC_Validate(VehicleController vc)
        {
#if NVP2_DEBUG
            //Debug.Log("VC_Validate() " + GetType().Name);
            Debug.Assert(vc != null);
#endif
        }


        /// <summary>
        ///     Loads state settings from StateSettings ScriptableObject.
        /// </summary>
        private void LoadStateFromDefinitionsFile(string fullTypeName, ref StateDefinition state)
        {
            if (vehicleController.stateSettings == null)
            {
                return;
            }

            StateDefinition loadedState = vehicleController.stateSettings.GetDefinition(fullTypeName);
            if (loadedState != null)
            {
                state.isEnabled = loadedState.isEnabled;
                state.lodIndex = loadedState.lodIndex;
                state.fullName = fullTypeName;
            }
            else
            {
                Debug.Log(
                    $"State definition {fullTypeName} could not be loaded. Refreshing the list of available components.");
                vehicleController.stateSettings?.Reload();
            }
        }


        /// <summary>
        ///     Checks the current state and enables or disables the component if needed.
        ///     Also handles LOD checking.
        /// </summary>
        public virtual void UpdateLOD()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning($"Trying to run UpdateState out of play mode on {GetType().Name}.");
                return;
            }

            if (vehicleController == null)
            {
                Debug.LogWarning($"Trying to run UpdateState with no VehicleController reference set on {GetType().Name}.");
                return;
            }

            // LOD
            bool useLOD = state.lodIndex >= 0;
            if (useLOD)
            {
                bool isInsideLOD = vehicleController.activeLODIndex <= state.lodIndex;
                if (isInsideLOD)
                {
                    if (!state.isEnabled) VC_Enable(false);
                }
                else
                {
                    if (state.isEnabled) VC_Disable(false);
                }
            }
        }


        /// <summary>
        /// If enabled disables the component and vice versa.
        /// Should be used only during play mode.
        /// </summary>
        public virtual void ToggleState()
        {
            if (state.isEnabled)
            {
                VC_Disable(false);
            }
            else
            {
                VC_Enable(false);
            }
        }
    }
}