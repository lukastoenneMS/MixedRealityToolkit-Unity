// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;

#if UNITY_WSA
using UnityEngine;
using UnityEngine.XR.WSA.Input;
#endif

namespace Microsoft.MixedReality.Toolkit.WindowsMixedReality.Input
{
    /// <summary>
    /// A Windows Mixed Reality Source Instance.
    /// </summary>
    public abstract class BaseWindowsMixedRealitySource : BaseController
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="trackingState"></param>
        /// <param name="sourceHandedness"></param>
        /// <param name="inputSource"></param>
        /// <param name="interactions"></param>
        public BaseWindowsMixedRealitySource(TrackingState trackingState, Handedness sourceHandedness, IMixedRealityInputSource inputSource = null, MixedRealityInteractionMapping[] interactions = null)
                : base(trackingState, sourceHandedness, inputSource, interactions)
        {
        }

        /// <inheritdoc />
        public override MixedRealityInteractionMapping[] DefaultLeftHandedInteractions => DefaultInteractions;

        /// <inheritdoc />
        public override MixedRealityInteractionMapping[] DefaultRightHandedInteractions => DefaultInteractions;

        /// <inheritdoc />
        public override void SetupDefaultInteractions(Handedness controllerHandedness)
        {
            AssignControllerMappings(DefaultInteractions);
        }

#if UNITY_WSA

        /// <summary>
        /// The last updated source state reading for this Windows Mixed Reality Source.
        /// </summary>
        public InteractionSourceState LastSourceStateReading { get; protected set; }

        private MixedRealityPose currentSourcePose = MixedRealityPose.ZeroIdentity;
        private MixedRealityPose currentPointerPose = MixedRealityPose.ZeroIdentity;
        private MixedRealityPose currentGripPose = MixedRealityPose.ZeroIdentity;
        private MixedRealityPose lastSourcePose = MixedRealityPose.ZeroIdentity;

        #region Update data functions

        /// <summary>
        /// Update the source data from the provided platform state.
        /// </summary>
        /// <param name="interactionSourceState">The InteractionSourceState retrieved from the platform.</param>
        public virtual void UpdateController(InteractionSourceState interactionSourceState)
        {
            if (!Enabled) { return; }

            UpdateSourceData(interactionSourceState);
            UpdateVelocity(interactionSourceState);

            if (Interactions == null)
            {
                Debug.LogError($"No interaction configuration for Windows Mixed Reality {ControllerHandedness} Source");
                Enabled = false;
            }

            for (int i = 0; i < Interactions?.Length; i++)
            {
                switch (Interactions[i].InputType)
                {
                    case DeviceInputType.None:
                        break;
                    case DeviceInputType.SpatialPointer:
                        UpdatePointerData(interactionSourceState, Interactions[i]);
                        break;
                    case DeviceInputType.Select:
                    case DeviceInputType.Trigger:
                    case DeviceInputType.TriggerTouch:
                    case DeviceInputType.TriggerPress:
                        UpdateTriggerData(interactionSourceState, Interactions[i]);
                        break;
                    case DeviceInputType.SpatialGrip:
                        UpdateGripData(interactionSourceState, Interactions[i]);
                        break;
                }
            }

            LastSourceStateReading = interactionSourceState;
        }

        public void UpdateVelocity(InteractionSourceState interactionSourceState)
        {
            Vector3 newVelocity;
            bool isVelocityValid = interactionSourceState.sourcePose.TryGetVelocity(out newVelocity);
            if (isVelocityValid)
            {
                Velocity = newVelocity;
            }
            Vector3 newAngularVelocity;
            bool isAngularVelocityValid = interactionSourceState.sourcePose.TryGetAngularVelocity(out newAngularVelocity);
            if(isAngularVelocityValid)
            {
                AngularVelocity = newAngularVelocity;
            }
        }

