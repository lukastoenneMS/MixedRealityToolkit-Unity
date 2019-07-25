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
    public interface ICPShape
    {
        ICPClosestPointFinder CreateClosestPointFinder();

        void GenerateSamples(float maxSampleDistance, ICPSampleBuffer buffer);
    }

    public interface ICPClosestPointFinder
    {
        void Reserve(int numPoints);

        void FindClosestPoints(Vector3[] points, Vector3[] result);
    }

    public class ICPSampleBuffer
    {
        public Vector3[] samples = new Vector3[0];

        public void Transform(Pose offset)
        {
            for (int i = 0; i < samples.Length; ++i)
            {
                samples[i] = offset.Multiply(samples[i]);
            }
        }
    }

    public class ICPSolver
    {
        public float ErrorConvergenceThreshold = 0.001f;
        public int MaxIterations = 30;

        private ICPClosestPointFinder targetPointFinder;
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

        private readonly PointSetTransformSolver pointSetSolver = new PointSetTransformSolver();
        public PointSetTransformSolver PointSetSolver => pointSetSolver;

        public void Solve(Vector3[] points, ICPClosestPointFinder targetPointFinder)
        {
            Init(points, targetPointFinder);
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

        public void Init(Vector3[] points, ICPClosestPointFinder targetPointFinder)
        {
            this.targetPointFinder = targetPointFinder;
            this.points = points;

            closestPoints = new Vector3[points.Length];

            meanSquareError = 0.0f;
            hasFoundLocalOptimum = false;
            iterations = 0;
            targetOffset = Pose.ZeroIdentity;
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
            for (int i = 0; i < points.Length; ++i)
            {
                points[i] = invOffset.Multiply(points[i]);
            }
            targetOffset = targetOffset.Multiply(offset);

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
            return true;
        }
    }
}