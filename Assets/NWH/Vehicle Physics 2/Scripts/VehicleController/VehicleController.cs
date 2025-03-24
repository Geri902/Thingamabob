using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using NWH.VehiclePhysics2.Effects;
using NWH.VehiclePhysics2.Input;
using NWH.VehiclePhysics2.Modules;
using NWH.VehiclePhysics2.Powertrain;
using NWH.VehiclePhysics2.Powertrain.Wheel;
using NWH.VehiclePhysics2.Sound;
using NWH.Common.Vehicles;
using System;
using NWH.Common.AssetInfo;
using Random = UnityEngine.Random;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2
{
    /// <summary>
    ///     Main class controlling all the other parts of the vehicle.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(90)]
    public partial class VehicleController : Vehicle
    {
        public const string DEFAULT_RESOURCES_PATH = "NWH Vehicle Physics 2/Defaults/";

        public Brakes brakes = new Brakes();
        public EffectManager effectsManager = new EffectManager();
        public GroundDetection.GroundDetection groundDetection = new GroundDetection.GroundDetection();
        public VehicleInputHandler input = new VehicleInputHandler();
        public ModuleManager moduleManager = new ModuleManager();
        public Powertrain.Powertrain powertrain = new Powertrain.Powertrain();
        public SoundManager soundManager = new SoundManager();
        public Steering steering = new Steering();


        /// <summary>
        ///     State settings for the current vehicle.
        ///     State settings determine which components are enabled or disabled, as well as which LOD they belong to.
        /// </summary>
        [Tooltip(
            "State settings for the current vehicle.\r\n" +
            "State settings determine which components are enabled or disabled, as well as which LOD they belong to.")]
        public StateSettings stateSettings;


        /// <summary>
        ///     Used as a threshold value for lateral slip. When absolute lateral slip of a wheel is
        ///     lower than this value wheel is considered to have no lateral slip (wheel skid). Used mostly for effects and sound.
        /// </summary>
        [Tooltip(
            "Used as a threshold value for lateral slip. When absolute lateral slip of a wheel is\r\nlower than this value wheel is considered to have no lateral slip (wheel skid). Used mostly for effects and sound.")]
        public float lateralSlipThreshold = 0.15f;


        /// <summary>
        ///     Used as a threshold value for longitudinal slip. When absolute longitudinal slip of a wheel is
        ///     lower than this value wheel is considered to have no longitudinal slip (wheel spin). Used mostly for effects and
        ///     sound.
        /// </summary>
        [Tooltip(
            "Used as a threshold value for longitudinal slip. When absolute longitudinal slip of a wheel is\r\nlower than this value wheel is considered to have no longitudinal slip (wheel spin). Used mostly for effects and sound.")]
        public float longitudinalSlipThreshold = 0.3f;


        /// <summary>
        ///     Position of the engine relative to the vehicle. Turn on gizmos to see the marker.
        /// </summary>
        [Tooltip("    Position of the engine relative to the vehicle. Turn on gizmos to see the marker.")]
        public Vector3 enginePosition = new Vector3(0f, 0.4f, 1.5f);


        /// <summary>
        ///     Position of the exhaust relative to the vehicle. Turn on gizmos to see the marker.
        /// </summary>
        [Tooltip("    Position of the exhaust relative to the vehicle. Turn on gizmos to see the marker.")]
        public Vector3 exhaustPosition = new Vector3(0f, 0.1f, -2f);


        /// <summary>
        ///     Position of the transmission relative to the vehicle. Turn on gizmos to see the marker.
        /// </summary>
        [Tooltip("    Position of the transmission relative to the vehicle. Turn on gizmos to see the marker.")]
        public Vector3 transmissionPosition = new Vector3(0f, 0.2f, 0.2f);


        /// <summary>
        ///     Position of the engine in world coordinates. Used for effects and sound.
        /// </summary>
        public Vector3 WorldEnginePosition
        {
            get { return transform.TransformPoint(enginePosition); }
        }


        /// <summary>
        ///     Position of the exhaust in world coordinates. Used for effects and sound.
        /// </summary>
        public Vector3 WorldExhaustPosition
        {
            get { return transform.TransformPoint(exhaustPosition); }
        }


        /// <summary>
        ///     Position of the transmission in world coordinates. Used for effects and sound.
        /// </summary>
        public Vector3 WorldTransmissionPosition
        {
            get { return transform.TransformPoint(transmissionPosition); }
        }


        /// <summary>
        ///     Valid only for 4-wheeled vehicles with 2 axles (i.e. cars).
        ///     For other vehicles this value will be 0.
        /// </summary>
        [Tooltip(
            "    Valid only for 4-wheeled vehicles with 2 axles (i.e. cars).\r\n    For other vehicles this value will be 0.")]
        public float wheelbase = -1f;


        /// <summary>
        ///     Cached Time.fixedDeltaTime.
        /// </summary>
        [NonSerialized]
        [Tooltip("    Cached Time.fixedDeltaTime.")]
        public float fixedDeltaTime = 0.02f;


        /// <summary>
        ///     Cached Time.deltaTime;
        /// </summary>
        [NonSerialized]
        [Tooltip("    Cached Time.deltaTime;")]
        public float deltaTime = 0.02f;


        /// <summary>
        /// Cached version of the Time.realtimeSinceStartup
        /// </summary>
        public float realtimeSinceStartup;


        /// <summary>
        /// Should the VC_Validate() be run on each Unity Validate()?
        /// </summary>
        public bool runAutomaticValidation = true;


        /// <summary>
        ///     Called after vehicle has finished initializing.
        /// </summary>
        [NonSerialized]
        [Tooltip("    Called after vehicle has finished initializing.")]
        public UnityEvent onVehicleInitialized = new UnityEvent();

        [NonSerialized]
        private List<VehicleComponent> _components;

        private int _componentCount;

        /// <summary>
        /// Camera transform for calculating the LOD.
        /// </summary>
        private Transform _cameraTransform;

        /// <summary>
        /// True if the VehicleComponents on the vehicle have finished the initialization.
        /// Do not access the VehicleController from an outside script if this is false as 
        /// some fields might be null.
        /// </summary>
        public bool IsInitialized
        {
            get { return _isInitialized; }
        }
        protected bool _isInitialized;


        /// <summary>
        /// All the VehicleComponents present on this vehicle (SoundManager, InputHandler, etc.)
        /// </summary>
        public List<VehicleComponent> Components
        {
            get
            {
                return _components ??= new List<VehicleComponent>
                                       {
                                           input,
                                           soundManager,
                                           moduleManager,
                                           steering,
                                           powertrain,
                                           effectsManager,
                                           brakes,
                                           groundDetection,
                                       };
            }
        }


        private void Start()
        {
#if NVP2_DEBUG
            Debug.Log($"Start called on {name}.");
#endif

            Debug.Assert(transform.localScale == Vector3.one, "Vehicle scale is not 1. Vehicle scale should be [1,1,1]. " +
                                                              "To scale the vehicle use the Unity model import settings or 3D software.");

            Debug.Assert(stateSettings != null, "StateSettings are null. StateSettings are required for vehicle to function properly.");

            // Set vehicle controller reference on the components
            _componentCount = Components.Count;
            for (int i = 0; i < _componentCount; i++)
            {
                VehicleComponent component = _components[i];
                component.VC_SetVehicleController(this);
            }

            // Load states
            foreach (VehicleComponent component in _components)
            {
                component.VC_LoadStateFromStateSettings();
            }

            // Re-start the lod check as it might have been killed by disabling the behaviour
            StartCoroutine(LODCheckCoroutine());

            // Vehicle is fully initialized at this point.
            _isInitialized = true;
            onVehicleInitialized.Invoke();

            // Enable after the fact since OnEnable would otherwise run before Reset() which sets the defaults 
            // if the vehicle has been added at runtime.
            for (int i = 0; i < _componentCount; i++)
            {
                VehicleComponent component = _components[i];
                component.VC_Enable(true);
            }

            onEnable.Invoke();
        }


        public virtual void Update()
        {
            deltaTime = Time.deltaTime;
            realtimeSinceStartup = Time.realtimeSinceStartup;

            for (int i = 0; i < _componentCount; i++)
            {
                VehicleComponent component = Components[i];
                if (component.IsActive)
                {
                    component.VC_Update();
                }
            }
        }


        public override void FixedUpdate()
        {
            base.FixedUpdate();

            fixedDeltaTime = Time.fixedDeltaTime;

            for (int i = 0; i < _componentCount; i++)
            {
                VehicleComponent component = _components[i];
                if (component.IsActive)
                {
                    component.VC_FixedUpdate();
                }
            }
        }


        public override void OnEnable()
        {
#if NVP2_DEBUG
            Debug.Log($"OnEnable called on {name}.");
#endif

            base.OnEnable();

            if (!_isInitialized) return;

            // Re-start the lod check as it might have been killed by disabling the behaviour
            StartCoroutine(LODCheckCoroutine());

            for (int i = 0; i < _componentCount; i++)
            {
                VehicleComponent component = _components[i];
                component.VC_Enable(true);
            }
        }


        public override void OnDisable()
        {
#if NVP2_DEBUG
            Debug.Log($"OnDisable called on {name}.");
#endif

            base.OnDisable();

            for (int i = 0; i < _componentCount; i++)
            {
                VehicleComponent component = _components[i];
                component.VC_Disable(true);
            }

            StopAllCoroutines();
        }


        private void OnDestroy()
        {
#if NVP2_DEBUG
            Debug.Log($"OnDestroy called on {name}.");
#endif

            StopAllCoroutines();
        }


        public virtual void Reset()
        {
#if NVP2_DEBUG
            Debug.Log($"Reset called on {name}.");
#endif

            SetDefaults();
        }


        private void OnValidate()
        {
            if (runAutomaticValidation && !Application.isPlaying)
            {
                Validate();

                Debug.Assert(stateSettings != null, "StateSettings are null. Make sure to assign StateSettings " +
                    "to VehicleController (Settings tab) for the vehicle to work properly");

                Debug.Assert(stateSettings.LODs.Count > 0, "StateSetting.LODs.Count is 0. Number of LODs must be >0");
            }
        }


        /// <summary>
        ///     Resets the vehicle to default state.
        ///     Sets default values for all fields and assign default objects from resources folder.
        /// </summary>
        public virtual void SetDefaults()
        {
#if NVP2_DEBUG
            Debug.Log($"SetDefaults called on {name}.");
#endif

            foreach (VehicleComponent component in Components)
            {
                component.VC_SetVehicleController(this);
                component.VC_SetDefaults();
            }
        }


        /// <summary>
        ///     True if all of the wheels are touching the ground.
        /// </summary>
        public bool IsFullyGrounded()
        {
            int wheelCount = powertrain.wheelCount;
            for (int i = 0; i < wheelCount; i++)
            {
                if (!powertrain.wheels[i].wheelUAPI.IsGrounded)
                {
                    return false;
                }
            }

            return true;
        }


        /// <summary>
        ///     True if any of the wheels are touching ground.
        /// </summary>
        public bool IsGrounded()
        {
            int wheelCount = powertrain.wheelCount;
            for (int i = 0; i < wheelCount; i++)
            {
                if (powertrain.wheels[i].wheelUAPI.IsGrounded)
                {
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Validates the vehicle setup and outputs any issues as Debug messages.
        /// </summary>
        public void Validate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            if (transform.localScale != Vector3.one)
            {
                VC_LogWarning(
                    "VehicleController Transform scale is other than [1,1,1]. It is recommended to avoid " +
                    " scaling the vehicle parent object" +
                    " and use Scale Factor from Unity model import settings instead.");
            }

            foreach (VehicleComponent component in Components)
            {
                if (!component.state.initialized)
                {
                    component.VC_SetVehicleController(this);
                    component.VC_LoadStateFromStateSettings();
                }
                component.VC_Validate(this);
            }
        }


        public void VC_LogWarning(string message)
        {
            Debug.LogWarning($"{name} > {message}\r\n This message will show up for prefabs, too, " +
                $"so make sure to apply the changes to " +
                $"the prefab after fixing the issue, or disable the validation through Settings tab of VehicleController.");
        }


        private void OnDrawGizmosSelected()
        {
#if UNITY_EDITOR
            if (vehicleRigidbody == null)
            {
                vehicleRigidbody = GetComponent<Rigidbody>();
            }


            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(WorldEnginePosition, 0.04f);
            Handles.Label(WorldEnginePosition, new GUIContent("  Engine"));

            Gizmos.DrawWireSphere(WorldTransmissionPosition, 0.04f);
            Handles.Label(WorldTransmissionPosition, new GUIContent("  Transmission"));

            Gizmos.DrawWireSphere(WorldExhaustPosition, 0.04f);
            Handles.Label(WorldExhaustPosition, new GUIContent("  Exhaust"));

            Gizmos.color = Color.white;

            foreach (VehicleComponent component in Components)
            {
                if (component.state.isEnabled)
                {
                    component.VC_SetVehicleController(this);
                    component.VC_DrawGizmos();
                }
            }
#endif
        }
    }

#if UNITY_EDITOR
    /// <summary>
    ///     Inspector for VehicleController.
    /// </summary>
    [CustomEditor(typeof(VehicleController))]
    [CanEditMultipleObjects]
    public partial class VehicleControllerEditor : NVP_NUIEditor
    {
        private VehicleController _vc;

        public override bool OnInspectorNUI()
        {
            if (!base.OnInspectorNUI())
            {
                return false;
            }

            _vc = (VehicleController)target;

            Rect awakeButtonRect = new Rect(drawer.positionRect.x + drawer.positionRect.width - 58f,
                                            drawer.positionRect.y - 20f,
                                            56f, 15f);

            // Draw logo texture
            Rect logoRect = drawer.positionRect;
            logoRect.height = 60f;
            drawer.DrawEditorTexture(logoRect, "NWH Vehicle Physics 2/Editor/logo_bg", ScaleMode.ScaleAndCrop);
            drawer.DrawEditorTexture(
                new Rect(logoRect.x + 8f, logoRect.y + 11f, logoRect.width - 8f, logoRect.height - 22f),
                "NWH Vehicle Physics 2/Editor/logo_light", ScaleMode.ScaleToFit);
            drawer.AdvancePosition(logoRect.height);

            if (Application.isPlaying)
            {
                // Draw lod text
                Rect lodRect = awakeButtonRect;
                GUI.Label(lodRect, "LOD " + _vc.activeLODIndex, EditorStyles.whiteMiniLabel);
            }

            Rect stateSettingsRect = awakeButtonRect;
            stateSettingsRect.x -= 140f;
            stateSettingsRect.width = 200f;

            GUIStyle miniStyle = EditorStyles.whiteMiniLabel;
            miniStyle.alignment = TextAnchor.MiddleLeft;
            GUI.Label(stateSettingsRect, _vc.stateSettings?.name, miniStyle);

            drawer.Space(2);

            float tipValue = (Time.realtimeSinceStartup * 0.05f) % 1f;
            if (tipValue < 0.33f)
            {
                drawer.Info("Tip: Performance will be reduced while inspector is open.");
            }
            else if (tipValue >= 0.33f && tipValue < 0.66f)
            {
                drawer.Info("Tip: Hover the cursor over the fields to see their explanations.");
            }
            else
            {
                drawer.Info("Tip: Click the '?' next to the component name to open the documentation.");
            }

            if (Time.fixedDeltaTime >= 0.02f)
            {
                drawer.Info("Current Project Settings > Time > Fixed Timestep is higher than 0.02.\n " +
                            "For best results use 0.02 (50Hz physics update) or lower for desktop (0.016667, 0.01333 or 0.01).");
            }


            // Draw toolbar
            int categoryTab = drawer.HorizontalToolbar("categoryTab",
                                                        new[]
                                                        {
                                                        "Sound", "FX", "PWR", "Control", "Settings", "About",
                                                        }, true, true);
            drawer.Space(2);

            if (categoryTab == 0) // FX
            {
#if NVP2_FMOD
            drawer.Info("When NVP2_FMOD is defined sound settings are set through Modules > Sound > FMOD Module.", MessageType.Warning);
#else
                drawer.Property("soundManager");
#endif
            }
            else if (categoryTab == 1)
            {
                int fxTab = drawer.HorizontalToolbar("fxTab",
                                                        new[] { "Effects", "Grnd. Det." }, true, true);
                drawer.Space(2);

                if (fxTab == 0) // Effects
                {
                    drawer.Property("effectsManager");
                }
                else if (fxTab == 1)
                {
                    drawer.Property("groundDetection");
                }
            }
            else if (categoryTab == 2) // Powertrain
            {
                drawer.Property("powertrain");
            }                          // Powertrain
            else if (categoryTab == 3) // Control
            {
                int controlTab =
                    drawer.HorizontalToolbar("controlTab", new[] { "Input", "Steering", "Brakes", }, true, true);
                switch (controlTab)
                {
                    case 0:
                        DrawInputTab();
                        break;
                    case 1:
                        drawer.Property("steering");
                        break;
                    case 2:
                        drawer.Property("brakes");
                        break;
                }
            }
            else if (categoryTab == 4) // Settings
            {
                DrawSettingsTab();
            }
            else if (categoryTab == 5)
            {
                DrawAboutTab();
            }
            else
            {
                categoryTab = 0;
            }

            if (drawer.totalHeight < 820)
            {
                drawer.totalHeight = 820;
            }

            drawer.EndEditor(this);
            return true;
        }


        public override bool UseDefaultMargins()
        {
            return false;
        }


        private void DrawInputTab()
        {
            drawer.Property("input");
        }


        private void DrawAboutTab()
        {
            AssetInfo assetInfo = Resources.Load("NWH Vehicle Physics 2/NWH Vehicle Physics 2 AssetInfo") as AssetInfo;
            if (assetInfo == null)
            {
                return;
            }

            GUILayout.Space(drawer.positionRect.y - 20f);
            WelcomeMessageWindow.DrawWelcomeMessage(assetInfo, EditorGUIUtility.currentViewWidth);
        }


        private void DrawSettingsTab()
        {
            drawer.Header("Settings");

            drawer.BeginSubsection("General");
            drawer.Field("isPlayerControllable");
            drawer.EndSubsection();

            drawer.BeginSubsection("Actions");
            if (drawer.Button("Validate Setup"))
            {
                Debug.Log($"----- Starting {_vc.name} validation. -----");
                _vc.Validate();
                Debug.Log($"  => Finished vehicle validation. If no other messages or warnings popped up, the vehicle is good to go.");
            }
            drawer.Field("runAutomaticValidation");
            drawer.EndSubsection();

            drawer.BeginSubsection("State and LOD Settings");

            drawer.Field("stateSettings");
            if (_vc.stateSettings == null)
            {
                drawer.Info("StateSettings not assigned. To use component states and LODs assign StateSettings.",
                            MessageType.Warning);
            }

            drawer.Info("Individual LOD settings can be changed through StateSettings above.");
            drawer.Field("lodCamera");
            drawer.Info("If LOD Camera is left empty, Camera.main will be used.");

            if (Application.isPlaying)
            {
                drawer.Label($"Distance To Camera: {_vc.vehicleToCamDistance}m");
                string lodName = _vc.activeLOD != null ? _vc.activeLOD.name : "[not set]";
                drawer.Label($"Active LOD: {_vc.activeLODIndex} ({lodName})");
            }
            else
            {
                drawer.Info("Enter play mode to view LOD debug data.");
            }
            drawer.EndSubsection();

            drawer.BeginSubsection("Positions");
            drawer.Field("enginePosition");
            drawer.Field("transmissionPosition");
            drawer.Field("exhaustPosition");
            drawer.EndSubsection();

            drawer.BeginSubsection("Friction");
            drawer.Info("Slip threshold values are used only for effects and sound and do not affect handling.");
            drawer.Field("longitudinalSlipThreshold");
            drawer.Field("lateralSlipThreshold");
            drawer.EndSubsection();

            drawer.BeginSubsection("Debug");
            if (Application.isPlaying)
            {
                drawer.Label($"Current LOD: {_vc.activeLOD}");
            }
            else
            {
                drawer.Label("Debug data is visible only in play mode.");
            }
            drawer.EndSubsection();

            drawer.Space(50f);
        }
    }
#endif
}