        /// <summary>
        /// Update the source input from the device.
        /// </summary>
        /// <param name="interactionSourceState">The InteractionSourceState retrieved from the platform.</param>
        private void UpdateSourceData(InteractionSourceState interactionSourceState)
        {
            var lastState = TrackingState;
            var sourceKind = interactionSourceState.source.kind;

            lastSourcePose = currentSourcePose;

            if (sourceKind == InteractionSourceKind.Hand ||
               (sourceKind == InteractionSourceKind.Controller && interactionSourceState.source.supportsPointing))
            {
                // The source is either a hand or a controller that supports pointing.
                // We can now check for position and rotation.
                IsPositionAvailable = interactionSourceState.sourcePose.TryGetPosition(out Vector3 position);
                if (IsPositionAvailable)
                {
                    currentSourcePose.Position = position;
                    IsPositionApproximate = (interactionSourceState.sourcePose.positionAccuracy == InteractionSourcePositionAccuracy.Approximate);
                }
                else
                {
                    IsPositionApproximate = false;
                }

                IsRotationAvailable = interactionSourceState.sourcePose.TryGetRotation(out Quaternion rotation);
                if (IsRotationAvailable)
                {
                    currentSourcePose.Rotation = rotation;
                }

                // We want the source to follow the Playspace, so fold in the playspace transform here to 
                // put the source pose into world space.
                currentSourcePose.Position = MixedRealityPlayspace.TransformPoint(currentSourcePose.Position);
                currentSourcePose.Rotation = MixedRealityPlayspace.Rotation * currentSourcePose.Rotation;

                // Devices are considered tracked if we receive position OR rotation data from the sensors.
                TrackingState = (IsPositionAvailable || IsRotationAvailable) ? TrackingState.Tracked : TrackingState.NotTracked;
            }
            else
            {
                // The input source does not support tracking.
                TrackingState = TrackingState.NotApplicable;
            }

            // Raise input system events if it is enabled.
            if (lastState != TrackingState)
            {
                InputSystem?.RaiseSourceTrackingStateChanged(InputSource, this, TrackingState);
            }

            if (TrackingState == TrackingState.Tracked && lastSourcePose != currentSourcePose)
            {
                if (IsPositionAvailable && IsRotationAvailable)
                {
                    InputSystem?.RaiseSourcePoseChanged(InputSource, this, currentSourcePose);
                }
                else if (IsPositionAvailable && !IsRotationAvailable)
                {
                    InputSystem?.RaiseSourcePositionChanged(InputSource, this, currentSourcePose.Position);
                }
                else if (!IsPositionAvailable && IsRotationAvailable)
                {
                    InputSystem?.RaiseSourceRotationChanged(InputSource, this, currentSourcePose.Rotation);
                }
            }
        }

        /// <summary>
        /// Update the spatial pointer input from the device.
        /// </summary>
        /// <param name="interactionSourceState">The InteractionSourceState retrieved from the platform.</param>
        /// <param name="interactionMapping"></param>
        private void UpdatePointerData(InteractionSourceState interactionSourceState, MixedRealityInteractionMapping interactionMapping)
        {
            if (interactionSourceState.source.supportsPointing)
            {
                // We want the source to follow the Playspace, so fold in the playspace transform here to 
                // put the source pose into world space.
                if (interactionSourceState.sourcePose.TryGetPosition(out Vector3 position, InteractionSourceNode.Pointer))
                {
                    currentPointerPose.Position = MixedRealityPlayspace.TransformPoint(position);
                }
                if (interactionSourceState.sourcePose.TryGetRotation(out Quaternion rotation, InteractionSourceNode.Pointer))
                {
                    currentPointerPose.Rotation = MixedRealityPlayspace.Rotation * rotation;
                }
            }

            // Update the interaction data source
            interactionMapping.PoseData = currentPointerPose;

            // If our value changed raise it.
            if (interactionMapping.Changed)
            {
                // Raise input system Event if it enabled
                InputSystem?.RaisePoseInputChanged(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction, currentPointerPose);
            }
        }

        /// <summary>
        /// Update the spatial grip input from the device.
        /// </summary>
        /// <param name="interactionSourceState">The InteractionSourceState retrieved from the platform.</param>
        /// <param name="interactionMapping"></param>
        private void UpdateGripData(InteractionSourceState interactionSourceState, MixedRealityInteractionMapping interactionMapping)
        {
            switch (interactionMapping.AxisType)
            {
                case AxisType.SixDof:
                {
                    // We want the source to follow the Playspace, so fold in the playspace transform here to 
                    // put the source pose into world space.
                    if (interactionSourceState.sourcePose.TryGetPosition(out Vector3 position, InteractionSourceNode.Grip))
                    {
                        currentGripPose.Position = MixedRealityPlayspace.TransformPoint(position);
                    }
                    if (interactionSourceState.sourcePose.TryGetRotation(out Quaternion rotation, InteractionSourceNode.Grip))
                    {
                        currentGripPose.Rotation = MixedRealityPlayspace.Rotation * rotation;
                    }

                    // Update the interaction data source
                    interactionMapping.PoseData = currentGripPose;

                    // If our value changed raise it.
                    if (interactionMapping.Changed)
                    {
                        // Raise input system Event if it enabled
                        InputSystem?.RaisePoseInputChanged(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction, currentGripPose);
                    }
                }
                break;
            }
        }

