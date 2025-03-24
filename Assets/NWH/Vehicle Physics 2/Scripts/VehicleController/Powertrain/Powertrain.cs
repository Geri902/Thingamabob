using System;
using System.Collections.Generic;
using System.Linq;
using NWH.Common.Vehicles;
using NWH.VehiclePhysics2.Powertrain.Wheel;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NWH.VehiclePhysics2.Powertrain
{
    [Serializable]
    public partial class Powertrain : ManagerVehicleComponent
    {
        public ClutchComponent clutch = new ClutchComponent();
        public List<DifferentialComponent> differentials = new List<DifferentialComponent>();
        public EngineComponent engine = new EngineComponent();
        public TransmissionComponent transmission = new TransmissionComponent();
        public List<WheelGroup> wheelGroups = new List<WheelGroup>();
        public List<WheelComponent> wheels = new List<WheelComponent>();

        public int wheelGroupCount
        {
            get { return wheelGroups.Count; }
        }


        public int wheelCount
        {
            get { return wheels.Count; }
        }


        public override void VC_SetVehicleController(VehicleController vc)
        {
            base.VC_SetVehicleController(vc);
            wheelGroups.ForEach(w => w.vc = vc);
        }


        public override void VC_FixedUpdate()
        {
            base.VC_FixedUpdate();

            if (vehicleController.input.InputSwappedThrottle > Vehicle.INPUT_DEADZONE)
            {
                for (int i = 0; i < wheelCount; i++)
                {
                    wheels[i].wheelUAPI.WakeFromSleep();
                }
            }


            for (int i = 0; i < wheelGroupCount; i++)
            {
                wheelGroups[i].Update();
            }

            engine.IntegrateDownwards(vehicleController.fixedDeltaTime);
        }


        public override void VC_SetDefaults()
        {
            engine.name = "Engine";
            clutch.name = "Clutch";
            transmission.name = "Transmission";

            base.VC_SetDefaults();

            // Find wheels
            wheels = new List<WheelComponent>();
            WheelUAPI[] wheelUAPIs = vehicleController.GetComponentsInChildren<WheelUAPI>();
            for (int i = 0; i < wheelUAPIs.Length; i++)
            {
                WheelUAPI wheelUAPI = wheelUAPIs[i];
                Debug.Log($"VehicleController setup: Found wheel '{wheelUAPI.transform.name}'");

                WheelComponent wheel = new WheelComponent();
                wheel.name = "Wheel" + wheelUAPI.transform.name;
                wheel.wheelUAPI = wheelUAPI;
                wheels.Add(wheel);
            }

            if (wheels.Count == 0)
            {
                Debug.LogWarning("No WheelControllers found, skipping powertrain auto-setup.");
                return;
            }

            // Order wheels in left-right, front to back order.
            wheels = wheels.OrderByDescending(w => w.wheelUAPI.transform.localPosition.z).ToList();
            List<int> wheelGroupIndices = new List<int>();
            int wheelGroupCount = 1;
            float prevWheelZ = wheels[0].wheelUAPI.transform.localPosition.z;
            for (int i = 0; i < wheels.Count; i++)
            {
                WheelComponent wheel = wheels[i];
                float wheelZ = wheel.wheelUAPI.transform.localPosition.z;

                // Wheels are on different axes, add new axis/wheel group.
                if (Mathf.Abs(wheelZ - prevWheelZ) > 0.2f)
                {
                    wheelGroupCount++;
                }
                // Wheels are on the same axle, order left to right.
                else if (i > 0)
                {
                    if (wheels[i].wheelUAPI.transform.localPosition.x <
                        wheels[i - 1].wheelUAPI.transform.localPosition.x)
                    {
                        (wheels[i - 1], wheels[i]) = (wheels[i], wheels[i - 1]);
                    }
                }

                wheelGroupIndices.Add(wheelGroupCount - 1);
                prevWheelZ = wheelZ;
            }

            // Add wheel groups
            wheelGroups = new List<WheelGroup>();
            for (int i = 0; i < wheelGroupCount; i++)
            {
                string appendix = i == 0 ? "Front" : i == wheelGroupCount - 1 ? "Rear" : "Middle";
                string groupName = $"{appendix} Axle {i}";
                wheelGroups.Add(new WheelGroup
                {
                    name = groupName,
                    brakeCoefficient = i == 0 || wheelGroupCount > 2 ? 1f : 0.7f,
                    handbrakeCoefficient = i == wheelGroupCount - 1 ? 1f : 0f,
                    steerCoefficient = i == 0 ? 1f : i == 1 && wheelGroupCount > 2 ? 0.5f : 0f,
                    addAckerman = true,
                    isSolid = false
                });
                Debug.Log($"VehicleController setup: Creating WheelGroup '{groupName}'");
            }

            // Add differentials
            differentials = new List<DifferentialComponent>();
            Debug.Log("[Powertrain] Adding 'Front Differential'");
            differentials.Add(new DifferentialComponent { name = "Front Differential" });
            Debug.Log("[Powertrain] Adding 'Rear Differential'");
            differentials.Add(new DifferentialComponent { name = "Rear Differential" });
            Debug.Log("[Powertrain] Adding 'Center Differential'");
            differentials.Add(new DifferentialComponent { name = "Center Differential" });
            differentials[2].Output = differentials[0];
            differentials[2].OutputB = differentials[1];

            // Connect transmission to differentials
            Debug.Log($"[Powertrain] Setting transmission output to '{differentials[2].name}'");
            transmission.Output = differentials[2];

            // Add wheels to wheel groups
            for (int i = 0; i < wheels.Count; i++)
            {
                int wheelGroupIndex = wheelGroupIndices[i];
                wheels[i].wheelGroupSelector = new WheelGroupSelector { index = wheelGroupIndex };
                Debug.Log($"[Powertrain] Adding '{wheels[i].name}' to '{wheelGroups[wheelGroupIndex].name}'");
            }

            // Connect wheels to differentials
            int wheelGroupsCount = wheelGroups.Count;
            wheelGroupsCount =
                wheelGroupCount > 2
                    ? 2
                    : wheelGroupCount; // Prevent from resetting diffs on vehicles with more than 2 axles
            for (int i = 0; i < wheelGroupsCount; i++)
            {
                WheelGroup group = wheelGroups[i];
                List<WheelComponent> belongingWheels = group.FindWheelsBelongingToGroup(ref wheels, i);

                if (belongingWheels.Count == 2)
                {
                    Debug.Log(
                        $"[Powertrain] Setting output of '{differentials[i].name}' to '{belongingWheels[0].name}'");
                    if (belongingWheels[0].wheelUAPI.transform.position.x < -0.01f)
                    {
                        differentials[i].Output = belongingWheels[0];
                        differentials[i].OutputB = belongingWheels[1];
                    }
                    else if (belongingWheels[0].wheelUAPI.transform.position.x > 0.01f)
                    {
                        differentials[i].Output = belongingWheels[1];
                        differentials[i].OutputB = belongingWheels[0];
                    }
                    else
                    {
                        Debug.LogWarning(
                            "[Powertrain] Powertrain settings for center wheels have to be manually set up. If powered either connect it directly to transmission (motorcycle) or to one side of center differential (trike).");
                    }
                }
            }

            FillComponentList();
        }


        public override void VC_Validate(VehicleController vc)
        {
            base.VC_Validate(vc);

            if (!state.isEnabled) return;

            engine.VC_Validate(vc);
            clutch.VC_Validate(vc);
            transmission.VC_Validate(vc);
            differentials.ForEach(diff => diff.VC_Validate(vc));
            wheels.ForEach(wheel => wheel.VC_Validate(vc));
        }


        public void Repair()
        {
            engine.Damage = 0;
            clutch.Damage = 0;
            transmission.Damage = 0;
            differentials.ForEach(diff => diff.Damage = 0);
            wheels.ForEach(wheel => wheel.Damage = 0);
        }


        /// <summary>
        ///     Helper script for the output dropdown,
        ///     should be used in inspector only as it is quite slow.
        /// </summary>
        /// <returns></returns>
        public List<string> Inspector_GetPowertrainComponentNames()
        {
            List<string> names = new List<string>();
            names.Add("[none]");
            names.Add(engine.name);
            names.Add(clutch.name);
            names.Add(transmission.name);
            names.AddRange(differentials.Select(diff => diff.name));
            names.AddRange(wheels.Select(wheel => wheel.name));
            return names;
        }


        /// <summary>
        ///     To be used with the inspectorComponentIndex.
        ///     First element is always null.
        /// </summary>
        public List<PowertrainComponent> Inspector_GetPowertrainComponents()
        {
            List<PowertrainComponent> components = new List<PowertrainComponent>();
            components.Add(null);
            components.Add(engine);
            components.Add(clutch);
            components.Add(transmission);
            components.AddRange(differentials);
            components.AddRange(wheels);
            return components;
        }


        public PowertrainComponent Inspector_GetPowertrainComponentFromNameHash(int nameHash)
        {
            return Inspector_GetPowertrainComponents().FirstOrDefault(c => c != null && c.name.GetHashCode() == nameHash);
        }


        protected override void VC_Initialize()
        {
            wheelGroups.ForEach(w => w.Initialize());

            base.VC_Initialize();
        }


        protected override void FillComponentList()
        {
            _components = new List<VehicleComponent>();
            _components.Add(engine);
            _components.Add(clutch);
            _components.Add(transmission);
            _components.AddRange(differentials);
            _components.AddRange(wheels);
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Powertrain
{
    [CustomPropertyDrawer(typeof(Powertrain))]
    public partial class PowertrainDrawer : ComponentNUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            int powertrainTab = drawer.HorizontalToolbar("powertrainTab",
                new[]
                {
                    "Engine", "Clutch", "Transmission", "Differentials",
                    "Wheels", "Wheel Groups"
                }, true, true);

            switch (powertrainTab)
            {
                case 0:
                    drawer.Property("engine");
                    break;
                case 1:
                    drawer.Property("clutch");
                    break;
                case 2:
                    drawer.Property("transmission");
                    break;
                case 3:
                    drawer.ReorderableList("differentials", null, true, true, null, 5f);
                    break;
                case 4:
                    drawer.Space(3);
                    drawer.Info(
                        "Wheels must be added in left to right, front to back order. E.g.: FrontLeft, FrontRight, RearLeft, RearRight.",
                        MessageType.Warning);
                    drawer.ReorderableList("wheels", null, true, true, null, 5f);
                    break;
                case 5:
                    drawer.ReorderableList("wheelGroups", null, true, true, null, 5f);
                    break;
            }

            drawer.EndProperty();
            return true;
        }
    }
}

#endif