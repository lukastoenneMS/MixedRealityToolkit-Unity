// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;
using System;
using System.Collections.Generic;
using UnityEngine;

using Pose = Microsoft.MixedReality.Toolkit.Utilities.MixedRealityPose;

namespace Microsoft.MixedReality.Toolkit.PoseMatching
{
    public class ICPSolver
    {
        public float ErrorConvergenceThreshold = 0.001f;
        public int MaxIterations = 30;

        private ShapeClosestPointFinder targetPointFinder;
        private Vector3[] points;
        public Vector3[] Points => points;

        private Vector3[] closestPoints;
        public Vector3[] ClosestPoints => closestPoints;

        private float meanSquareError = 0.0f;
        public float MeanSquareError => meanSquareError;

        private int iterations = 0;
        public int Iterations => iterations;

        private bool hasFoundLocalOptimum = false;
        public bool HasFoundLocalOptimum => hasFoundLocalOptimum;

        private Pose targetOffset;
        public Pose TargetOffset => targetOffset;

        private Vector3 targetScale;
        public Vector3 TargetScale => targetScale;

        private readonly PointSetTransformSolver pointSetSolver;
        public PointSetTransformSolver PointSetSolver => pointSetSolver;

        private readonly PCASolver pcaSolver;
        public PCASolver PCASolver => pcaSolver;

        public bool DebugDrawingEnabled = false;

        public ICPSolver(PointSetTransformSolver.ScaleSolverMode scaleMode)
        {
            pointSetSolver = new PointSetTransformSolver(scaleMode);
            pcaSolver = new PCASolver();
        }

        public void Solve(Vector3[] points, ShapeClosestPointFinder targetPointFinder, Pose targetPCAPose, Vector3 targetPCAMoments)
        {
            Init(points, targetPointFinder, targetPCAPose, targetPCAMoments);
            if (points.Length > 0)
            {
                while (iterations < MaxIterations)
                {
                    SolveStep();

                    // Finish when MSE does not decrease significantly
                    if (hasFoundLocalOptimum)
                    {
                        break;
                    }
                }
            }
        }

        public void Init(Vector3[] points, ShapeClosestPointFinder targetPointFinder, Pose targetPCAPose, Vector3 targetPCAMoments)
        {
            this.targetPointFinder = targetPointFinder;
            this.points = points;

            closestPoints = new Vector3[points.Length];

            meanSquareError = 0.0f;
            hasFoundLocalOptimum = false;
            iterations = 0;
            InitPose(points, targetPCAPose, targetPCAMoments);
        }

        private void InitPose(Vector3[] points, Pose targetPCAPose, Vector3 targetPCAMoments)
        {
            #if true
            pcaSolver.Solve(points);
            Pose inputPCAPose = new Pose(pcaSolver.CentroidOffset, pcaSolver.RotationOffset);
            #else
            Pose inputPCAPose = new Pose(MathUtils.GetCentroid(points), Quaternion.identity);
            #endif

            Pose offset = targetPCAPose.Inverse().Multiply(inputPCAPose);
            Pose invOffset = offset.Inverse();

            Vector3 moments = Vector3.zero;
            for (int i = 0; i < points.Length; ++i)
            {
                points[i] = invOffset.Multiply(points[i]);
                moments += Vector3.Scale(points[i], points[i]);
            }
            // moments /= Mathf.Max(points.Length, 1);

            targetOffset = offset;
            targetScale = MathUtils.VSqrt(MathUtils.RScale(moments, targetPCAMoments));
            Debug.Log($"SCALE = {targetScale.x:F4}, {targetScale.y:F4}, {targetScale.z:F4}");

            for (int i = 0; i < points.Length; ++i)
            {
                points[i] = MathUtils.RScale(points[i], targetScale);
                {
                    Vector3 p = points[i];
                    float s = 0.0005f;
                    Vector3 dx = new Vector3(1, 0, 0) * s;
                    Vector3 dy = new Vector3(0, 1, 0) * s;
                    Vector3 dz = new Vector3(0, 0, 1) * s;
                    Debug.DrawLine(p - dx, p + dx, Color.cyan, 3.0f);
                    Debug.DrawLine(p - dy, p + dy, Color.cyan, 3.0f);
                    Debug.DrawLine(p - dz, p + dz, Color.cyan, 3.0f);
                }
            }
        }

        public bool SolveStep()
        {
            if (!FindClosestPoints())
            {
                return false;
            }

            pointSetSolver.Solve(points, closestPoints);

            Pose offset = new Pose(pointSetSolver.CentroidOffset, pointSetSolver.RotationOffset);
            Pose invOffset = offset.Inverse();
            // Vector3 invScale = MathUtils.RScale(Vector3.one, pointSetSolver.Scale);
            Vector3 invScale = Vector3.one;

            for (int i = 0; i < points.Length; ++i)
            {
                points[i] = invOffset.Multiply(Vector3.Scale(points[i], invScale));
            }
            targetOffset = targetOffset.Multiply(offset);
            targetScale.Scale(pointSetSolver.Scale);

            float prevMeanSquareError = meanSquareError;
            meanSquareError = MathUtils.ComputeMeanSquareError(points, closestPoints);

            float sqrTau = ErrorConvergenceThreshold * ErrorConvergenceThreshold;
            hasFoundLocalOptimum = iterations > 1 && prevMeanSquareError - meanSquareError <= sqrTau;

            ++iterations;
            return true;
        }

        private bool FindClosestPoints()
        {
            targetPointFinder.FindClosestPoints(points, closestPoints);
            if (DebugDrawingEnabled)
            {
                for (int i = 0; i < points.Length; ++i)
                {
                    if (i % 5 != 0)
                    {
                        continue;
                    }
                    // Vector3 a = targetOffset.Multiply(points[i]);
                    // Vector3 b = targetOffset.Multiply(closestPoints[i]);
                    Vector3 a = points[i];
                    Vector3 b = closestPoints[i];
                    Debug.DrawLine(a, b, Color.white, 3.0f);
                }
            }
            return true;
        }
    }
}