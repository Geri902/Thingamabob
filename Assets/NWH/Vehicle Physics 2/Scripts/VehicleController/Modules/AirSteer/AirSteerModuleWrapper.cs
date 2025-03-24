using System;
using UnityEngine;

namespace NWH.VehiclePhysics2.Modules.AirSteer
{
    /// <summary>
    ///     MonoBehaviour wrapper for example module.
    /// </summary>
    [Serializable]
    [DisallowMultipleComponent]
    public partial class AirSteerModuleWrapper : ModuleWrapper
    {
        public AirSteerModule module = new AirSteerModule();

        public override VehicleComponent GetModule()
        {
            return module;
        }


        public override void SetModule(VehicleComponent module)
        {
            this.module = module as AirSteerModule;
        }
    }
}