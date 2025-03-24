using System;
using UnityEngine;

namespace NWH.VehiclePhysics2.Modules.ESC
{
    /// <summary>
    ///     MonoBehaviour wrapper for Electronic Stability Control (ESC) module.
    /// </summary>
    [Serializable]
    [DisallowMultipleComponent]
    public partial class ESCModuleWrapper : ModuleWrapper
    {
        public ESCModule module = new ESCModule();


        public override VehicleComponent GetModule()
        {
            return module;
        }


        public override void SetModule(VehicleComponent module)
        {
            this.module = module as ESCModule;
        }
    }
}