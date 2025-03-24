using System;
using UnityEngine;

namespace NWH.VehiclePhysics2.Modules.TCS
{
    /// <summary>
    ///     MonoBehaviour wrapper for TCS module.
    /// </summary>
    [Serializable]
    [DisallowMultipleComponent]
    public partial class TCSModuleWrapper : ModuleWrapper
    {
        public TCSModule module = new TCSModule();


        public override VehicleComponent GetModule()
        {
            return module;
        }


        public override void SetModule(VehicleComponent module)
        {
            this.module = module as TCSModule;
        }
    }
}