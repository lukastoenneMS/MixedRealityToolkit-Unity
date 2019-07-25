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
    [RequireComponent(typeof(MeshFilter))]
    public class CurveRecorder : HandTracker
    {
        public TextMeshPro InfoText;
        public AudioSource Claxon;
        public GameObject ShapeObject;
        private GameObject[] ShapeObjectSteps;

        public TrackedHandJoint TrackedJoint = TrackedHandJoint.IndexTip;
        public float SamplingDistance = 0.03f;
        public float MaxCurveLength = 3.0f;
        public int MaxSamples = 200;

        public int RenderResolution = 6;
        public float RenderThickness = 0.003f;

        public SplineCurve Curve { get; private set; }
        public Handedness CurveHandedness { get; private set; }
        private Vector3? lastPosition;
        private float movedDistance;

        private MeshFilter meshFilter;
        private MaterialPropertyBlock materialProps;

        void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();

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

            if (ShapeObject)
            {
#if false
                int numSteps = 3;
                FindCurveMatchSteps(numSteps, out Pose[] targetOffset, out float[] MSE);
                if (ShapeObjectSteps == null)
                {
                    ShapeObjectSteps = new GameObject[numSteps];
                    for (int i = 0; i < numSteps; ++i)
                    {
                        ShapeObjectSteps[i] = GameObject.Instantiate(ShapeObject);
                        ShapeObjectSteps[i].name = $"ICP Step {i}";
                        var renderer = ShapeObjectSteps[i].GetComponentInChildren<MeshRenderer>();
                        if (renderer)
                        {
                            float mix = (float)i / (float)(numSteps - 1);
                            materialProps.SetColor("_Color", Color.green * mix + Color.red * (1.0f - mix));
                            renderer.SetPropertyBlock(materialProps);
                        }
                    }
                }
                for (int i = 0; i < numSteps; ++i)
                {
                    ShapeObjectSteps[i].transform.position = targetOffset[i].Multiply(transform.position);
                    ShapeObjectSteps[i].transform.rotation = targetOffset[i].Multiply(transform.rotation);
                }
#else
                if (FindCurveMatch(out Pose targetOffset))
                {
                    ShapeObject.SetActive(true);
                    ShapeObject.transform.position = targetOffset.Multiply(transform.position);
                    ShapeObject.transform.rotation = targetOffset.Multiply(transform.rotation);
                }
                else
                {
                    ShapeObject.SetActive(false);
                }
#endif
            }

            if (meshFilter)
            {
                if (meshFilter.sharedMesh == null)
                {
                    meshFilter.mesh = new Mesh();
                }

                CurveMeshUtils.UpdateMesh(meshFilter.sharedMesh, Curve, RenderResolution, RenderThickness);
            }

            if (InfoText)
            {
                // InfoText.text =
                //     $"Mean Error = {Mathf.Sqrt(MSE):F5}m\n" +
                //     $"Condition = {match.ConditionNumber:F5}\n";
            }
        }

        private readonly ICPSolver icpSolver = new ICPSolver();

        private class CircleTestShape : ICPShape
        {
            public void FindClosestPoints(Vector3[] points, Vector3[] result)
            {
                float radius = 0.2f;
                for (int i = 0; i < points.Length; ++i)
                {
                    float x = points[i].x;
                    float z = points[i].z;
                    result[i] = new Vector3(x, 0, z).normalized * radius;
                }
            }
        }

        private bool FindCurveMatch(out Pose result)
        {
            Vector3[] points = Curve.ControlPoints.Select(cp => cp.position).ToArray();
            var shape = new CircleTestShape();

            icpSolver.Solve(points, shape);

            result = icpSolver.TargetOffset;
            return icpSolver.Iterations < icpSolver.MaxIterations;
        }

        private bool FindCurveMatchSteps(int steps, out Pose[] result, out float[] MSE)
        {
            Vector3[] points = Curve.ControlPoints.Select(cp => cp.position).ToArray();
            var shape = new CircleTestShape();

            result = new Pose[steps];
            MSE = new float[steps];

            // icpSolver.Solve(points, shape);
            icpSolver.Init(points, shape);
            for (int i = 0; i < steps; ++i)
            {
                icpSolver.SolveStep();

                result[i] = icpSolver.TargetOffset;
                MSE[i] = icpSolver.MeanSquareError;
            }

            return icpSolver.Iterations < icpSolver.MaxIterations;
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
