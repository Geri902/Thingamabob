using System;
using System.Collections.Generic;
using UnityEngine;


namespace NWH.VehiclePhysics2
{
    public abstract class ManagerVehicleComponent : VehicleComponent
    {
        [NonSerialized]
        protected List<VehicleComponent> _components = null;

        public virtual List<VehicleComponent> Components
        {
            get
            {
                if (_components == null)
                {
                    UpdateComponentList();
                }
                return _components;
            }
        }


        protected abstract void FillComponentList();


        private void UpdateComponentList()
        {
            FillComponentList();
        }


        public void AddAndOnboardNewComponent(VehicleComponent component)
        {
            Components.Add(component);
            component.VC_SetVehicleController(vehicleController);
            component.VC_LoadStateFromStateSettings();
            component.UpdateLOD();
        }


        public override void VC_SetVehicleController(VehicleController vc)
        {
            base.VC_SetVehicleController(vc);

            for (int i = 0; i < Components.Count; i++)
            {
                Components[i].VC_SetVehicleController(vc);
            }
        }


        public override void VC_LoadStateFromStateSettings()
        {
            base.VC_LoadStateFromStateSettings();

            for (int i = 0; i < Components.Count; i++)
            {
                VehicleComponent component = Components[i];
                component.VC_LoadStateFromStateSettings();
            }
        }


        public override void VC_Update()
        {
            base.VC_Update();

            for (int i = 0; i < Components.Count; i++)
            {
                VehicleComponent component = Components[i];
                if (component.IsActive)
                {
                    component.VC_Update();
                }
            }
        }


        public override void VC_FixedUpdate()
        {
            base.VC_FixedUpdate();

            for (int i = 0; i < Components.Count; i++)
            {
                VehicleComponent component = Components[i];
                if (component.IsActive)
                {
                    component.VC_FixedUpdate();
                }
            }
        }


        public override bool VC_Enable(bool calledByParent)
        {
            if (base.VC_Enable(calledByParent))
            {
                for (int i = 0; i < Components.Count; i++)
                {
                    VehicleComponent component = Components[i];
                    component.VC_Enable(true);
                }

                return true;
            }

            return false;
        }


        public override bool VC_Disable(bool calledByParent)
        {
            if (base.VC_Disable(calledByParent))
            {
                for (int i = 0; i < Components.Count; i++)
                {
                    VehicleComponent component = Components[i];
                    component.VC_Disable(true);
                }

                return true;
            }

            return false;
        }


        public override void VC_DrawGizmos()
        {
            base.VC_DrawGizmos();

            for (int i = 0; i < Components.Count; i++)
            {
                VehicleComponent component = Components[i];
                if (component.state.isEnabled)
                {
                    component.VC_DrawGizmos();
                }
            }
        }


        public override void VC_SetDefaults()
        {
            base.VC_SetDefaults();

            for (int i = 0; i < Components.Count; i++)
            {
                Components[i].VC_SetDefaults();
            }
        }


        public override void VC_Validate(VehicleController vc)
        {
            base.VC_Validate(vc);

            if (!state.isEnabled) return;

            for (int i = 0; i < Components.Count; i++)
            {
                VehicleComponent component = Components[i];
                if (component.state.isEnabled)
                {
                    component.VC_Validate(vc);
                }
            }
        }


        public override void UpdateLOD()
        {
            base.UpdateLOD();

            for (int i = 0; i < Components.Count; i++)
            {
                Components[i].UpdateLOD();
            }
        }
    }
}

