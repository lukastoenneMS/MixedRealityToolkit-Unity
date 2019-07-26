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
        public GameObject FollowerPrefab;
        public GameObject ShapeVisualizerPrefab;

        public TrackedHandJoint TrackedJoint = TrackedHandJoint.IndexTip;
        public float SamplingDistance = 0.03f;
        public float MaxRecordingTime = 0.8f;
        public float MaxCurveLength = 3.0f;
        public int MaxSamples = 200;

        public SplineCurve Curve { get; private set; }
        public Handedness CurveHandedness { get; private set; }
        private Vector3? lastPosition;
        private float movedDistance;

        public readonly ICPShape[] shapes = new ICPShape[]
        {
            LineShapeUtils.CreateCircle(0.2f, 16),
        };

        public float MeanErrorThreshold = 0.05f;

        private bool curveDirty = false;
        private SplineCurveRenderer curveRenderer;
        private GameObject follower;

        void Awake()
        {
            curveRenderer = GetComponentInChildren<SplineCurveRenderer>();

            if (InfoText)
            {
                InfoText.text = "...";
            }
        }

        void Update()
        {
            if (Curve != null)
            {
                float currentTime = Time.time;
                float arcLength = Curve.ArcLength;
                int numRemoved = Curve.RemoveAll(cp =>
                {
                    float segmentEnd = cp.segmentStart + cp.segmentLength;
                    float lengthFromTail = arcLength - segmentEnd;
                    if (lengthFromTail > MaxCurveLength)
                    {
                        return true;
                    }

                    float age = currentTime - cp.timestamp;
                    if (age > MaxRecordingTime)
                    {
                        return true;
                    }

                    return false;
                });
                if (numRemoved > 0)
                {
                    curveDirty = true;
                }

                if (curveDirty)
                {
                    FindMatchingShapes();

                    if (curveRenderer)
                    {
                        curveRenderer.UpdateCurveMesh(Curve);
                    }

                    curveDirty = false;
                }
            }
        }

        protected override void UpdateHandMatch(Handedness handedness, IDictionary<TrackedHandJoint, Pose> joints)
        {
            if (handedness != TrackedHand.ControllerHandedness)
            {
                return;
            }

            if (Curve == null)
            {
                InitCurve(handedness);
            }

            if (joints.TryGetValue(TrackedJoint, out Pose trackedPose))
            {
                ExtendCurve(trackedPose.Position);

                if (follower)
                {
                    follower.transform.position = trackedPose.Position;
                    follower.transform.rotation = trackedPose.Rotation;
                }
            }

            if (InfoText)
            {
                // InfoText.text =
                //     $"Mean Error = {Mathf.Sqrt(MSE):F5}m\n" +
                //     $"Condition = {match.ConditionNumber:F5}\n";
            }
        }

        protected override void ClearHandMatch()
        {
            ClearCurve();
        }

        private void InitCurve(Handedness handedness)
        {
            Curve = new SplineCurve();
            CurveHandedness = handedness;
            lastPosition = null;
            movedDistance = 0.0f;

            if (!follower && FollowerPrefab)
            {
                follower = GameObject.Instantiate(FollowerPrefab);
            }
        }

        private void ClearCurve()
        {
            Curve = null;
            CurveHandedness = Handedness.None;
            lastPosition = null;
            movedDistance = 0.0f;

            if (curveRenderer)
            {
                curveRenderer.ClearCurveMesh();
            }
        }

        private void ExtendCurve(Vector3 point)
        {
            if (lastPosition.HasValue)
            {
                Vector3 delta = point - lastPosition.Value;
                movedDistance += delta.magnitude;
            }
            else
            {
                movedDistance = 0.0f;
            }
            lastPosition = point;

            if (movedDistance >= SamplingDistance)
            {
                float timestamp = Time.time;
                Curve.Append(point, timestamp);
                movedDistance = 0.0f;

                if (Curve.Count > MaxSamples)
                {
                    Curve.RemoveRange(0, Curve.Count - MaxSamples);
                }

                curveDirty = true;
            }
        }

        private readonly ICPSolver icpSolver = new ICPSolver();

        public bool DrawDebugSolverSteps = true;

        private void FindMatchingShapes()
        {
            foreach (ICPShape shape in shapes)
            {
                if (DrawDebugSolverSteps)
                {
                    if (!FindCurveMatchSteps(shape, MeanErrorThreshold, out Pose[] targetOffset, out float[] MSE))
                    {
                        continue;
                    }

                    int numSteps = targetOffset.Length;
                    for (int i = 0; i < numSteps; ++i)
                    {
                        float mix = (float)i / (float)(numSteps - 1);
                        CreateShapeMesh(shape, $"ShapeMatch Step {i}", targetOffset[i], mix);
                    }
                }
                else
                {
                    if (!FindCurveMatch(shape, MeanErrorThreshold, out Pose targetOffset, out float icpError))
                    {
                        continue;
                    }
                    if (!CompareShapeCoverage(shape, MeanErrorThreshold, targetOffset, out float coverageError))
                    {
                        // Debug.Log($"targetOffset.Position=({targetOffset.Position.x:F4}, {targetOffset.Position.y:F4}, {targetOffset.Position.z:F4}) coverageError={Mathf.Sqrt(coverageError)}");
                        continue;
                    }

                    CreateShapeMesh(shape, "ShapeMatch", targetOffset);

                    Curve.Clear();
                }
            }
        }

        private bool FindCurveMatch(ICPShape shape, float ErrorThreshold, out Pose result, out float MSE)
        {
            ICPClosestPointFinder shapePointFinder = shape.CreateClosestPointFinder();
            ICPSampleBuffer curveBuffer = new ICPSampleBuffer();
            Curve.GenerateSamples(ErrorThreshold, curveBuffer);

            icpSolver.Solve(curveBuffer.samples, shapePointFinder, shape.PrincipalComponentsTransform);

            result = icpSolver.TargetOffset;
            MSE = icpSolver.MeanSquareError;
            return icpSolver.HasFoundLocalOptimum && icpSolver.MeanSquareError <= ErrorThreshold * ErrorThreshold;
        }

        private bool FindCurveMatchSteps(ICPShape shape, float ErrorThreshold, out Pose[] result, out float[] MSE)
        {
            ICPClosestPointFinder shapePointFinder = shape.CreateClosestPointFinder();
            ICPSampleBuffer curveBuffer = new ICPSampleBuffer();
            Curve.GenerateSamples(ErrorThreshold, curveBuffer);

            List<Pose> poseList = new List<Pose>();
            List<float> mseList = new List<float>();

            // icpSolver.Solve(points, shape);

            icpSolver.Init(curveBuffer.samples, shapePointFinder, shape.PrincipalComponentsTransform);

            if (curveBuffer.samples.Length > 0)
            {
                poseList.Add(icpSolver.TargetOffset);
                mseList.Add(icpSolver.MeanSquareError);

                while (icpSolver.Iterations < icpSolver.MaxIterations)
                {
                    icpSolver.DebugDrawingEnabled = icpSolver.Iterations == 0;
                    icpSolver.SolveStep();

                    poseList.Add(icpSolver.TargetOffset);
                    mseList.Add(icpSolver.MeanSquareError);

                    // Finish when MSE does not decrease significantly
                    if (icpSolver.HasFoundLocalOptimum)
                    {
                        break;
                    }
                }
            }

            result = poseList.ToArray();
            MSE = mseList.ToArray();
            return icpSolver.HasFoundLocalOptimum && icpSolver.MeanSquareError <= ErrorThreshold * ErrorThreshold;
        }

        // Verify shape coverage by sampling the target shape and computing residual
        // under the tranform provided by ICP.
        // This ensures that the target shape is matched in its entirety rather than just
        // a small section that happens to fit the input curve.
        private bool CompareShapeCoverage(ICPShape shape, float ErrorThreshold, Pose targetOffset, out float MSE)
        {
            ICPClosestPointFinder curvePointFinder = Curve.CreateClosestPointFinder();

            ICPSampleBuffer shapeBuffer = new ICPSampleBuffer();
            shape.GenerateSamples(ErrorThreshold, shapeBuffer);
            // Transform into curve space for computing the MSE
            shapeBuffer.Transform(targetOffset);

            Vector3[] closestCurvePoints = new Vector3[shapeBuffer.samples.Length];
            curvePointFinder.FindClosestPoints(shapeBuffer.samples, closestCurvePoints);

            // for (int i = 0; i < shapeBuffer.samples.Length; ++i)
            // {
            //     Vector3 a = shapeBuffer.samples[i];
            //     Vector3 b = closestCurvePoints[i];
            //     Debug.DrawLine(a, b, Color.magenta);
            // }

            MSE = MathUtils.ComputeMeanSquareError(shapeBuffer.samples, closestCurvePoints);
            return MSE <= ErrorThreshold * ErrorThreshold;
        }

        private GameObject CreateShapeMesh(ICPShape shape, string name, Pose pose, float? colorMix = null)
        {
            if (!ShapeVisualizerPrefab)
            {
                return null;
            }

            GameObject shapeObj = GameObject.Instantiate(ShapeVisualizerPrefab);
            shapeObj.name = name;

            var shapeRenderer = shapeObj.GetComponentInChildren<ICPShapeRenderer>();
            if (shapeRenderer)
            {
                shapeRenderer.UpdateShapeMesh(shape);
                if (colorMix.HasValue)
                {
                    shapeRenderer.SetColorMix(colorMix.Value);
                }
            }

            shapeObj.transform.position = pose.Position;
            shapeObj.transform.rotation = pose.Rotation;

            return shapeObj;
        }
    }
}

