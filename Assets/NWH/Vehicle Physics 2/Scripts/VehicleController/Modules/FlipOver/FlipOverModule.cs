using System;
using System.Collections;
using NWH.VehiclePhysics2.Powertrain;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using NWH.NUI;
#endif

namespace NWH.VehiclePhysics2.Modules.FlipOver
{
    /// <summary>
    ///     Flip over module. Flips the vehicle over to be the right side up if needed.
    /// </summary>
    [Serializable]
    public partial class FlipOverModule : VehicleComponent
    {
        public enum FlipOverType { Gradual, Instant }

        public enum FlipOverActivation { Manual, Automatic }

        /// <summary>
        /// Determines how the vehicle will be flipped over. 
        /// </summary>
        [UnityEngine.Tooltip("Determines how the vehicle will be flipped over. ")]
        public FlipOverType flipOverType = FlipOverType.Instant;

        public FlipOverActivation flipOverActivation = FlipOverActivation.Manual;

        /// <summary>
        ///     Minimum angle that the vehicle needs to be at for it to be detected as flipped over.
        /// </summary>
        [Tooltip("    Minimum angle that the vehicle needs to be at for it to be detected as flipped over.")]
        public float allowedAngle = 70f;

        /// <summary>
        /// If using instant (not gradual) flip over this value will be applied to the transform.y position to prevent rotating
        /// the object to a position that is underground.
        /// </summary>
        [UnityEngine.Tooltip("If using instant (not gradual) flip over this value will be applied to the transform.y position to prevent rotating\r\nthe object to a position that is underground.")]
        public float instantFlipOverVerticalOffset = 1f;

        /// <summary>
        ///     Is the vehicle flipped over?
        /// </summary>
        [Tooltip("    Is the vehicle flipped over?")]
        public bool flippedOver;

        /// <summary>
        ///     Flip over detection will be disabled if velocity is above this value [m/s].
        /// </summary>
        [Tooltip("    Flip over detection will be disabled if velocity is above this value [m/s].")]
        public float maxDetectionSpeed = 0.6f;

        /// <summary>
        ///     Time after detecting flip over after which vehicle will be flipped back.
        /// </summary>
        [Tooltip(
            "Time after detecting flip over after which vehicle will be flipped back or the manual button can be used.")]
        public float timeout = 1f;

        /// <summary>
        /// How long the flip over process will take if using gradual flip over.
        /// </summary>
        [Tooltip("How long the flip over process will take if using gradual flip over.")]
        public float flipOverDuration = 5f;

        private bool _flipOverInProgress = false;

        public override bool VC_Enable(bool calledByParent)
        {
            if (base.VC_Enable(calledByParent))
            {
                vehicleController.StartCoroutine(FlipOverCheckCoroutine());
                return true;
            }

            return false;
        }

        public override bool VC_Disable(bool calledByParent)
        {
            if (base.VC_Enable(calledByParent))
            {
                vehicleController.StopCoroutine(FlipOverCheckCoroutine());
                return true;
            }

            return false;
        }

        private IEnumerator FlipOverCheckCoroutine()
        {
            while (true)
            {
                float vehicleAngle = Vector3.Angle(vehicleController.transform.up, -Physics.gravity.normalized);
                flippedOver = vehicleController.Speed < maxDetectionSpeed
                             && vehicleController.vehicleRigidbody.angularVelocity.magnitude < maxDetectionSpeed
                             && vehicleAngle > allowedAngle;

                if (!_flipOverInProgress && flippedOver
                    && ((vehicleController.input.FlipOver && flipOverActivation == FlipOverActivation.Manual) || flipOverActivation == FlipOverActivation.Automatic))
                {
                    if (flipOverType == FlipOverType.Gradual)
                    {
#if NVP2_DEBUG
                        Debug.Log("Flipping over gradually.");
#endif
                        vehicleController.StartCoroutine(FlipOverGraduallyCoroutine());
                    }
                    else
                    {
#if NVP2_DEBUG
                        Debug.Log("Flipping over instantly.");
#endif
                        FlipOverInstantly();
                        yield return new WaitForSeconds(1);
                    }
                }

                vehicleController.input.FlipOver = false;

                yield return new WaitForSeconds(timeout);
            }
        }


        private IEnumerator FlipOverGraduallyCoroutine()
        {
            float timer = 0;
            RigidbodyConstraints initConstraints = vehicleController.vehicleRigidbody.constraints;
            Quaternion initRotation = vehicleController.transform.rotation;
            Quaternion targetRotation = Mathf.Abs(Vector3.Dot(vehicleController.transform.forward, Vector3.up)) < 0.7f ?
                Quaternion.LookRotation(vehicleController.transform.forward, Vector3.up) :
                Quaternion.LookRotation(vehicleController.transform.up, Vector3.up);

            float initialDrag = vehicleController.vehicleRigidbody.linearDamping;
            float initialAngularDrag = vehicleController.vehicleRigidbody.angularDamping;
            vehicleController.vehicleRigidbody.linearDamping = 30f;
            vehicleController.vehicleRigidbody.angularDamping = 30f;

            while (timer < 20f)
            {
                float progress = timer / flipOverDuration;
                if (progress > 1f)
                {
                    vehicleController.vehicleRigidbody.constraints = initConstraints;
                    _flipOverInProgress = false;
                    break;
                }

                vehicleController.vehicleRigidbody.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
                vehicleController.vehicleRigidbody.MoveRotation(Quaternion.Slerp(initRotation, targetRotation, progress));
                _flipOverInProgress = true;

                timer += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            timer = 0f;
            while (timer < 1f)
            {
                vehicleController.vehicleRigidbody.linearDamping = Mathf.Lerp(30f, initialDrag, timer);
                vehicleController.vehicleRigidbody.angularDamping = Mathf.Lerp(30f, initialAngularDrag, timer);

                timer += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }


            vehicleController.vehicleRigidbody.linearDamping = initialDrag;
            _flipOverInProgress = false;

            yield return null;
        }


        private void FlipOverInstantly()
        {
            Quaternion targetRotation = Mathf.Abs(Vector3.Dot(vehicleController.transform.forward, Vector3.up)) < 0.7f ?
                Quaternion.LookRotation(vehicleController.transform.forward, Vector3.up) :
                Quaternion.LookRotation(vehicleController.transform.up, Vector3.up);
            vehicleController.vehicleRigidbody.MoveRotation(targetRotation);
            vehicleController.vehicleRigidbody.MovePosition(vehicleController.transform.position + Vector3.up * instantFlipOverVerticalOffset);
        }
    }
}


#if UNITY_EDITOR
namespace NWH.VehiclePhysics2.Modules.FlipOver
{
    [CustomPropertyDrawer(typeof(FlipOverModule))]
    public partial class FlipOverModuleDrawer : ComponentNUIPropertyDrawer
    {
        public override bool OnNUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!base.OnNUI(position, property, label))
            {
                return false;
            }

            drawer.Field("flipOverActivation");
            drawer.Field("flipOverType");
            drawer.Field("instantFlipOverVerticalOffset");
            drawer.Field("timeout");
            drawer.Field("allowedAngle");
            drawer.Field("maxDetectionSpeed");
            drawer.Field("flippedOver", false);

            drawer.EndProperty();
            return true;
        }
    }
}
#endif
