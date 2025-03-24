using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif


namespace NWH.VehiclePhysics2.Modules
{
    /// <summary>
    ///     Wrapper around modules.
    ///     Unity does not support polymorphic serializations (not counting the SerializeReference which in 2019.3 is not
    ///     production ready) so
    ///     the workaround is to wrap each module type in a MonoBehaviour wrapper.
    /// </summary>
    [RequireComponent(typeof(VehicleController))]
    [Serializable]
    [DefaultExecutionOrder(200)]
    public abstract class ModuleWrapper : MonoBehaviour
    {
        /// <summary>
        ///     Returns wrapper's module.
        /// </summary>
        /// <returns></returns>
        public abstract VehicleComponent GetModule();


        /// <summary>
        ///     Sets wrapper's module.
        /// </summary>
        /// <param name="vehicleComponent"></param>
        public abstract void SetModule(VehicleComponent vehicleComponent);


        private void Reset()
        {
            InitModule();
            GetModule().VC_SetDefaults();
        }

        private void InitModule()
        {
            VehicleController vc = GetComponent<VehicleController>();
            Debug.Assert(vc != null, $"VehicleController not found on {name}");
            VehicleComponent module = GetModule();
            module.VC_SetVehicleController(vc);
            module.VC_LoadStateFromStateSettings();
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Modules
{
    [CustomEditor(typeof(ModuleWrapper), true)]
    [CanEditMultipleObjects]
    public partial class ModuleWrapperEditor : NVP_NUIEditor
    {
        public override void OnInspectorGUI()
        {
            OnInspectorNUI();
        }


        public override bool OnInspectorNUI()
        {
            drawer.BeginEditor(serializedObject);
            drawer.Property(drawer.serializedObject.FindProperty("module"));
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
