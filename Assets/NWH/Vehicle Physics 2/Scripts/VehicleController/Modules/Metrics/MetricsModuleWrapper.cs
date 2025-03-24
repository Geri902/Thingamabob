using System;
using UnityEngine;

namespace NWH.VehiclePhysics2.Modules.Metrics
{
    /// <summary>
    ///     MonoBehaviour wrapper for Metrics module.
    /// </summary>
    [Serializable]
    [DisallowMultipleComponent]
    public partial class MetricsModuleWrapper : ModuleWrapper
    {
        public MetricsModule module = new MetricsModule();


        public override VehicleComponent GetModule()
        {
            return module;
        }


        public override void SetModule(VehicleComponent module)
        {
            this.module = module as MetricsModule;
        }
    }
}