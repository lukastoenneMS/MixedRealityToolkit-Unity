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

        private readonly List<IMixedRealityHand> trackedHands = new List<IMixedRealityHand>();

        private readonly PoseEvaluator evaluator = new PoseEvaluator();
        private readonly Dictionary<TrackedHandJoint, GameObject> jointIndicators = new Dictionary<TrackedHandJoint, GameObject>();
        private GameObject matchIndicator;
        private MaterialPropertyBlock materialProps;

        private System.Diagnostics.Stopwatch debugStopwatch = new System.Diagnostics.Stopwatch();

        public void InitPoseConfig()
        {
            var hand = trackedHands.LastOrDefault();
            if (hand != null)
            {
                PoseHandedness = hand.ControllerHandedness;

                var points = GetPointsFromJoints(PollHandPose(hand));
                PoseConfig = new PoseConfiguration();
                PoseConfig.Init(points);
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
                matchIndicator = new GameObject("PoseMatchIndicator");
                matchIndicator.transform.SetParent(transform);

                foreach (TrackedHandJoint joint in UsedJointValues)
                {
                    var jointOb = GameObject.Instantiate(JointIndicatorPrefab, matchIndicator.transform);
                    jointIndicators.Add(joint, jointOb);
                    jointOb.SetActive(false);
                }
            }

            materialProps = new MaterialPropertyBlock();

            debugStopwatch.Start();
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
            foreach (TrackedHandJoint joint in UsedJointValues)
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
                Vector3[] points = GetPointsFromJoints(joints);
                PoseMatch match = evaluator.EvaluatePose(points, PoseConfig);

                evaluator.ComputeResiduals(points, PoseConfig, match, out float[] residuals, out float MSE);
                string summary = $"{Time.time}: condition={match.ConditionNumber} MSE={MSE}";

                matchIndicator.transform.SetPositionAndRotation(match.Offset.Position, match.Offset.Rotation);

                for (int i = 0; i < UsedJointValues.Length; ++i)
                {
                    TrackedHandJoint joint = UsedJointValues[i];
                    if (jointIndicators.TryGetValue(joint, out GameObject jointOb))
                    {
                        jointOb.SetActive(true);
                        jointOb.transform.localPosition = PoseConfig.Targets[i];
                        jointOb.transform.localScale = Vector3.one * PoseConfig.Weights[i];

                        float mix = GetMixFactor(residuals[i]);
                        Color color = Color.green * mix + Color.red * (1.0f - mix);
                        summary += $"\n   {i}: R={residuals[i]}, mix={mix} | DELTA={(jointOb.transform.position - points[i]).magnitude}";
                        materialProps.SetColor("_Color", color);
                        jointOb.GetComponentInChildren<Renderer>().SetPropertyBlock(materialProps);
                    }
                }

                if (debugStopwatch.Elapsed.TotalSeconds > 3.0f)
                {
                    debugStopwatch.Restart();
                    Debug.Log(summary);
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

        private static Vector3[] GetPointsFromJoints(IDictionary<TrackedHandJoint, Pose> joints)
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

        public float ExpectedMaximumError = 0.05f;
        private const float mixFactorExpected = 0.15f;
        private static float mixFactorExp = -Mathf.Log(mixFactorExpected);
        private float GetMixFactor(float residual)
        {
            float invSqrExpectedMaximumError = ExpectedMaximumError > 0.0f ? 1.0f / (ExpectedMaximumError * ExpectedMaximumError) : float.MaxValue;
            return ExpectedMaximumError > 0.0f ? Mathf.Exp(-residual * invSqrExpectedMaximumError * mixFactorExp) : 0.0f;
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

        private static readonly TrackedHandJoint[] UsedJointValues = new TrackedHandJoint[]
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