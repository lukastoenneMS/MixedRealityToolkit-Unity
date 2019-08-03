// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;

using Pose = Microsoft.MixedReality.Toolkit.Utilities.MixedRealityPose;

namespace Microsoft.MixedReality.Toolkit.PoseMatching
{
    public class PoseRecorder : HandTracker
    {
        public const float MinimumError = 0.0001f;
        public float GoodMatchError = 0.01f;
        public float SloppyMatchError = 0.05f;

        public GameObject JointIndicatorPrefab;
        public TextMeshPro InfoText;
        public AudioSource Claxon;

        public PoseConfiguration PoseConfig { get; private set; }
        public Handedness PoseHandedness { get; private set; }

        private readonly PoseEvaluator evaluator = new PoseEvaluator();
        private readonly Dictionary<TrackedHandJoint, GameObject> jointIndicators = new Dictionary<TrackedHandJoint, GameObject>();
        private GameObject matchIndicator;
        private MaterialPropertyBlock materialProps;

        private System.Diagnostics.Stopwatch debugStopwatch = new System.Diagnostics.Stopwatch();

        public void InitPoseConfig()
        {
            if (IsTracking)
            {
                PoseHandedness = TrackedHand.ControllerHandedness;

                var points = GetPointsFromJoints(PollHandPose(TrackedHand));
                PoseConfig = new PoseConfiguration(points);
            }
        }

        public void DiscardPoseConfig()
        {
            PoseConfig = null;
            PoseHandedness = Handedness.None;
        }

        public void LimitWeightsToGoodMatch()
        {
            LimitWeights(GoodMatchError);
        }

        public void LimitWeightsToSloppyMatch()
        {
            LimitWeights(SloppyMatchError);
        }

        public void LimitWeights(float maxError)
        {
            if (PoseConfig != null)
            {
                if (IsTracking)
                {
                    var joints = PollHandPose(TrackedHand);
                    Vector3[] points = GetPointsFromJoints(joints);
                    PoseMatch match = evaluator.EvaluatePose(points, PoseConfig);
                    PoseConfig = evaluator.GetErrorLimitedConfig(points, PoseConfig, match, maxError);
                }
            }
        }

        public void ResetWeights()
        {
            if (PoseConfig != null)
            {
                PoseConfig = new PoseConfiguration(PoseConfig.Targets);
            }
        }

        public void SavePoseConfiguration()
        {
            if (PoseConfig != null)
            {
                string baseName = "PoseConfig";
                string filename = String.Format("{0}-{1}.{2}", baseName, DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"), ".json");
                string filepath = Path.Combine(Application.persistentDataPath, filename);

                string[] allNames = Enum.GetNames(typeof(TrackedHandJoint));
                string[] identifiers = UsedJointValues.Select(joint => allNames[(int)joint]).ToArray();

                PoseSerializationUtils.Serialize(filepath, PoseConfig, identifiers);
            }
        }

        void OnValidate()
        {
            if (GoodMatchError <= MinimumError)
            {
                GoodMatchError = MinimumError;
            }
            if (SloppyMatchError <= MinimumError)
            {
                SloppyMatchError = MinimumError;
            }
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

            if (InfoText)
            {
                InfoText.text = "...";
            }

            materialProps = new MaterialPropertyBlock();

            debugStopwatch.Start();
        }

        protected override void UpdateHandMatch(Handedness handedness, IDictionary<TrackedHandJoint, Pose> joints)
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

                Vector3[] result = points.Select(p => match.Offset.Multiply(p)).ToArray();
                MathUtils.ComputeResiduals(result, PoseConfig.Targets, PoseConfig.Weights, out float[] residuals, out float MSE);
                // string summary = $"{Time.time}: condition={match.ConditionNumber} MSE={MSE}";

                matchIndicator.transform.SetPositionAndRotation(match.Offset.Position, match.Offset.Rotation);

                for (int i = 0; i < UsedJointValues.Length; ++i)
                {
                    TrackedHandJoint joint = UsedJointValues[i];
                    if (jointIndicators.TryGetValue(joint, out GameObject jointOb))
                    {
                        jointOb.SetActive(true);
                        jointOb.transform.localPosition = PoseConfig.Targets[i];
                        // jointOb.transform.localScale = Vector3.one * PoseConfig.Weights[i];
                        // Use volume instead of radius for visualizing weight (keeps small weights visible too)
                        jointOb.transform.localScale = Vector3.one * Mathf.Pow(PoseConfig.Weights[i], 0.33333f);

                        float mix = GetMixFactor(residuals[i]);
                        Color color = Color.green * mix + Color.red * (1.0f - mix);
                        // summary += $"\n   {i}: R={residuals[i]}, mix={mix} | DELTA={(jointOb.transform.position - points[i]).magnitude}";
                        materialProps.SetColor("_Color", color);
                        jointOb.GetComponentInChildren<Renderer>().SetPropertyBlock(materialProps);
                    }
                }

                if (InfoText)
                {
                    InfoText.text =
                        $"Mean Error = {Mathf.Sqrt(MSE):F5}m\n" +
                        $"Condition = {match.ConditionNumber:F5}\n";
                }

                // if (debugStopwatch.Elapsed.TotalSeconds > 3.0f)
                // {
                //     debugStopwatch.Restart();
                //     Debug.Log(summary);
                // }
            }
        }

        protected override void ClearHandMatch()
        {
            foreach (var item in jointIndicators)
            {
                var jointOb = item.Value;
                jointOb.SetActive(false);
            }
        }

        private const float mixFactorExpected = 0.15f;
        private static float mixFactorExp = -Mathf.Log(mixFactorExpected);
        private float GetMixFactor(float residual)
        {
            return Mathf.Exp(-residual / SloppyMatchError * mixFactorExp);
        }
    }
}