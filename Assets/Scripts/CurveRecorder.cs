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

        public TrackedHandJoint TrackedJoint = TrackedHandJoint.IndexTip;
        public float SamplingDistance = 0.03f;
        public float MaxCurveLength = 3.0f;
        public int MaxSamples = 200;

        public int RenderResolution = 6;
        public float RenderThickness = 0.003f;
        public Material ShapeMaterial;

        public SplineCurve Curve { get; private set; }
        public Handedness CurveHandedness { get; private set; }
        private Vector3? lastPosition;
        private float movedDistance;

        public readonly ICPShape[] shapes = new ICPShape[]
        {
            LineShapeUtils.CreateCircle(0.2f, 16),
        };

        public float MeanErrorThreshold = 0.05f;

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
                ExtendCurve(trackedPose.Position);

                FindMatchingShapes();
            }

            if (InfoText)
            {
                // InfoText.text =
                //     $"Mean Error = {Mathf.Sqrt(MSE):F5}m\n" +
                //     $"Condition = {match.ConditionNumber:F5}\n";
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
                Curve.Append(point);
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

                if (meshFilter)
                {
                    if (meshFilter.sharedMesh == null)
                    {
                        meshFilter.mesh = new Mesh();
                    }
                    CurveMeshUtils.GenerateCurveMesh(meshFilter.sharedMesh, Curve, RenderResolution, RenderThickness);
                }
            }
        }

        private readonly ICPSolver icpSolver = new ICPSolver();

        private void FindMatchingShapes()
        {
            foreach (ICPShape shape in shapes)
            {
#if false
                FindCurveMatchSteps(shape, MeanErrorThreshold, out Pose[] targetOffset, out float[] MSE);
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
                if (FindCurveMatch(shape, MeanErrorThreshold, out Pose targetOffset))
                {
                    // ShapeObject.SetActive(true);
                    // ShapeObject.transform.position = targetOffset.Multiply(transform.position);
                    // ShapeObject.transform.rotation = targetOffset.Multiply(transform.rotation);

                    var lineShape = shape as LineShape;
                    if (lineShape != null)
                    {
                        GameObject shapeObj = new GameObject();
                        shapeObj.name = $"ShapeMatch_{icpSolver.MeanSquareError}";

                        var shapeMeshFilter = shapeObj.AddComponent<MeshFilter>();
                        shapeMeshFilter.mesh = new Mesh();
                        CurveMeshUtils.GenerateLineShapeMesh(shapeMeshFilter.sharedMesh, lineShape, RenderResolution, RenderThickness);

                        var shapeRenderer = shapeObj.AddComponent<MeshRenderer>();
                        shapeRenderer.sharedMaterial = ShapeMaterial;

                        shapeObj.transform.position = targetOffset.Position;
                        shapeObj.transform.rotation = targetOffset.Rotation;
                    }

                    Curve.Clear();
                }
                else
                {
                    // ShapeObject.SetActive(false);
                }
#endif
            }
        }

        private bool FindCurveMatch(ICPShape shape, float ErrorThreshold, out Pose result)
        {
            if (Curve.Count < shape.MinimumPointCount)
            {
                result = Pose.ZeroIdentity;
                return false;
            }

            Vector3[] points = Curve.ControlPoints.Select(cp => cp.position).ToArray();
            var targetPointFinder = shape.CreateClosestPointFinder();

            icpSolver.Solve(points, targetPointFinder);

            result = icpSolver.TargetOffset;
            return icpSolver.HasFoundLocalOptimum && icpSolver.MeanSquareError <= ErrorThreshold * ErrorThreshold;
        }

        private bool FindCurveMatchSteps(ICPShape shape, float ErrorThreshold, out Pose[] result, out float[] MSE)
        {
            if (Curve.Count < shape.MinimumPointCount)
            {
                result = null;
                MSE = null;
                return false;
            }

            Vector3[] points = Curve.ControlPoints.Select(cp => cp.position).ToArray();
            var targetPointFinder = shape.CreateClosestPointFinder();

            result = new Pose[icpSolver.MaxIterations];
            MSE = new float[icpSolver.MaxIterations];

            // icpSolver.Solve(points, shape);

            icpSolver.Init(points, targetPointFinder);

            int i = 0;
            while (icpSolver.Iterations < icpSolver.MaxIterations)
            {
                icpSolver.SolveStep();

                result[i] = icpSolver.TargetOffset;
                MSE[i] = icpSolver.MeanSquareError;
                ++i;

                // Finish when MSE does not decrease significantly
                if (icpSolver.HasFoundLocalOptimum)
                {
                    break;
                }
            }

            return icpSolver.HasFoundLocalOptimum && icpSolver.MeanSquareError <= ErrorThreshold * ErrorThreshold;
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
