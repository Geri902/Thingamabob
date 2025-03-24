using System;
using System.Collections.Generic;
using NWH.Common.Utility;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using NWH.Common.Vehicles;
using System.Linq;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Powertrain
{
    [Serializable]
    public partial class TransmissionComponent : PowertrainComponent
    {
        public float ReferenceShiftRPM
        {
            get { return _referenceShiftRPM; }
        }

        private float _referenceShiftRPM;


        /// <summary>
        /// Function that handles gear shifts.
        /// Use External transmission type and assign this delegate manually to use a custom
        /// gear shift function.
        /// </summary>
        public delegate void Shift(VehicleController vc);


        /// <summary>
        ///     If true the gear input has to be held for the transmission to stay in gear, otherwise it goes to neutral.
        ///     Used for hardware H-shifters.
        /// </summary>
        [Tooltip(
            "If true the gear input has to be held for the transmission to stay in gear, otherwise it goes to neutral.\r\nUsed for hardware H-shifters.")]
        public bool holdToKeepInGear;

        /// <summary>
        ///     Final gear multiplier. Each gear gets multiplied by this value.
        ///     Equivalent to axle/differential ratio in real life.
        /// </summary>
        [Tooltip(
            "    Final gear multiplier. Each gear gets multiplied by this value.\r\n    Equivalent to axle/differential ratio in real life.")]
        [ShowInSettings("Final Ratio", 1f, 20f, 1f)]
        public float finalGearRatio = 6;

        /// <summary>
        ///     [Obsolete, will be removed]
        ///     Currently active gearing profile.
        ///     Final gear ratio will be determined from this and final gear ratio.
        /// </summary>
        [Tooltip(
            "    Currently active gearing profile.\r\n    Final gear ratio will be determined from this and final gear ratio.")]
        [SerializeField]
        public TransmissionGearingProfile gearingProfile;

        /// <summary>
        /// A list of gears ratios in order of negative, neutral and then positive.
        /// E.g. -4, 0, 6, 4, 3, 2 => one reverse, 4 forward gears.
        /// </summary>
        [SerializeField]
        public List<float> gears = new List<float>();

        /// <summary>
        ///     Number of forward gears.
        /// </summary>
        public int forwardGearCount;

        /// <summary>
        ///     Number of reverse gears.
        /// </summary>
        public int reverseGearCount;


        /// <summary>
        ///     How much inclines affect shift point position. Higher value will push the shift up and shift down RPM up depending
        ///     on the current incline to prevent vehicle from upshifting at the wrong time.
        /// </summary>
        [Range(0, 4)]
        [Tooltip(
            "How much inclines affect shift point position. Higher value will push the shift up and shift down RPM up depending \r\n" +
            "on the current incline to prevent vehicle from upshifting at the wrong time.")]
        public float inclineEffectCoeff;


        /// <summary>
        ///     Event that gets triggered when transmission shifts down.
        /// </summary>
        [SerializeField]
        [Tooltip("    Event that gets triggered when transmission shifts down.")]
        public UnityEvent onDownshift = new UnityEvent();


        /// <summary>
        ///     Event that gets triggered when transmission shifts (up or down).
        /// </summary>
        [SerializeField]
        [Tooltip("    Event that gets triggered when transmission shifts (up or down).")]
        public UnityEvent onShift = new UnityEvent();


        /// <summary>
        ///     Event that gets triggered when transmission shifts up.
        /// </summary>
        [SerializeField]
        [Tooltip("    Event that gets triggered when transmission shifts up.")]
        public UnityEvent onUpshift = new UnityEvent();


        /// <summary>
        ///     Time after shifting in which shifting can not be done again.
        /// </summary>
        [Tooltip("    Time after shifting in which shifting can not be done again.")]
        public float postShiftBan = 0.5f;


        public enum AutomaticTransmissionDNRShiftType
        {
            Auto,
            RequireShiftInput,
            RepeatInput,
        }


        /// <summary>
        ///     Behavior when switching from neutral to forward or reverse gear.
        /// </summary>
        [FormerlySerializedAs("automaticTransmissionReverseType")]
        [FormerlySerializedAs("reverseType")]
        [Tooltip("    Behavior when switching from neutral to forward or reverse gear.")]
        public AutomaticTransmissionDNRShiftType automaticTransmissionDNRShiftType =
            AutomaticTransmissionDNRShiftType.Auto;

        /// <summary>
        /// Speed at which the vehicle can switch between D/N/R gears.
        /// </summary>
        public float dnrSpeedThreshold = 0.4f;

        /// <summary>
        /// If set to >0, the clutch will need to be released to the value below the set number
        /// for gear shifts to occur.
        /// </summary>
        public float clutchInputShiftThreshold = 1.0f;


        /// <summary>
        ///     Function that changes the gears as required.
        ///     Use transmissionType External and assign this delegate to use your own gear shift code.
        /// </summary>
        [Tooltip(
            "Function that changes the gears as required.\r\nUse transmissionType External and assign this delegate to use your own gear shift code.")]
        public Shift shiftDelegate;


        /// <summary>
        ///     Time it takes transmission to shift between gears.
        /// </summary>
        [Tooltip("    Time it takes transmission to shift between gears.")]
        [ShowInSettings("Shift Duration", 0.001f, 0.5f, 0.05f)]
        public float shiftDuration = 0.2f;


        /// <summary>
        ///     Intensity of variable shift point. Higher value will result in shift point moving higher up with higher engine
        ///     load.
        /// </summary>
        [Range(0, 1)]
        [Tooltip(
            "Intensity of variable shift point. Higher value will result in shift point moving higher up with higher engine load.")]
        public float variableShiftIntensity = 0.3f;


        /// <summary>
        ///     If enabled shifting when in manual transmission will be instant, ignoring post shift ban.
        /// </summary>
        [Tooltip("    If enabled shifting when in manual transmission will be instant, ignoring post shift ban.")]
        public bool ignorePostShiftBanInManual = true;


        /// <summary>
        ///     If enabled transmission will adjust both shift up and down points to match current load.
        /// </summary>
        [Tooltip("    If enabled transmission will adjust both shift up and down points to match current load.")]
        [ShowInSettings("Variable Shift Point")]
        public bool variableShiftPoint = true;


        /// <summary>
        /// Current gear ratio.
        /// </summary>
        [UnityEngine.Tooltip("Current gear ratio.")]
        [ShowInTelemetry]
        public float currentGearRatio;


        /// <summary>
        /// Is the transmission currently in the post-shift phase in which the shifting is disabled/banned to prevent gear hunting?
        /// </summary>
        public bool isPostShiftBanActive;

        /// <summary>
        /// Is a gear shift currently in progress.
        /// </summary>
        public bool isShifting;

        /// <summary>
        /// Progress of the current gear shift in range of 0 to 1.
        /// </summary>
        public float shiftProgress;


        /// <summary>
        ///     Current RPM at which transmission will aim to downshift. All the modifiers are taken into account.
        ///     This value changes with driving conditions.
        /// </summary>
        public float DownshiftRPM
        {
            get { return _downshiftRPM; }
            set { _downshiftRPM = Mathf.Clamp(value, 0, Mathf.Infinity); }
        }

        [SerializeField]
        private float _downshiftRPM = 1400;


        /// <summary>
        ///     RPM at which the transmission will try to downshift, but the value might get changed by shift modifier such
        ///     as incline modifier.
        ///     To get actual downshift RPM use DownshiftRPM.
        /// </summary>
        public float TargetDownshiftRPM
        {
            get { return _targetDownshiftRPM; }
        }

        [ShowInTelemetry]
        private float _targetDownshiftRPM;


        /// <summary>
        ///     RPM at which automatic transmission will shift up. If dynamic shift point is enabled this value will change
        ///     depending on load.
        /// </summary>
        public float UpshiftRPM
        {
            get { return _upshiftRPM; }
            set { _upshiftRPM = Mathf.Clamp(value, 0, Mathf.Infinity); }
        }

        [Tooltip(
            "RPM at which automatic transmission will shift up. If dynamic shift point is enabled this value will change depending on load.")]
        [SerializeField]
        [ShowInTelemetry]
        private float _upshiftRPM = 2800;


        /// <summary>
        ///     RPM at which the transmission will try to upshift, but the value might get changed by shift modifier such
        ///     as incline modifier.
        ///     To get actual upshift RPM use UpshiftRPM.
        /// </summary>
        public float TargetUpshiftRPM
        {
            get { return _targetUpshiftRPM; }
        }

        [ShowInTelemetry]
        private float _targetUpshiftRPM;


        public enum TransmissionShiftType
        {
            Manual,
            Automatic,
            AutomaticSequential_Obsolete,
            CVT,
            External,
        }

        /// <summary>
        ///     Determines in which way gears can be changed.
        ///     Manual - gears can only be shifted by manual user input.
        ///     Automatic - automatic gear changing. Allows for gear skipping (e.g. 3rd->5th) which can be useful in trucks and
        ///     other high gear count vehicles.
        ///     AutomaticSequential - automatic gear changing but only one gear at the time can be shifted (e.g. 3rd->4th)
        /// </summary>
        [SerializeField]
        [Tooltip("Manual - gears can only be shifted by manual user input. " +
                 "Automatic - automatic gear changing. Allows for gear skipping (e.g. 3rd->5th) which can be useful in trucks and other high gear count vehicles. " +
                 "AutomaticSequential - automatic gear changing but only one gear at the time can be shifted (e.g. 3rd->4th)")]
        [ShowInSettings("Type")]
        [FormerlySerializedAs("_transmissionType")]
        public TransmissionShiftType transmissionType = TransmissionShiftType.Automatic;

        /// <summary>
        /// Remembers the previous value of the transmission type. Needed
        /// because assigning the shift delegate each frame causes a lot of GC.
        /// </summary>
        private TransmissionShiftType _prevTransmissionType;

        /// <summary>
        /// Is the automatic gearbox sequential?
        /// Has no effect on manual transmission.
        /// </summary>
        [ShowInSettings("Sequential")]
        [UnityEngine.Tooltip("Is the automatic gearbox sequential?\r\nHas no effect on manual transmission.")]
        public bool isSequential = false;

        public bool allowUpshiftGearSkipping;

        public bool allowDownshiftGearSkipping = true;

        private bool _repeatInputFlag;
        private float _smoothedThrottleInput;


        /// <summary>
        ///     Timer needed to prevent manual transmission from slipping out of gear too soon when hold in gear is enabled,
        ///     which could happen in FixedUpdate() runs twice for one Update() and the shift flag is reset
        ///     resulting in gearbox thinking it has no shift input.
        /// </summary>
        private float _slipOutOfGearTimer = -999f;


        /// <summary>
        ///     0 for neutral, less than 0 for reverse gears and lager than 0 for forward gears.
        ///     Use 'ShiftInto' to set gear.
        /// </summary>
        public int Gear
        {
            get { return IndexToGear(gearIndex); }
            set { gearIndex = GearToIndex(value); }
        }


        /// <summary>
        /// Current gear index in the gears list.
        /// Different from gear because gear uses -1 = R, 0 = N and D = 1, while this is the apsolute index
        /// in the range of 0 to gear list size minus one.
        /// Use Gear to get the actual gear.
        /// </summary>
        [NonSerialized]
        [ShowInTelemetry]
        public int gearIndex;

        // Called when user tries to shift without clutch input and the clutch shift threshold is below 1.
        public UnityEvent triedToShiftWithoutClutch = new UnityEvent();

        private Coroutine _shiftCoroutine;

        /// <summary>
        ///     Returns current gear name as a string, e.g. "R", "R2", "N" or "1"
        /// </summary>
        public string GearName
        {
            get
            {
                int gear = Gear;

                if (_gearNameCache.TryGetValue(gear, out string gearName))
                {
                    return gearName;
                }

                if (gear == 0)
                {
                    gearName = "N";
                }
                else if (gear > 0)
                {
                    gearName = Gear.ToString();
                }
                else
                {
                    gearName = "R" + -gear;
                }

                _gearNameCache[gear] = gearName;
                return gearName;
            }
        }

        private Dictionary<int, string> _gearNameCache = new Dictionary<int, string>();


        protected override void VC_Initialize()
        {
            UpdateGearCounts();
            Gear = 0;
            if (transmissionType == TransmissionShiftType.AutomaticSequential_Obsolete)
            {
                transmissionType = TransmissionShiftType.Automatic;
            }

            AssignShiftDelegate();

            base.VC_Initialize();
        }


        public override bool VC_Disable(bool calledByParent)
        {
            if (base.VC_Disable(calledByParent))
            {
                Gear = 0;
                currentGearRatio = 0f;
                if (_shiftCoroutine != null)
                {
                    vehicleController.StopCoroutine(_shiftCoroutine);
                }

                isShifting = false;
                isPostShiftBanActive = false;

                return true;
            }

            return false;
        }


        public override void VC_SetDefaults()
        {
            base.VC_SetDefaults();

            inertia = 0.02f;
            gears = new List<float> { -2.216f, 0f, 3.274f, 2.093f, 1.439f, 1.084f, 0.817f, 0.651f };
            triedToShiftWithoutClutch = new UnityEvent();
        }


        public override void VC_Validate(VehicleController vc)
        {
            base.VC_Validate(vc);

            if (gears == null || gears.Count == 0)
            {
                PC_LogWarning(
                    vc,
                    $"Gears list is empty on {vc.name}. Open the VehicleController > PWR > Transmission tab in Unity editor " +
                    $"to load the gears from the (now obsolete) gearing profile.");
                return;
            }

            if (gears.Count > 2)
            {
                if (!gears.Any(g => g > 0))
                {
                    PC_LogWarning(
                        vc,
                        "Gears list does not have any reverse gears. Reverse gears should be added first to the gears list and " +
                        "should be negative. Example gears list: -3, 0, 3, 2, 1.");
                }

                if (gears.All(g => g != 0))
                {
                    PC_LogWarning(
                        vc,
                        "There is no neutral gear. There should be one neutral gear in the gears list, placed in between the reverse" +
                        " (negative) and forward (positive) gears. Example gears list: -3, 0, 3, 2, 1.");
                }

                if (!gears.Any(g => g < 0))
                {
                    PC_LogWarning(
                        vc,
                        "Gears list does not have any forward gears. Forward gears should be added after the reverse (negative)" +
                        " and neutral (0) gears and should be positive. Example gears list: -3, 0, 3, 2, 1.");
                }
            }

            if (transmissionType == TransmissionShiftType.CVT)
            {
                if (gears.Count != 3)
                {
                    PC_LogWarning(
                        vc,
                        "CVT Transmission type requires 3 gears in the gears list, one reverse, one neutral and one forward. " +
                        "E.g. -3, 0, 2.");
                }
            }

            if (_upshiftRPM > vc.powertrain.engine.revLimiterRPM || _upshiftRPM > vc.powertrain.engine.revLimiterRPM)
            {
                PC_LogWarning(vc, $"Upshift RPM set to higher RPM than the engine can achieve (check revLimiterRPM).");
            }

            if (vc.powertrain.engine.engineType == EngineComponent.EngineType.ICE &&
                DownshiftRPM < vc.powertrain.engine.idleRPM)
            {
                PC_LogWarning(
                    vc,
                    $"Downshift RPM ({DownshiftRPM}) set to a lower value than the engine idle RPM ({vc.powertrain.engine.idleRPM}).");
            }
        }


        public void LoadGearsFromGearingProfile()
        {
            if (gearingProfile == null)
            {
                return;
            }

            int totalGears = gearingProfile.reverseGears.Count + 1 + gearingProfile.forwardGears.Count;
            if (gears == null)
            {
                gears = new List<float>(totalGears);
            }
            else
            {
                gears.Clear();
                gears.Capacity = totalGears;
            }

            gears.AddRange(gearingProfile.reverseGears);
            gears.Add(0);
            gears.AddRange(gearingProfile.forwardGears);
        }


        /// <summary>
        ///     Total gear ratio of the transmission for current gear.
        /// </summary>
        private float CalculateTotalGearRatio()
        {
            if (transmissionType == TransmissionShiftType.CVT)
            {
                float minRatio = gears[gearIndex];
                float maxRatio = minRatio * 40f;
                float t = Mathf.Clamp01(vehicleController.powertrain.engine.RPMPercent +
                                        (1f - vehicleController.powertrain.engine.ThrottlePosition));
                float ratio = Mathf.Lerp(maxRatio, minRatio, t) * finalGearRatio;
                return Mathf.Lerp(currentGearRatio, ratio, Time.fixedDeltaTime * 5f);
            }
            else
            {
                return gears[gearIndex] * finalGearRatio;
            }
        }


        /// <summary>
        /// Calculates the would-be RPM if none of the wheels was slipping.
        /// </summary>
        /// <returns>RPM as it would be if the wheels are not slipping or in the air.</returns>
        private float CalculateNoSlipRPM()
        {
            float vehicleLocalVelocity = vehicleController.LocalForwardVelocity;

            // Get the average no-slip wheel RPM
            // Use the vehicle velocity as the friction velocities for the wheel are 0 when in air and 
            // because the shift RPM is not really required to be extremely precise, so slight offset 
            // between the vehicle position and velocity and the wheel ones is not important.
            // Still, calculate for each wheel since radius might be different.
            float angVelSum = 0f;
            foreach (WheelComponent wheelComponent in vehicleController.powertrain.wheels)
            {
                angVelSum += vehicleLocalVelocity / wheelComponent.wheelUAPI.Radius;
            }

            // Apply total gear ratio to get the no-slip condition RPM
            return UnitConverter.AngularVelocityToRPM(angVelSum / vehicleController.powertrain.wheelCount) *
                   currentGearRatio;
        }


        /// <summary>
        ///     Total gear ratio of the transmission for the specific gear.
        ///     -1 for Rreverse, 0 for N, 1 for first gear, etc.
        /// </summary>
        /// <returns></returns>
        public float GetGearRatio(int g)
        {
            return gears[GearToIndex(g)] * finalGearRatio;
        }


        public override float QueryAngularVelocity(float angularVelocity, float dt)
        {
            inputAngularVelocity = angularVelocity;

            if (currentGearRatio == 0 || outputNameHash == 0)
            {
                outputAngularVelocity = 0f;
                return angularVelocity;
            }

            outputAngularVelocity = inputAngularVelocity / currentGearRatio;
            return _output.QueryAngularVelocity(outputAngularVelocity, dt) * currentGearRatio;
        }


        public override float QueryInertia()
        {
            if (outputNameHash == 0 || currentGearRatio == 0)
            {
                return inertia;
            }

            return inertia + _output.QueryInertia() / (currentGearRatio * currentGearRatio);
        }


        /// <summary>
        ///     Converts axle RPM to engine RPM for given gear in Gears list.
        /// </summary>
        public float ReverseTransmitRPM(float inputRPM, int g)
        {
            float outRpm = inputRPM * gears[GearToIndex(g)] * finalGearRatio;
            return Mathf.Abs(outRpm);
        }


        private void AssignShiftDelegate()
        {
            if (transmissionType == TransmissionShiftType.Manual)
            {
                shiftDelegate = ManualShift;
            }
            else if (transmissionType == TransmissionShiftType.Automatic)
            {
                shiftDelegate = AutomaticShift;
            }
            else if (transmissionType == TransmissionShiftType.CVT)
            {
                shiftDelegate = CVTShift;
            }
        }


        private void UpdateGearCounts()
        {
            forwardGearCount = 0;
            reverseGearCount = 0;
            int gearCount = gears.Count;
            for (int i = 0; i < gearCount; i++)
            {
                float gear = gears[i];
                if (gear > 0)
                {
                    forwardGearCount++;
                }
                else if (gear < 0)
                {
                    reverseGearCount++;
                }
            }
        }


        public override float ForwardStep(float torque, float inertiaSum, float dt)
        {
            inputTorque = torque;
            inputInertia = inertiaSum;

            if (_prevTransmissionType != transmissionType)
            {
                AssignShiftDelegate();
            }

            _prevTransmissionType = transmissionType;

            UpdateGearCounts();

            if (_output == null)
            {
                PC_LogWarning(vehicleController, "Transmission output is null.");
                return inputTorque;
            }

            // Update current gear ratio
            currentGearRatio = CalculateTotalGearRatio();

            // Run the shift function
            _referenceShiftRPM = CalculateNoSlipRPM();
            shiftDelegate.Invoke(vehicleController);

            // Reset any input related to shifting, now that the shifting has been processed
            vehicleController.input.ResetShiftFlags();

            // Run the physics step
            // No output, simply return the torque to the sender
            if (outputNameHash == 0)
            {
                return torque;
            }

            // In neutral, do not send any torque but update components downstram
            if (currentGearRatio < Vehicle.SMALL_NUMBER && currentGearRatio > -Vehicle.SMALL_NUMBER)
            {
                outputTorque = 0;
                outputInertia = inputInertia;
                _output.ForwardStep(outputTorque, outputInertia, dt);
                return torque;
            }

            // Always send torque to keep wheels updated
            outputTorque = torque * currentGearRatio;
            outputInertia = (inertiaSum + inertia) * (currentGearRatio * currentGearRatio);
            return _output.ForwardStep(torque * currentGearRatio, outputInertia, dt) / currentGearRatio;
        }


        /// <summary>
        ///     Shifts into given gear. 0 for neutral, less than 0 for reverse and above 0 for forward gears.
        ///     Does nothing if the target gear is equal to current gear.
        /// </summary>
        public void ShiftInto(int targetGear, bool instant = false)
        {
            // Clutch is not pressed above the set threshold, exit and do not shift.
            if (vehicleController.input.Clutch > clutchInputShiftThreshold)
            {
                triedToShiftWithoutClutch.Invoke();
                return;
            }

            int currentGear = Gear;
            bool isShiftFromOrToNeutral = targetGear == 0 || currentGear == 0;

            //Debug.Log($"Shift from {currentGear} into {targetGear}");

            // Check if shift can happen at all
            if (targetGear == currentGear || targetGear < -100 || _damage == 1f)
            {
                return;
            }

            // Convert gear to gear list index
            int targetIndex = GearToIndex(targetGear);

            // Check for gear list bounds
            if (targetIndex < 0 || targetIndex >= gears.Count)
            {
                return;
            }

            if (!isShifting && (isShiftFromOrToNeutral || !isPostShiftBanActive))
            {
                _shiftCoroutine = vehicleController.StartCoroutine(ShiftCoroutine(currentGear, targetGear,
                                                                       isShiftFromOrToNeutral || instant));

                // If in neutral reset the repeated input flat required for repeat input reverse
                if (targetGear == 0)
                {
                    _repeatInputFlag = false;
                }
            }
        }


        private IEnumerator ShiftCoroutine(int currentGear, int targetGear, bool instant)
        {
            if (isShifting)
            {
                yield return null;
            }

            float dt = 0.02f;
            bool isManual = transmissionType == TransmissionShiftType.Manual;

            //Debug.Log($"Shift from {currentGear} to {targetGear}, instant: {instant}");

            // Immediately start shift ban to prevent repeated shifts while this one has not finished
            if (!isManual)
            {
                isPostShiftBanActive = true;
            }

            isShifting = true;
            shiftProgress = 0f;

            // Run the first half of shift timer
            float shiftTimer = 0;
            float halfDuration = shiftDuration * 0.5f;
            if (!instant)
            {
                while (shiftTimer < halfDuration)
                {
                    shiftProgress = shiftTimer / shiftDuration;
                    shiftTimer += dt;
                    yield return new WaitForSeconds(dt);
                }
            }

            // Do the shift at the half point of shift duration
            Gear = targetGear;
            if (currentGear < targetGear)
            {
                onUpshift.Invoke();
            }
            else
            {
                onDownshift.Invoke();
            }

            onShift.Invoke();

            // Run the second half of the shift timer
            if (!instant)
            {
                while (shiftTimer < shiftDuration)
                {
                    shiftProgress = shiftTimer / shiftDuration;
                    shiftTimer += dt;
                    yield return new WaitForSeconds(dt);
                }
            }

            // Shift has finished
            shiftProgress = 1f;
            isShifting = false;

            // Run post shift ban only if not manual as blocking user input feels unresponsive and post shift ban
            // exists to prevent auto transmission from hunting.
            if (!isManual)
            {
                // Post shift ban timer
                float postShiftBanTimer = 0;
                while (postShiftBanTimer < postShiftBan)
                {
                    postShiftBanTimer += dt;
                    yield return new WaitForSeconds(dt);
                }

                // Post shift ban has finished
                isPostShiftBanActive = false;
            }
        }


        private void CVTShift(VehicleController vc)
        {
            AutomaticShift(vc);
        }


        /// <summary>
        ///     Handles automatic and automatic sequential shifting.
        /// </summary>
        private void AutomaticShift(VehicleController vc)
        {
            float vehicleSpeed = vc.Speed;
            float throttleInput = vc.input.InputSwappedThrottle;
            float brakeInput = vc.input.InputSwappedBrakes;
            int currentGear = Gear;

            // Assign base shift points
            _targetDownshiftRPM = _downshiftRPM;
            _targetUpshiftRPM = _upshiftRPM;

            // Calculate shift points for variable shift RPM
            if (variableShiftPoint)
            {
                // Smooth throttle input so that the variable shift point does not shift suddenly and cause gear hunting
                _smoothedThrottleInput = Mathf.Lerp(_smoothedThrottleInput, throttleInput, vc.fixedDeltaTime * 2f);
                float revLimiterRPM = vc.powertrain.engine.revLimiterRPM;

                _targetUpshiftRPM = _upshiftRPM +
                                    Mathf.Clamp01(_smoothedThrottleInput * variableShiftIntensity) * _upshiftRPM;
                _targetUpshiftRPM = Mathf.Clamp(_targetUpshiftRPM, _upshiftRPM, revLimiterRPM * 0.97f);

                _targetDownshiftRPM = _downshiftRPM +
                                      Mathf.Clamp01(_smoothedThrottleInput * variableShiftIntensity) * _downshiftRPM;
                _targetDownshiftRPM = Mathf.Clamp(_targetDownshiftRPM, vc.powertrain.engine.idleRPM * 1.1f,
                                                  _targetUpshiftRPM * 0.7f);

                // Add incline modifier
                float inclineModifier =
                    Mathf.Clamp01(Vector3.Dot(vc.vehicleTransform.forward, Vector3.up) * inclineEffectCoeff);
                _targetUpshiftRPM += revLimiterRPM * inclineModifier;
                _targetDownshiftRPM += revLimiterRPM * inclineModifier;
            }


            // In neutral
            if (currentGear == 0)
            {
                if (automaticTransmissionDNRShiftType == AutomaticTransmissionDNRShiftType.Auto)
                {
                    if (throttleInput > Vehicle.INPUT_DEADZONE)
                    {
                        ShiftInto(1);
                    }
                    else if (brakeInput > Vehicle.INPUT_DEADZONE)
                    {
                        ShiftInto(-1);
                    }
                }
                else if (automaticTransmissionDNRShiftType == AutomaticTransmissionDNRShiftType.RequireShiftInput)
                {
                    if (vc.input.ShiftUp || vc.input.ShiftInto == 1)
                    {
                        ShiftInto(1);
                    }
                    else if (vc.input.ShiftDown || vc.input.ShiftInto == -1)
                    {
                        ShiftInto(-1);
                    }
                }
                else if (automaticTransmissionDNRShiftType == AutomaticTransmissionDNRShiftType.RepeatInput)
                {
                    if (_repeatInputFlag == false && throttleInput < Vehicle.INPUT_DEADZONE && brakeInput < Vehicle.INPUT_DEADZONE)
                    {
                        _repeatInputFlag = true;
                    }

                    if (_repeatInputFlag)
                    {
                        if (throttleInput > Vehicle.INPUT_DEADZONE)
                        {
                            ShiftInto(1);
                        }
                        else if (brakeInput > Vehicle.INPUT_DEADZONE)
                        {
                            ShiftInto(-1);
                        }
                    }
                }
            }
            // In reverse
            else if (currentGear < 0)
            {
                // Shift into neutral
                if (automaticTransmissionDNRShiftType == AutomaticTransmissionDNRShiftType.RequireShiftInput)
                {
                    if (vc.input.ShiftUp || vc.input.ShiftInto == 0)
                    {
                        ShiftInto(0);
                    }
                    else if (vc.input.ShiftInto == 1)
                    {
                        ShiftInto(1);
                    }
                }
                else
                {
                    if (vehicleSpeed < dnrSpeedThreshold &&
                        (brakeInput > Vehicle.INPUT_DEADZONE || throttleInput < Vehicle.INPUT_DEADZONE))
                    {
                        ShiftInto(0);
                    }
                }

                // Reverse upshift
                float absGearMinusOne = currentGear - 1;
                absGearMinusOne = absGearMinusOne < 0 ? -absGearMinusOne : absGearMinusOne;
                if (_referenceShiftRPM > TargetUpshiftRPM && absGearMinusOne < reverseGearCount)
                {
                    ShiftInto(currentGear - 1);
                }
                // Reverse downshift
                else if (_referenceShiftRPM < TargetDownshiftRPM && currentGear < -1)
                {
                    ShiftInto(currentGear + 1);
                }
            }
            // In forward
            else
            {
                if (vehicleSpeed > 0.4f)
                {
                    // Upshift
                    if (currentGear < forwardGearCount && _referenceShiftRPM > TargetUpshiftRPM)
                    {
                        if (!isSequential && allowUpshiftGearSkipping)
                        {
                            int g = currentGear;

                            while (g < forwardGearCount)
                            {
                                g++;

                                float wouldBeEngineRPM = ReverseTransmitRPM(_referenceShiftRPM / currentGearRatio, g);
                                float shiftDurationPadding =
                                    Mathf.Clamp01(shiftDuration) * (_targetUpshiftRPM - _targetDownshiftRPM) * 0.25f;
                                if (wouldBeEngineRPM < _targetDownshiftRPM + shiftDurationPadding)
                                {
                                    g--;
                                    break;
                                }
                            }

                            if (g != currentGear)
                            {
                                ShiftInto(g);
                            }
                        }
                        else
                        {
                            ShiftInto(currentGear + 1);
                        }
                    }
                    // Downshift
                    else if (_referenceShiftRPM < TargetDownshiftRPM)
                    {
                        // Non-sequential
                        if (!isSequential && allowDownshiftGearSkipping)
                        {
                            if (currentGear != 1)
                            {
                                int g = currentGear;
                                while (g > 1)
                                {
                                    g--;
                                    float wouldBeEngineRPM =
                                        ReverseTransmitRPM(_referenceShiftRPM / currentGearRatio, g);
                                    if (wouldBeEngineRPM > _targetUpshiftRPM)
                                    {
                                        g++;
                                        break;
                                    }
                                }

                                if (g != currentGear)
                                {
                                    ShiftInto(g);
                                }
                            }
                            else if (vehicleSpeed < dnrSpeedThreshold && throttleInput < Vehicle.INPUT_DEADZONE
                                                                      && automaticTransmissionDNRShiftType !=
                                                                      AutomaticTransmissionDNRShiftType
                                                                         .RequireShiftInput)
                            {
                                ShiftInto(0);
                            }
                        }
                        // Sequential
                        else
                        {
                            if (currentGear != 1)
                            {
                                ShiftInto(currentGear - 1);
                            }
                            else if (vehicleSpeed < dnrSpeedThreshold && throttleInput < Vehicle.INPUT_DEADZONE &&
                                     brakeInput < Vehicle.INPUT_DEADZONE
                                     && automaticTransmissionDNRShiftType !=
                                     AutomaticTransmissionDNRShiftType.RequireShiftInput)
                            {
                                ShiftInto(0);
                            }
                        }
                    }
                }
                // Shift into neutral
                else
                {
                    if (automaticTransmissionDNRShiftType != AutomaticTransmissionDNRShiftType.RequireShiftInput)
                    {
                        if (throttleInput < Vehicle.INPUT_DEADZONE)
                        {
                            ShiftInto(0);
                        }
                    }
                    else
                    {
                        if (vc.input.ShiftDown || vc.input.ShiftInto == 0)
                        {
                            ShiftInto(0);
                        }
                        else if (vc.input.ShiftInto == -1 && vc.Speed < dnrSpeedThreshold)
                        {
                            ShiftInto(-1);
                        }
                    }
                }
            }
        }


        private int GearToIndex(int g)
        {
            return g + reverseGearCount;
        }


        private int IndexToGear(int g)
        {
            return g - reverseGearCount;
        }


        private void ManualShift(VehicleController vc)
        {
            if (vc.input.ShiftUp)
            {
                ShiftInto(Gear + 1);
                return;
            }

            if (vc.input.ShiftDown)
            {
                ShiftInto(Gear - 1);
                return;
            }

            int shiftIntoSignal = vc.input.ShiftInto;
            if (shiftIntoSignal > -100)
            {
                ShiftInto(shiftIntoSignal);
                _slipOutOfGearTimer = 0;
            }
            else if (holdToKeepInGear)
            {
                _slipOutOfGearTimer += vc.fixedDeltaTime;
                if (Gear != 0 && _slipOutOfGearTimer > 0.1f)
                {
                    ShiftInto(0);
                }
            }
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Powertrain
{
    [CustomPropertyDrawer(typeof(TransmissionComponent))]
    public partial class TransmissionComponentDrawer : PowertrainComponentDrawer
    {
        private TransmissionComponent tc;


        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            tc = SerializedPropertyHelper.GetTargetObjectOfProperty(property) as TransmissionComponent;
            SerializedProperty transmissionType = property.FindPropertyRelative("transmissionType");
            TransmissionComponent.TransmissionShiftType type =
                (TransmissionComponent.TransmissionShiftType)transmissionType.enumValueIndex;

            DrawCommonProperties();


            // Load the gears from the gearing profile
            if (tc.gearingProfile != null && (tc.gears == null || tc.gears.Count == 0))
            {
                tc.LoadGearsFromGearingProfile();
                tc.gearingProfile = null; // Obsolete, do not use gearing profile after the gears are loaded.
                EditorUtility.SetDirty(drawer.serializedObject.targetObject);
                drawer.serializedObject.ApplyModifiedProperties();
            }


            drawer.BeginSubsection("General");
            drawer.Field("transmissionType");
            drawer.EndSubsection();


            if (type != TransmissionComponent.TransmissionShiftType.CVT)
            {
                drawer.BeginSubsection("Shifting");
                if (!drawer.Field("isSequential").boolValue)
                {
                    drawer.Field("allowUpshiftGearSkipping");
                    drawer.Field("allowDownshiftGearSkipping");
                }

                drawer.Field("shiftDuration", true, "s");
                drawer.Field("postShiftBan", true, "s");
                drawer.Field("clutchInputShiftThreshold");

                if (Application.isPlaying)
                {
                    drawer.Label($"Status: " +
                                 $"{(tc.isShifting ? " Shifting" : "--------")} | " +
                                 $"{(tc.isPostShiftBanActive ? " Post Shift Ban" : "-------------")}");
                }

                if (type != TransmissionComponent.TransmissionShiftType.Manual)
                {
                    drawer.Field("automaticTransmissionDNRShiftType", true, null, "D/N/R Shift Type");
                    drawer.Field("dnrSpeedThreshold", true, null, "D/N/R Speed Threshold");
                    drawer.Field("_upshiftRPM", true, "rpm");
                    drawer.Field("_downshiftRPM", true, "rpm");
                    if (Application.isPlaying) drawer.Label("Reference RPM:\t" + tc.ReferenceShiftRPM);
                    if (drawer.Field("variableShiftPoint").boolValue)
                    {
                        drawer.Field("variableShiftIntensity");
                        drawer.Field("inclineEffectCoeff");
                        drawer.Info(
                            "High Incline Effect Coefficient values can prevent vehicle from changing gears as it is possible to get the Target Upshift RPM value higher than Rev Limiter RPM value. " +
                            "This is intentional to prevent heavy vehicles from upshifting on steep inclines.");
                        if (Application.isPlaying)
                        {
                            drawer.Label("Target Upshift RPM: " + tc.TargetUpshiftRPM);
                            drawer.Label("Target Downshift RPM: " + tc.TargetDownshiftRPM);
                        }
                    }

                    drawer.EndSubsection();
                }
                else
                {
                    drawer.Field("holdToKeepInGear");
                    drawer.Field("ignorePostShiftBanInManual");
                    drawer.EndSubsection();
                }
            }


            drawer.BeginSubsection("Gearing");
            if (Application.isPlaying)
            {
                drawer.Label($"Current gear: {tc.Gear}");
                drawer.Label($"Current gear ratio:\t{tc.currentGearRatio}");
            }

            drawer.Field("finalGearRatio");
            drawer.ReorderableList("gears");
            drawer.EndSubsection();


            // TOP SPEEDS PER GEAR
            VehicleController vc = property.serializedObject.targetObject as VehicleController;

            if (vc != null)
            {
                WheelUAPI wc = vc.gameObject.GetComponentInChildren<WheelUAPI>();

                if (wc != null)
                {
                    drawer.BeginSubsection("Top Speed Per Gear");

                    float revLimiterRPM = vc.powertrain.engine.revLimiterRPM;
                    float wheelRadius = wc.Radius;

                    int reverseGearCount = tc.gears.Count(g => g < 0);
                    for (int i = 0; i < tc.gears.Count; i++)
                    {
                        if (i == reverseGearCount)
                        {
                            continue;
                        }

                        float gearRatio = tc.gears[i];
                        float wheelRPM = revLimiterRPM / (gearRatio * tc.finalGearRatio);
                        float topSpeed = wheelRadius * (2f * Mathf.PI / 60f) * wheelRPM;
                        string prefix = i < reverseGearCount ? "R" : i == reverseGearCount ? "N" : "D";
                        drawer.Label($"{prefix}{Mathf.Abs(i - reverseGearCount)}:" +
                                     $"\t{topSpeed.ToString("0.0")} m/s | " +
                                     $"{(topSpeed * 3.6f).ToString("0.0")} km/h" +
                                     $" | {(topSpeed * 2.24f).ToString("0.0")} mph");
                    }

                    drawer.EndSubsection();
                }
            }

            EditorGUI.EndDisabledGroup();

            drawer.EndProperty();
            return true;
        }
    }
}

#endif