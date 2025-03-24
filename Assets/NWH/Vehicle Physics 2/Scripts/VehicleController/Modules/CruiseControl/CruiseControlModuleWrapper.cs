using System;
using UnityEngine;

namespace NWH.VehiclePhysics2.Modules.CruiseControl
{
    /// <summary>
    ///     MonoBehaviour wrapper for CruiseControl module.
    /// </summary>
    [Serializable]
    [DisallowMultipleComponent]
    public partial class CruiseControlModuleWrapper : ModuleWrapper
    {
        public CruiseControlModule module = new CruiseControlModule();


        public override VehicleComponent GetModule()
        {
            return module;
        }


        public override void SetModule(VehicleComponent module)
        {
            this.module = module as CruiseControlModule;
        }
    }
}