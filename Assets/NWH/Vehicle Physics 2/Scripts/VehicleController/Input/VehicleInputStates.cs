using System;
using System.Diagnostics;
using UnityEngine;

namespace NWH.VehiclePhysics2.Input
{
    /// <summary>
    ///     Struct for storing input states of the vehicle.
    ///     Allows for input to be copied between the vehicles.
    /// </summary>
    [Serializable]
    public struct VehicleInputStates
    {
        /// <summary>
        /// Steering input. Range is -1 to 1.
        /// </summary>
        [Range(-1f, 1f)]
        [NonSerialized]
        public float steering;

        /// <summary>
        /// Same as steering but without any modifications. Raw user input.
        /// </summary>
        [HideInInspector]
        public float steeringRaw;

        /// <summary>
        /// Throttle input. Range is 0 to 1.
        /// </summary>
        [Range(0, 1f)]
        [NonSerialized]
        public float throttle;

        /// <summary>
        /// Same as throttle but without any modifications. Raw user input.
        /// </summary>
        [HideInInspector]
        public float throttleRaw;

        /// <summary>
        /// Can represent hrottle or brake input, depending on the travel direction of the vehicle.
        /// </summary>
        [HideInInspector]
        public float inputSwappedThrottle;

        /// <summary>
        /// Same as inputSwappedThrottle but without any modifications. Raw user input.
        /// </summary>
        [HideInInspector]
        public float inputSwappedThrottleRaw;

        /// <summary>
        /// Brake input. Range is 0 to 1.
        /// </summary>
        [Range(0, 1f)]
        [NonSerialized]
        public float brakes;

        /// <summary>
        /// Same as brakes but without any modifications. Raw user input.
        /// </summary>
        [HideInInspector]
        public float brakesRaw;

        /// <summary>
        /// Can represent hrottle or brake input, depending on the travel direction of the vehicle.
        /// </summary>
        [HideInInspector]
        public float inputSwappedBrakes;

        /// <summary>
        /// Same as inputSwappedBrakes but without any modifications. Raw user input.
        /// </summary>
        [HideInInspector]
        public float inputSwappedBrakesRaw;

        /// <summary>
        /// Clutch input. Range is 0 to 1.
        /// </summary>
        [Range(0f, 1f)]
        [NonSerialized]
        public float clutch;

        /// <summary>
        /// Same as clutch but without any modifications. Raw user input.
        /// </summary>
        [HideInInspector]
        public float clutchRaw;

        /// <summary>
        /// Handbrake input. Range is 0 to 1.
        /// </summary>
        [Range(0f, 1f)]
        [NonSerialized]
        public float handbrake;

        /// <summary>
        /// Same as handbrake but without any modifications. Raw user input.
        /// </summary>
        [HideInInspector]
        public float handbrakeRaw;

        /// <summary>
        /// Engine start/stop flag.
        /// </summary>
        [NonSerialized]
        public bool engineStartStop;

        /// <summary>
        /// Extra lights flag.
        /// </summary>
        [NonSerialized]
        public bool extraLights;

        /// <summary>
        /// High beams flag.
        /// </summary>
        [NonSerialized]
        public bool highBeamLights;

        /// <summary>
        /// Hazards flag.
        /// </summary>
        [NonSerialized]
        public bool hazardLights;

        /// <summary>
        /// Horn flag.
        /// </summary>
        [NonSerialized]
        public bool horn;

        /// <summary>
        /// Left blinker flag.
        /// </summary>
        [NonSerialized]
        public bool leftBlinker;

        /// <summary>
        /// Low beam light flag.
        /// </summary>
        [NonSerialized]
        public bool lowBeamLights;

        /// <summary>
        /// Right blinker flag.
        /// </summary>
        [NonSerialized]
        public bool rightBlinker;

        /// <summary>
        /// Shift down flag.
        /// </summary>
        [NonSerialized]
        public bool shiftDown;

        /// <summary>
        /// Shift into gear. -999 means no shift.
        /// </summary>
        [NonSerialized]
        public int shiftInto;

        /// <summary>
        /// Shift up flag.
        /// </summary>
        [NonSerialized]
        public bool shiftUp;

        /// <summary>
        /// Trailer attach/detach flag. Only active if there is a trailer module present.
        /// </summary>
        [NonSerialized]
        public bool trailerAttachDetach;

        /// <summary>
        /// Cruise control flag. Only active if there is a cruise control module present.
        /// </summary>
        [NonSerialized]
        public bool cruiseControl;

        /// <summary>
        /// Boost flag. Only active if there is a boost module present.
        /// </summary>
        [NonSerialized]
        public bool boost;

        /// <summary>
        /// Flip over flag. Only active if there is a flip over module present.
        /// </summary>
        [NonSerialized]
        public bool flipOver;


        public void Reset()
        {
            steering = 0;
            steeringRaw = 0;
            throttle = 0;
            throttleRaw = 0;
            inputSwappedThrottle = 0;
            inputSwappedThrottleRaw = 0;
            clutch = 0;
            clutchRaw = 0;
            brakes = 0;
            brakesRaw = 0;
            inputSwappedBrakes = 0;
            inputSwappedBrakesRaw = 0;
            handbrake = 0;
            handbrakeRaw = 0;
            shiftInto = -999;
            shiftUp = false;
            shiftDown = false;
            leftBlinker = false;
            rightBlinker = false;
            lowBeamLights = false;
            highBeamLights = false;
            hazardLights = false;
            extraLights = false;
            trailerAttachDetach = false;
            horn = false;
            engineStartStop = false;
            cruiseControl = false;
            boost = false;
            flipOver = false;
        }
    }
}