        /// <summary>
        /// Update the trigger and grasped input from the device.
        /// </summary>
        /// <param name="interactionSourceState">The InteractionSourceState retrieved from the platform.</param>
        /// <param name="interactionMapping"></param>
        private void UpdateTriggerData(InteractionSourceState interactionSourceState, MixedRealityInteractionMapping interactionMapping)
        {
            switch (interactionMapping.InputType)
            {
                case DeviceInputType.TriggerPress:
                {
                    // Update the interaction data source
                    interactionMapping.BoolData = interactionSourceState.grasped;

                    // If our value changed raise it.
                    if (interactionMapping.Changed)
                    {
                        // Raise input system Event if it enabled
                        if (interactionMapping.BoolData)
                        {
                            InputSystem?.RaiseOnInputDown(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                        }
                        else
                        {
                            InputSystem?.RaiseOnInputUp(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                        }
                    }
                    break;
                }
                case DeviceInputType.Select:
                {
                    // Get the select pressed state, factoring in a workaround for Unity issue #1033526.
                    // When that issue is fixed, it should be possible change the line below to:
                    // interactionMapping.BoolData = interactionSourceState.selectPressed;
                    interactionMapping.BoolData = GetSelectPressedWorkaround(interactionSourceState);

                    // If our value changed raise it.
                    if (interactionMapping.Changed)
                    {
                        // Raise input system Event if it enabled
                        if (interactionMapping.BoolData)
                        {
                            InputSystem?.RaiseOnInputDown(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                        }
                        else
                        {
                            InputSystem?.RaiseOnInputUp(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                        }
                    }
                    break;
                }
                case DeviceInputType.Trigger:
                {
                    // Update the interaction data source
                    interactionMapping.FloatData = interactionSourceState.selectPressedAmount;

                    // If our value changed raise it.
                    if (interactionMapping.Changed)
                    {
                        // Raise input system Event if it enabled
                        InputSystem?.RaiseFloatInputChanged(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction, interactionSourceState.selectPressedAmount);
                    }
                    break;
                }
                case DeviceInputType.TriggerTouch:
                {
                    // Update the interaction data source
                    interactionMapping.BoolData = interactionSourceState.selectPressedAmount > 0;

                    // If our value changed raise it.
                    if (interactionMapping.Changed)
                    {
                        // Raise input system Event if it enabled
                        if (interactionSourceState.selectPressedAmount > 0)
                        {
                            InputSystem?.RaiseOnInputDown(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                        }
                        else
                        {
                            InputSystem?.RaiseOnInputUp(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                        }
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Gets whether or not 'select' has been pressed.
        /// </summary>
        /// <remarks>
        /// This includes a workaround to fix air-tap gestures in HoloLens 1 remoting, to work around the following Unity issue:
        /// https://issuetracker.unity3d.com/issues/hololens-interactionsourcestate-dot-selectpressed-is-false-when-air-tap-and-hold
        /// Bug was discovered May 2018 and still exists as of May 2019 in version 2018.3.11f1. This workaround is scoped to only
        /// cases where remoting is active.
        /// </remarks>
        private bool GetSelectPressedWorkaround(InteractionSourceState interactionSourceState)
        {
            bool selectPressed = interactionSourceState.selectPressed;
            if (interactionSourceState.source.kind == InteractionSourceKind.Hand && 
                UnityEngine.XR.WSA.HolographicRemoting.ConnectionState == UnityEngine.XR.WSA.HolographicStreamerConnectionState.Connected)
            {
                // This workaround is safe as long as all these assumptions hold:
                Debug.Assert(!interactionSourceState.selectPressed, "Unity issue #1033526 seems to have been resolved. Please remove this workaround!");
                Debug.Assert(!interactionSourceState.source.supportsGrasp);
                Debug.Assert(!interactionSourceState.source.supportsMenu);
                Debug.Assert(!interactionSourceState.source.supportsPointing);
                Debug.Assert(!interactionSourceState.source.supportsThumbstick);
                Debug.Assert(!interactionSourceState.source.supportsTouchpad);

                selectPressed = interactionSourceState.anyPressed;
            }
            return selectPressed;
        }

        #endregion Update data functions

#endif // UNITY_WSA
    }
}
