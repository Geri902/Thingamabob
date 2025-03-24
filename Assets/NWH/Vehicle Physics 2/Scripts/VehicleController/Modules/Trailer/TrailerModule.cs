using System;
using UnityEngine;
using UnityEngine.Events;
using NWH.VehiclePhysics2.Powertrain;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Modules.Trailer
{
    [Serializable]
    public partial class TrailerModule : VehicleComponent
    {
        /// <summary>
        ///     True if object is trailer and is attached to a towing vehicle and also true if towing vehicle and has trailer
        ///     attached.
        /// </summary>
        [Tooltip(
            "True if object is trailer and is attached to a towing vehicle and also true if towing vehicle and has trailer\r\nattached.")]
        public bool attached;

        /// <summary>
        ///     If the vehicle is a trailer, this is the object placed at the point at which it will connect to the towing vehicle.
        ///     If the vehicle is towing, this is the object placed at point at which trailer will be coneected.
        /// </summary>
        [Tooltip(
            "If the vehicle is a trailer, this is the object placed at the point at which it will connect to the towing vehicle." +
            " If the vehicle is towing, this is the object placed at point at which trailer will be coneected.")]
        public Transform attachmentPoint;

        /// <summary>
        /// The layer of the SphereCollider used to detect if the trailer hitch module is in range.
        /// </summary>
        public int attachmentLayer = 0;

        /// <summary>
        /// The radius of the SphereCollider used to detect if the trailer hitch module is in range.
        /// </summary>
        public float attachmentTriggerRadius = 0.2f;

        /// <summary>
        /// Called when the trailer is attached to a hitch.
        /// </summary>
        public UnityEvent onAttach = new UnityEvent();

        /// <summary>
        /// Called when the trailer is detached from a hitch.
        /// </summary>
        public UnityEvent onDetach = new UnityEvent();

        /// <summary>
        ///     Should the trailer input states be reset when trailer is detached?
        /// </summary>
        [Tooltip("    Should the trailer input states be reset when trailer is detached?")]
        public bool resetInputStatesOnDetach = true;

        /// <summary>
        ///     If enabled the trailer will keep in same gear as the tractor, assuming powertrain on trailer is enabled.
        /// </summary>
        [Tooltip(
            "If enabled the trailer will keep in same gear as the tractor, assuming powertrain on trailer is enabled.")]
        public bool synchronizeGearShifts = false;

        /// <summary>
        ///     Object that will be disabled when trailer is attached and disabled when trailer is detached.
        /// </summary>
        [Tooltip("    Object that will be disabled when trailer is attached and disabled when trailer is detached.")]
        public GameObject trailerStand;

        [NonSerialized]
        public TrailerHitchModule trailerHitch;


        protected override void VC_Initialize()
        {
            vehicleController.input.autoSetInput = false;

            base.VC_Initialize();
        }


        public override void VC_FixedUpdate()
        {
            base.VC_FixedUpdate();
            if (trailerHitch == null)
            {
                return;
            }

            if (attached)
            {
                // Make sure that the ratio is the same for flip input check.
                vehicleController.powertrain.transmission.Gear = trailerHitch.vehicleController.powertrain.transmission.Gear;

                if (synchronizeGearShifts)
                {
                    Debug.Assert(trailerHitch.vehicleController.powertrain.transmission.forwardGearCount == vehicleController.powertrain.transmission.forwardGearCount &&
                                 trailerHitch.vehicleController.powertrain.transmission.reverseGearCount == vehicleController.powertrain.transmission.reverseGearCount,
                        "When TrailerModule.synchronizeGearShifts is enabled make sure that both truck and trailer have the same number of forward and reverse gears or" +
                        " disable this option.");
                    vehicleController.powertrain.transmission.ShiftInto(trailerHitch.vehicleController.powertrain.transmission.Gear);
                }
            }
        }



        public void OnAttach(TrailerHitchModule trailerHitch)
        {
            Debug.Assert(vehicleController != null);

            this.trailerHitch = trailerHitch;
            this.trailerHitch.vehicleController.onEnable.AddListener(EnableTrailer);
            this.trailerHitch.vehicleController.onDisable.AddListener(DisableTrailer);

            if (trailerHitch.vehicleController.enabled)
            {
                EnableTrailer();
            }
            else
            {
                DisableTrailer();
            }

            vehicleController.input.autoSetInput = false;

            // Raise trailer stand
            if (trailerStand != null)
            {
                trailerStand.SetActive(false);
            }

            attached = true;

            onAttach.Invoke();
        }


        public void OnDetach()
        {
            if (resetInputStatesOnDetach)
            {
                vehicleController.input.states.Reset();
            }

            vehicleController.input.autoSetInput = false;


            // Lower trailer stand
            if (trailerStand != null)
            {
                trailerStand.SetActive(true);
            }

            trailerHitch.vehicleController.onEnable.RemoveListener(EnableTrailer);
            trailerHitch.vehicleController.onDisable.RemoveListener(DisableTrailer);
            trailerHitch = null;

            attached = false;

            onDetach.Invoke();

            DisableTrailer();
        }


        private void EnableTrailer()
        {
            if (vehicleController != null) vehicleController.enabled = true;
        }

        private void DisableTrailer()
        {
            if (vehicleController != null) vehicleController.enabled = false;
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Modules.Trailer
{
    [CustomPropertyDrawer(typeof(TrailerModule))]
    public partial class TrailerModuleDrawer : ComponentNUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.BeginSubsection("Trailer Settings");
            drawer.Field("attachmentPoint");
            drawer.Field("attachmentTriggerRadius");
            drawer.Field("attachmentLayer");
            drawer.Field("trailerStand");
            drawer.Field("synchronizeGearShifts");
            drawer.EndSubsection();

            drawer.BeginSubsection("Events");
            drawer.Field("onAttach");
            drawer.Field("onDetach");
            drawer.EndSubsection();

            drawer.EndProperty();
            return true;
        }
    }
}
#endif
