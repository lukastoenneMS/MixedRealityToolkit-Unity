// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Pose = Microsoft.MixedReality.Toolkit.Utilities.MixedRealityPose;

namespace Microsoft.MixedReality.Toolkit.PoseMatching
{
    public class PoseRecorder : InputSystemGlobalHandlerListener, IMixedRealitySourceStateHandler, IMixedRealityHandJointHandler
    {
        public GameObject JointIndicatorPrefab;

        public PoseConfiguration PoseConfig { get; private set; }
        public Handedness PoseHandedness { get; private set; }

        private readonly Dictionary<TrackedHandJoint, GameObject> jointIndicators = new Dictionary<TrackedHandJoint, GameObject>();

        private static readonly TrackedHandJoint[] jointValues = new TrackedHandJoint[]
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

        private readonly List<IMixedRealityHand> trackedHands = new List<IMixedRealityHand>();

        public void InitPoseConfig()
        {
            var hand = trackedHands.Last();
            if (hand != null)
            {
                PoseHandedness = hand.ControllerHandedness;

                PoseConfig = new PoseConfiguration();
                PoseConfig.Init(PollHandPose(hand).Select(item => item.Value.Position));
            }
        }

        public void DiscardPoseConfig()
        {
            PoseConfig = null;
            PoseHandedness = Handedness.None;
        }

        void Awake()
        {
            if (JointIndicatorPrefab)
            {
                foreach (TrackedHandJoint joint in jointValues)
                {
                    var jointOb = GameObject.Instantiate(JointIndicatorPrefab, transform);
                    jointIndicators.Add(joint, jointOb);
                    jointOb.SetActive(false);
                }
            }
        }

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

        private static IDictionary<TrackedHandJoint, Pose> PollHandPose(IMixedRealityHand hand)
        {
            var joints = new Dictionary<TrackedHandJoint, Pose>();
            foreach (TrackedHandJoint joint in jointValues)
            {
                if (hand.TryGetJoint(joint, out Pose pose))
                {
                    joints.Add(joint, pose);
                }
            }
            return joints;
        }

        private void UpdateHandMatch(Handedness handedness, IDictionary<TrackedHandJoint, Pose> joints)
        {
            if (PoseConfig == null)
            {
                ClearHandMatch();
                return;
            }

            if (handedness == PoseHandedness)
            {
                foreach (var item in jointIndicators)
                {
                    var jointOb = item.Value;
                    if (joints.TryGetValue(item.Key, out Pose pose))
                    {
                        jointOb.SetActive(true);
                        jointOb.transform.position = pose.Position;
                        jointOb.transform.rotation = pose.Rotation;
                    }
                    else
                    {
                        jointOb.SetActive(false);
                    }
                }
            }
        }

        private void ClearHandMatch()
        {
            foreach (var item in jointIndicators)
            {
                var jointOb = item.Value;
                jointOb.SetActive(false);
            }
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
    }
}