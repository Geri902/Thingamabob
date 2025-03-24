using System;
using System.Collections.Generic;
using UnityEngine;

namespace NWH.VehiclePhysics2.Modules.ModuleTemplate
{
    /// <summary>
    ///     Empty module example / template.
    /// </summary>
    [Serializable]
    public partial class ModuleTemplate : VehicleComponent
    {
        // EXAMPLE FIELDS

        /// <summary>
        ///     Example float field.
        /// </summary>
        [Range(0, 1)]
        [Tooltip("    Example float field.")]
        public float floatExample;

        /// <summary>
        ///     Example list field.
        /// </summary>
        [Tooltip("    Example list field.")]
        public List<int> listExample = new List<int>();


        protected override void VC_Initialize()
        {
            // Run initialization code here and return before the base call
            // if initialization failed.

            base.VC_Initialize();
        }


        public override void VC_Update()
        {
            base.VC_Update();
        }


        public override void VC_FixedUpdate()
        {
            base.VC_FixedUpdate();
        }
    }
}