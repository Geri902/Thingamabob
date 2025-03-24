using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif


namespace NWH.VehiclePhysics2.Modules
{
    /// <summary>
    ///     Manages vehicle modules.
    /// </summary>
    [System.Serializable]
    public partial class ModuleManager : ManagerVehicleComponent
    {
        protected override void FillComponentList()
        {
            if (_components == null)
            {
                _components = new List<VehicleComponent>();
            }
            else
            {
                _components.Clear();
            }

            ModuleWrapper[] moduleWrappers = vehicleController.GetComponents<ModuleWrapper>();
            if (moduleWrappers == null || moduleWrappers.Length == 0)
            {
                return;
            }

            for (int i = 0; i < moduleWrappers.Length; i++)
            {
                _components.Add(moduleWrappers[i].GetModule());
            }
        }
    }
}

#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Modules
{
    [CustomPropertyDrawer(typeof(ModuleManager))]
    public partial class ModuleManagerDrawer : ComponentNUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.EndProperty();
            return true;
        }
    }
}

#endif