using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NWH.VehiclePhysics2.Modules.Trailer
{
    /// <summary>
    ///     MonoBehaviour wrapper for TrailerHitch module.
    /// </summary>
    [Serializable]
    [DisallowMultipleComponent]
    public partial class TrailerHitchModuleWrapper : ModuleWrapper
    {
        public TrailerHitchModule module = new TrailerHitchModule();

        private VehicleController _vehicleController;


        private void Awake()
        {
            // Run some of the trailer initialization here since the trailer VehicleController
            // might be disabled and un-initialized at the time of attachment.
            _vehicleController = GetComponentInParent<VehicleController>();
            if (_vehicleController == null)
            {
                Debug.LogError($"No VehicleController attached to the TrailerModule {name}");
                return;
            }

            if (module.attachmentPoint == null)
            {
                Debug.LogError($"{name}: TrailerHitchModule attachmentPoint is null.");
                return;
            }

            SphereCollider triggerCollider = module.attachmentPoint.GetComponent<SphereCollider>();
            if (triggerCollider == null)
            {
                triggerCollider = module.attachmentPoint.gameObject.AddComponent<SphereCollider>();
                triggerCollider.radius = module.attachmentTriggerRadius;
                triggerCollider.isTrigger = true;
                triggerCollider.gameObject.layer = module.attachmentLayer;
            }

            module.vehicleController = _vehicleController;
        }


        public override VehicleComponent GetModule()
        {
            return module;
        }


        public void OnTriggerEnter(Collider other)
        {
            module.OnTriggerEnter(other);
        }


        public void OnTriggerStay(Collider other)
        {
            module.OnTriggerStay(other);
        }

        public override void SetModule(VehicleComponent module)
        {
            this.module = module as TrailerHitchModule;
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawSphere(module.attachmentPoint.position, 0.1f);

#if UNITY_EDITOR
            Handles.Label(module.attachmentPoint.position, "Trailer Hitch");
#endif
        }
    }
}