// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
    public class CurveRecorder : HandTracker
    {
        public TextMeshPro InfoText;
        public AudioSource Claxon;

        public TrackedHandJoint TrackedJoint = TrackedHandJoint.IndexTip;
        public float SamplingDistance = 0.03f;
        public float MaxCurveLength = 3.0f;
        public int MaxSamples = 200;

        public SplineCurve Curve { get; private set; }
        public Handedness CurveHandedness { get; private set; }
        private Vector3? lastPosition;
        private float movedDistance;

        private MaterialPropertyBlock materialProps;

        void Awake()
        {
            // if (JointIndicatorPrefab)
            // {
            //     matchIndicator = new GameObject("PoseMatchIndicator");
            //     matchIndicator.transform.SetParent(transform);

            //     foreach (TrackedHandJoint joint in UsedJointValues)
            //     {
            //         var jointOb = GameObject.Instantiate(JointIndicatorPrefab, matchIndicator.transform);
            //         jointIndicators.Add(joint, jointOb);
            //         jointOb.SetActive(false);
            //     }
            // }

            if (InfoText)
            {
                InfoText.text = "...";
            }

            materialProps = new MaterialPropertyBlock();
        }

        protected override void UpdateHandMatch(Handedness handedness, IDictionary<TrackedHandJoint, Pose> joints)
        {
            if (handedness != TrackedHand.ControllerHandedness)
            {
                return;
            }

            if (Curve == null)
            {
                Curve = new SplineCurve();
                CurveHandedness = handedness;
                lastPosition = null;
                movedDistance = 0.0f;
            }

            if (joints.TryGetValue(TrackedJoint, out Pose trackedPose))
            {
                if (lastPosition.HasValue)
                {
                    Vector3 delta = trackedPose.Position - lastPosition.Value;
                    movedDistance += delta.magnitude;
                }
                else
                {
                    movedDistance = 0.0f;
                }
                lastPosition = trackedPose.Position;

                if (movedDistance >= SamplingDistance)
                {
                    Curve.Append(trackedPose.Position);
                    movedDistance = 0.0f;

                    if (Curve.Count > MaxSamples)
                    {
                        Curve.RemoveRange(0, Curve.Count - MaxSamples);
                    }
                    if (Curve.ArcLength > MaxCurveLength)
                    {
                        if (Curve.TryFindControlPoint(Curve.ArcLength - MaxCurveLength, out int numRemove))
                        {
                            Curve.RemoveRange(0, numRemove);
                        }
                        else
                        {
                            Curve.Clear();
                        }
                    }
                }
            }

            if (handedness == CurveHandedness)
            {
                if (InfoText)
                {
                    // InfoText.text =
                    //     $"Mean Error = {Mathf.Sqrt(MSE):F5}m\n" +
                    //     $"Condition = {match.ConditionNumber:F5}\n";
                }
            }
        }

        protected override void ClearHandMatch()
        {
            Curve = null;
            CurveHandedness = Handedness.None;
            lastPosition = null;
            movedDistance = 0.0f;

            // foreach (var item in jointIndicators)
            // {
            //     var jointOb = item.Value;
            //     jointOb.SetActive(false);
            // }
        }
    }
}
