using System;
using UnityEngine;

namespace NWH.VehiclePhysics2.Modules.Rigging
{
    /// <summary>
    ///     MonoBehaviour wrapper for Rigging module.
    /// </summary>
    [Serializable]
    [DisallowMultipleComponent]
    public partial class RiggingModuleWrapper : ModuleWrapper
    {
        public RiggingModule module = new RiggingModule();


        public override VehicleComponent GetModule()
        {
            return module;
        }


        public override void SetModule(VehicleComponent module)
        {
            this.module = module as RiggingModule;
        }
    }
}