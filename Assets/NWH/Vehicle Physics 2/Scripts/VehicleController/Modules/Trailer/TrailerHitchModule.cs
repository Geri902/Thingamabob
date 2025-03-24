using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;
using System.Collections;
using System.Linq;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif


namespace NWH.VehiclePhysics2.Modules.Trailer
{
    /// <summary>
    ///     Module representing the towing vehicle.
    ///     When a trailer is instantiated after initialization, running SyncTrailers() manually is required for the script to
    ///     find trailers.
    /// </summary>
    [Serializable]
    public partial class TrailerHitchModule : VehicleComponent
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
        /// The layer of the SphereCollider used to detect if the trailer module is in range.
        /// </summary>
        public float attachmentTriggerRadius = 0.4f;

        /// <summary>
        /// The radius of the SphereCollider used to detect if the trailer module is in range.
        /// </summary>
        public int attachmentLayer = 0;

        /// <summary>
        ///     If a trailer is in range when the scene is started it will be attached.
        /// </summary>
        [FormerlySerializedAs("attachOnPlay")]
        [Tooltip("    If a trailer is in range when the scene is started it will be attached.")]
        public bool attachOnEnable;

        /// <summary>
        ///     Breaking force of the generated joint.
        /// </summary>
        [Tooltip("    Breaking force of the generated joint.")]
        public float breakForce = Mathf.Infinity;

        /// <summary>
        ///     Can the trailer be detached once it is attached?
        /// </summary>
        [Tooltip("    Can the trailer be detached once it is attached?")]
        public bool detachable = true;

        /// <summary>
        ///     Power reduction that will be applied when vehicle has no trailer to avoid wheel spin when controlled with a binary
        ///     controller.
        /// </summary>
        [Tooltip(
            "Power reduction that will be applied when vehicle has no trailer to avoid wheel spin when controlled with a binary controller.")]
        public float noTrailerPowerCoefficient = 1f;

        public UnityEvent onTrailerAttach = new UnityEvent();
        public UnityEvent onTrailerDetach = new UnityEvent();

        /// <summary>
        ///     Is trailer's attachment point close enough to be attached to the towing vehicle?
        /// </summary>
        [Tooltip("    Is trailer's attachment point close enough to be attached to the towing vehicle?")]
        public bool trailerInRange;

        /// <summary>
        ///     Use for articulated busses and equipment where rotation around vertical axis is not wanted.
        /// </summary>
        [Tooltip("    Use for articulated busses and equipment where rotation around vertical axis is not wanted.")]
        public bool useHingeJoint;

        [NonSerialized]
        private ConfigurableJoint _configurableJoint;

        /// <summary>
        ///     A trailer that is attached to this trailer hitch
        /// </summary>
        [NonSerialized] public TrailerModule attachedTrailerModule;


        private Collider _triggerCollider;
        private bool _hasHadFirstFixedUpdate;


        public virtual void OnTriggerEnter(Collider other)
        {
            if (other == null || other.gameObject.layer != attachmentLayer) return;

            _triggerCollider = other;
            if (!_hasHadFirstFixedUpdate && attachOnEnable)
            {
                vehicleController.input.states.trailerAttachDetach = true;
            }
        }


        public virtual void OnTriggerStay(Collider other)
        {
            if (other == null || other.gameObject.layer != attachmentLayer) return;

            trailerInRange = true;
            _triggerCollider = other;
        }


        protected override void VC_Initialize()
        {
            base.VC_Initialize();

            attachedTrailerModule = null;
            attached = false;
        }


        public override void VC_Update()
        {
            base.VC_Update();

            if (attachedTrailerModule != null && attachedTrailerModule.vehicleController != null)
            {
                attachedTrailerModule.vehicleController.input.states = vehicleController.input.states;
                attachedTrailerModule.vehicleController.effectsManager.lightsManager.SetStateFromInt(
                    vehicleController.effectsManager.lightsManager.GetIntState()
                );
            }
        }


        public override void VC_FixedUpdate()
        {
            base.VC_FixedUpdate();

            // Attach trailer
            if (vehicleController.input.TrailerAttachDetach && !attached && _triggerCollider != null)
            {
                // Try get the trailer module wrapper.
                TrailerModuleWrapper trailerModuleWrapper = _triggerCollider.GetComponentInParent<TrailerModuleWrapper>();

                // Trigger does not have a module wrapper, exit.
                if (trailerModuleWrapper != null)
                {
                    AttachTrailer(trailerModuleWrapper);
                }
            }
            // Detach trailer
            else if (attached && vehicleController.input.TrailerAttachDetach)
            {
                DetachTrailer(vehicleController);
            }

            // Check if trailer attached but joint no longer exists (broken). 
            // OnJointBreak sometimes does not get called - Unity bug: https://forum.unity.com/threads/hinge-joints-not-destroying.973767/
            if (attached && _configurableJoint == null)
            {
                DetachTrailer(vehicleController);
            }

            if (trailerInRange)
            {
                trailerInRange = false;
            }
            else
            {
                vehicleController.input.TrailerAttachDetach = false;
            }

            trailerInRange = false;
            _triggerCollider = null;

            _hasHadFirstFixedUpdate = true;
        }


