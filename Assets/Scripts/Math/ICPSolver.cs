// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;
using Microsoft.MixedReality.Toolkit.Utilities.ShapeMatching;
using System;
using System.Collections.Generic;
using UnityEngine;

using Pose = Microsoft.MixedReality.Toolkit.Utilities.MixedRealityPose;

namespace Microsoft.MixedReality.Toolkit.Utilities.MathSolvers
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

        private const float debugDrawTime = 3.0f;

        private void InitPose(Vector3[] points, Pose targetPCAPose, Vector3 targetPCAMoments)
        {
            pcaSolver.Solve(points);
            Pose inputPCAPose = new Pose(pcaSolver.CentroidOffset, pcaSolver.RotationOffset);

            Pose offset = inputPCAPose.Multiply(targetPCAPose.Inverse());
            // Pose invOffset = targetPCAPose.Multiply(inputPCAPose.Inverse());
            Vector3 scale = MathUtils.RScale(pcaSolver.Scale, targetPCAMoments);
            Vector3 invScale = MathUtils.RScale(targetPCAMoments, pcaSolver.Scale);

            {
                Vector3 c = targetPCAPose.Position;
                float s = 0.3f;
                Vector3 x = c + targetPCAPose.Rotation * new Vector3(1, 0, 0) * s * targetPCAMoments.x;
                Vector3 y = c + targetPCAPose.Rotation * new Vector3(0, 1, 0) * s * targetPCAMoments.y;
                Vector3 z = c + targetPCAPose.Rotation * new Vector3(0, 0, 1) * s * targetPCAMoments.z;

                Debug.DrawLine(c, x, Color.red, debugDrawTime);
                Debug.DrawLine(c, y, Color.green, debugDrawTime);
                Debug.DrawLine(c, z, Color.blue, debugDrawTime);
            }
            {
                Pose tfm = targetPCAPose.Multiply(inputPCAPose.Inverse());
                // Pose tfm = Pose.ZeroIdentity;
                Vector3 c = tfm.Multiply(pcaSolver.CentroidOffset);
                float s = 0.3f;
                Vector3 x = c + tfm.Rotation * pcaSolver.RotationOffset * new Vector3(1, 0, 0) * s * pcaSolver.Scale.x;
                Vector3 y = c + tfm.Rotation * pcaSolver.RotationOffset * new Vector3(0, 1, 0) * s * pcaSolver.Scale.y;
                Vector3 z = c + tfm.Rotation * pcaSolver.RotationOffset * new Vector3(0, 0, 1) * s * pcaSolver.Scale.z;

                Debug.DrawLine(c, x, Color.magenta, debugDrawTime);
                Debug.DrawLine(c, y, Color.yellow, debugDrawTime);
                Debug.DrawLine(c, z, Color.cyan, debugDrawTime);
            }

            for (int i = 0; i < points.Length; ++i)
            {
                points[i] = inputPCAPose.Inverse().Multiply(points[i]);
                points[i].Scale(scale);
                points[i] = targetPCAPose.Multiply(points[i]);
            }

            targetOffset = offset;
            targetScale = invScale;
            // targetScale = Vector3.one;
            // targetScale = MathUtils.VSqrt(MathUtils.RScale(moments, targetPCAMoments));
            // Debug.Log($"SCALE = {targetScale.x:F4}, {targetScale.y:F4}, {targetScale.z:F4}");

            // for (int i = 0; i < points.Length; ++i)
            // {
            //     Vector3 p = points[i];
            //     float s = 0.0005f;
            //     Vector3 dx = new Vector3(1, 0, 0) * s;
            //     Vector3 dy = new Vector3(0, 1, 0) * s;
            //     Vector3 dz = new Vector3(0, 0, 1) * s;
            //     Debug.DrawLine(p - dx, p + dx, Color.cyan, debugDrawTime);
            //     Debug.DrawLine(p - dy, p + dy, Color.cyan, debugDrawTime);
            //     Debug.DrawLine(p - dz, p + dz, Color.cyan, debugDrawTime);
            // }
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
            // if (DebugDrawingEnabled)
            // {
            //     for (int i = 0; i < points.Length; ++i)
            //     {
            //         if (i % 20 != 0)
            //         {
            //             continue;
            //         }
            //         Vector3 a = points[i];
            //         Vector3 b = closestPoints[i];
            //         Debug.DrawLine(a, b, Color.white, debugDrawTime);
            //     }
            // }
            return true;
        }
    }
}