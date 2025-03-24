using System;
using System.Collections;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace NWH.VehiclePhysics2
{
    /// <summary>
    ///     Collision related VehicleController code.
    /// </summary>
    public partial class VehicleController
    {
        /// <summary>
        ///     Called when a collision happens.
        /// </summary>
        [Tooltip("    Called when a collision happens.")]
        public UnityEvent<Collision> onCollision = new UnityEvent<Collision>();


        private void OnCollisionEnter(Collision other)
        {
#if NVP2_DEBUG
            Debug.Log($"OnCollisionEnter called on {name}.");
#endif

            onCollision.Invoke(other);
        }
    }
}

