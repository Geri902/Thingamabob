using System;
using UnityEngine;

namespace NWH.VehiclePhysics2
{
    /// <summary>
    ///     Multiplayer-related VehicleController code.
    /// </summary>
    public partial class VehicleController
    {
        /// <summary>
        /// Struct that holds multiplayer state to be synced over the network.
        /// </summary>
        [NonSerialized]
        protected MultiplayerState _multiplayerState;


        public MultiplayerState GetMultiplayerState()
        {
            _multiplayerState.lightState = effectsManager.lightsManager.GetIntState();
            _multiplayerState.engineAngularVelocity = powertrain.engine.outputAngularVelocity;
            _multiplayerState.steering = input.states.steering;
            _multiplayerState.throttle = input.states.throttle;
            _multiplayerState.clutch = input.states.clutch;
            _multiplayerState.handbrake = input.states.handbrake;
            _multiplayerState.shiftInto = input.states.shiftInto;
            _multiplayerState.shiftUp = input.states.shiftUp;
            _multiplayerState.shiftDown = input.states.shiftDown;
            _multiplayerState.trailerAttachDetach = input.states.trailerAttachDetach;
            _multiplayerState.horn = input.states.horn;
            _multiplayerState.engineStartStop = input.states.engineStartStop;
            _multiplayerState.cruiseControl = input.states.cruiseControl;
            _multiplayerState.boost = input.states.boost;
            _multiplayerState.flipOver = input.states.flipOver;

            return _multiplayerState;
        }


        public bool SetMultiplayerState(MultiplayerState inboundState)
        {
            effectsManager.lightsManager.SetStateFromInt(inboundState.lightState);
            powertrain.engine.outputAngularVelocity = inboundState.engineAngularVelocity;

            _multiplayerState = inboundState;

            input.autoSetInput = false;
            input.states.steering = inboundState.steering;
            input.states.throttle = inboundState.throttle;
            input.states.clutch = inboundState.clutch;
            input.states.handbrake = inboundState.handbrake;
            input.states.shiftInto = inboundState.shiftInto;
            input.states.shiftUp = inboundState.shiftUp;
            input.states.shiftDown = inboundState.shiftDown;
            input.states.trailerAttachDetach = inboundState.trailerAttachDetach;
            input.states.horn = inboundState.horn;
            input.states.engineStartStop = inboundState.engineStartStop;
            input.states.cruiseControl = inboundState.cruiseControl;
            input.states.boost = inboundState.boost;
            input.states.flipOver = inboundState.flipOver;

            return true;
        }


        public struct MultiplayerState
        {
            public int lightState;
            public float engineAngularVelocity;
            public float steering;
            public float throttle;
            public float clutch;
            public float handbrake;
            public int shiftInto;
            public bool shiftUp;
            public bool shiftDown;
            public bool trailerAttachDetach;
            public bool horn;
            public bool engineStartStop;
            public bool cruiseControl;
            public bool boost;
            public bool flipOver;
        }
    }
}