        public override bool VC_Enable(bool calledByParent)
        {
            if (base.VC_Enable(calledByParent))
            {
                vehicleController.powertrain.engine.powerModifiers.Add(NoTrailerPowerModifier);
                return true;
            }

            return false;
        }


        public override bool VC_Disable(bool calledByParent)
        {
            if (base.VC_Disable(calledByParent))
            {
                vehicleController.powertrain.engine.powerModifiers.Remove(NoTrailerPowerModifier);
                return true;
            }

            return false;
        }



        public float NoTrailerPowerModifier()
        {
            if (attached)
            {
                return 1f;
            }

            return noTrailerPowerCoefficient;
        }


        public void AttachTrailer(TrailerModuleWrapper trailerWrapper)
        {
            TrailerModule targetTrailerModule = trailerWrapper.module;
            if (targetTrailerModule == null)
            {
                Debug.LogWarning("Trying to attach a null trailer.");
                return;
            }

            VehicleController trailerVC = trailerWrapper.GetComponentInParent<VehicleController>();
            Debug.Assert(trailerVC != null, "Trailer wrapper is null");

            // Wake up the trailer
            trailerVC.enabled = true;

            // Position trailer
            trailerVC.vehicleTransform.position = trailerVC.transform.position -
                                                  (targetTrailerModule.attachmentPoint.transform.position -
                                                   attachmentPoint.transform.position);

            // Try to get joint
            _configurableJoint = vehicleController.GetComponent<ConfigurableJoint>();

            // Destroy existing joint
            if (_configurableJoint != null)
            {
                GameObject.Destroy(_configurableJoint);
            }

            // Reset input flag
            vehicleController.input.TrailerAttachDetach = false;
            attached = true;
            targetTrailerModule.OnAttach(this);
            onTrailerAttach.Invoke();


            // Add new joint
            if (_configurableJoint == null)
            {
                _configurableJoint = vehicleController.gameObject.AddComponent<ConfigurableJoint>();
            }

            // Configure the joint
            _configurableJoint.anchor =
                vehicleController.transform.InverseTransformPoint(targetTrailerModule.attachmentPoint.position);
            _configurableJoint.xMotion = ConfigurableJointMotion.Locked;
            _configurableJoint.yMotion = ConfigurableJointMotion.Locked;
            _configurableJoint.zMotion = ConfigurableJointMotion.Locked;
            _configurableJoint.angularZMotion =
                useHingeJoint ? ConfigurableJointMotion.Locked : ConfigurableJointMotion.Free;
            _configurableJoint.enableCollision = true;
            _configurableJoint.breakForce = breakForce;
            _configurableJoint.connectedBody = targetTrailerModule.vehicleController.vehicleRigidbody;

            attachedTrailerModule = targetTrailerModule;
        }


        public void DetachTrailer(VehicleController vc)
        {
            if (!detachable || attachedTrailerModule == null || attachedTrailerModule.vehicleController == null)
            {
                return;
            }

            attached = false;

            if (_configurableJoint != null)
            {
                Object.Destroy(_configurableJoint);
                _configurableJoint = null;
            }

            attachedTrailerModule.OnDetach();
            attachedTrailerModule = null;
            vc.input.TrailerAttachDetach = false;
            onTrailerDetach.Invoke();
        }
    }
}

#if UNITY_EDITOR

namespace NWH.VehiclePhysics2.Modules.Trailer
{
    [CustomPropertyDrawer(typeof(TrailerHitchModule))]
    public partial class TrailerHitchModuleDrawer : ComponentNUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.BeginSubsection("Attachment");
            drawer.Field("attachmentPoint");
            drawer.Field("attachmentTriggerRadius");
            drawer.Field("attachmentLayer");
            drawer.Field("attachOnEnable");
            drawer.Field("detachable");
            drawer.Field("trailerInRange", false);
            drawer.EndSubsection();

            drawer.BeginSubsection("Joint");
            drawer.Field("breakForce");
            drawer.Field("useHingeJoint");
            drawer.EndSubsection();

            drawer.BeginSubsection("Powertrain");
            drawer.Field("noTrailerPowerCoefficient");
            drawer.EndSubsection();

            drawer.BeginSubsection("Events");
            drawer.Field("onTrailerAttach");
            drawer.Field("onTrailerDetach");
            drawer.EndSubsection();

            drawer.EndProperty();
            return true;
        }
    }
}

#endif
