// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

using Pose = Microsoft.MixedReality.Toolkit.Utilities.MixedRealityPose;

namespace Microsoft.MixedReality.Toolkit.PoseMatching
{
    public abstract class HandTracker : InputSystemGlobalHandlerListener, IMixedRealitySourceStateHandler, IMixedRealityHandJointHandler
    {
        protected readonly List<IMixedRealityHand> trackedHands = new List<IMixedRealityHand>();

        public void OnSourceDetected(SourceStateEventData eventData)
        {
            var hand = eventData.Controller as IMixedRealityHand;
            if (hand != null)
            {
                StartTracking(hand);
            }
        }

        public void OnSourceLost(SourceStateEventData eventData)
        {
            var hand = eventData.Controller as IMixedRealityHand;
            if (hand != null)
            {
                StopTracking(hand);
            }
        }

        public void OnHandJointsUpdated(InputEventData<IDictionary<TrackedHandJoint, Pose>> eventData)
        {
            UpdateHandMatch(eventData.Handedness, eventData.InputData);
        }

        public new void OnDisable()
        {
            base.OnDisable();
            StopTracking();
        }

        private void StartTracking(IMixedRealityHand hand)
        {
            if (!trackedHands.Any(h => h.ControllerHandedness == hand.ControllerHandedness))
            {
                trackedHands.Add(hand);

                var handPose = PollHandPose(hand);
                UpdateHandMatch(hand.ControllerHandedness, handPose);
            }
        }

        private void StopTracking()
        {
            trackedHands.Clear();

            ClearHandMatch();
        }

        private void StopTracking(IMixedRealityHand hand)
        {
            trackedHands.RemoveAll(h => h == hand);

            ClearHandMatch();
        }

        private void StopTrackingAll(Predicate<IMixedRealityHand> pred)
        {
            trackedHands.RemoveAll(pred);

            ClearHandMatch();
        }

        protected abstract void UpdateHandMatch(Handedness handedness, IDictionary<TrackedHandJoint, Pose> joints);

        protected abstract void ClearHandMatch();

        protected static IDictionary<TrackedHandJoint, Pose> PollHandPose(IMixedRealityHand hand)
        {
            var joints = new Dictionary<TrackedHandJoint, Pose>();
            foreach (TrackedHandJoint joint in UsedJointValues)
            {
                if (hand.TryGetJoint(joint, out Pose pose))
                {
                    joints.Add(joint, pose);
                }
            }
            return joints;
        }

        protected static Vector3[] GetPointsFromJoints(IDictionary<TrackedHandJoint, Pose> joints)
        {
            Vector3[] points = new Vector3[UsedJointValues.Length];
            for (int i = 0; i < UsedJointValues.Length; ++i)
            {
                if (joints.TryGetValue(UsedJointValues[i], out Pose pose))
                {
                    points[i] = pose.Position;
                }
            }
            return points;
        }

        protected static IDictionary<TrackedHandJoint, Vector3> GetJointsFromPose(PoseConfiguration poseConfig)
        {
            var result = new Dictionary<TrackedHandJoint, Vector3>();
            for (int i = 0; i < UsedJointValues.Length; ++i)
            {
                result.Add(UsedJointValues[i], poseConfig.Targets[i]);
            }
            return result;
        }

        protected override void RegisterHandlers()
        {
            InputSystem?.RegisterHandler<IMixedRealitySourceStateHandler>(this);
            InputSystem?.RegisterHandler<IMixedRealityHandJointHandler>(this);
        }

        protected override void UnregisterHandlers()
        {
            InputSystem?.UnregisterHandler<IMixedRealitySourceStateHandler>(this);
            InputSystem?.UnregisterHandler<IMixedRealityHandJointHandler>(this);
        }

        protected static readonly TrackedHandJoint[] UsedJointValues = new TrackedHandJoint[]
        {
            // TrackedHandJoint.None,
            // TrackedHandJoint.Wrist,
            // TrackedHandJoint.Palm,
            TrackedHandJoint.ThumbMetacarpalJoint,
            TrackedHandJoint.ThumbProximalJoint,
            TrackedHandJoint.ThumbDistalJoint,
            TrackedHandJoint.ThumbTip,
            TrackedHandJoint.IndexMetacarpal,
            TrackedHandJoint.IndexKnuckle,
            TrackedHandJoint.IndexMiddleJoint,
            TrackedHandJoint.IndexDistalJoint,
            TrackedHandJoint.IndexTip,
            TrackedHandJoint.MiddleMetacarpal,
            TrackedHandJoint.MiddleKnuckle,
            TrackedHandJoint.MiddleMiddleJoint,
            TrackedHandJoint.MiddleDistalJoint,
            TrackedHandJoint.MiddleTip,
            TrackedHandJoint.RingMetacarpal,
            TrackedHandJoint.RingKnuckle,
            TrackedHandJoint.RingMiddleJoint,
            TrackedHandJoint.RingDistalJoint,
            TrackedHandJoint.RingTip,
            TrackedHandJoint.PinkyMetacarpal,
            TrackedHandJoint.PinkyKnuckle,
            TrackedHandJoint.PinkyMiddleJoint,
            TrackedHandJoint.PinkyDistalJoint,
            TrackedHandJoint.PinkyTip,
        };
    }
}