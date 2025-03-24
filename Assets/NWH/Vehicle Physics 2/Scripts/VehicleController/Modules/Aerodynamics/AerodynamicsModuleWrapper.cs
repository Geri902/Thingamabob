using System;
using UnityEngine;

namespace NWH.VehiclePhysics2.Modules.Aerodynamics
{
    /// <summary>
    ///     MonoBehaviour wrapper for Aerodynamics module.
    /// </summary>
    [Serializable]
    [DisallowMultipleComponent]
    public partial class AerodynamicsModuleWrapper : ModuleWrapper
    {
        public AerodynamicsModule module = new AerodynamicsModule();


        public override VehicleComponent GetModule()
        {
            return module;
        }


        public override void SetModule(VehicleComponent module)
        {
            this.module = module as AerodynamicsModule;
        }
    }
}