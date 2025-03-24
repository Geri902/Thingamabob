using System;
using UnityEngine;

namespace NWH.VehiclePhysics2.Modules.NOS
{
    /// <summary>
    ///     MonoBehaviour wrapper for NOS module.
    /// </summary>
    [Serializable]
    [DisallowMultipleComponent]
    public partial class NOSModuleWrapper : ModuleWrapper
    {
        public NOSModule module = new NOSModule();

        public override VehicleComponent GetModule()
        {
            return module;
        }

        public override void SetModule(VehicleComponent module)
        {
            this.module = module as NOSModule;
        }
    }
}