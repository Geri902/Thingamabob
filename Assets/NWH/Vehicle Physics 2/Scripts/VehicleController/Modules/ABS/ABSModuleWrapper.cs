using System;
using UnityEngine;

namespace NWH.VehiclePhysics2.Modules.ABS
{
    /// <summary>
    ///     MonoBehaviour wrapper for ABS module.
    /// </summary>
    [Serializable]
    [DisallowMultipleComponent]
    public partial class ABSModuleWrapper : ModuleWrapper
    {
        public ABSModule module = new ABSModule();


        public override VehicleComponent GetModule()
        {
            return module;
        }


        public override void SetModule(VehicleComponent module)
        {
            this.module = module as ABSModule;
        }
    }
}