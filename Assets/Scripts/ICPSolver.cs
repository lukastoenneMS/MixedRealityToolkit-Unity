// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;
using System;
using UnityEngine;

using Pose = Microsoft.MixedReality.Toolkit.Utilities.MixedRealityPose;

namespace Microsoft.MixedReality.Toolkit.PoseMatching
{
    public interface ICPShape
    {
        void FindClosestPoints(Vector3[] points, Vector3[] result);
    }

    public class ICPSolver
    {
        public float ErrorConvergenceThreshold = 0.001f;
        public float MaxIterations = 30;

        private ICPShape targetShape;
        private Vector3[] points;
        public Vector3[] Points => points;

        private Vector3[] closestPoints;
        public Vector3[] ClosestPoints => closestPoints;

        private float meanSquareError = 0.0f;
        public float MeanSquareError => meanSquareError;

        private int iterations = 0;
        public int Iterations => iterations;

        private Pose targetOffset;
        public Pose TargetOffset => targetOffset;

        private readonly PointSetTransformSolver pointSetSolver = new PointSetTransformSolver();
        public PointSetTransformSolver PointSetSolver => pointSetSolver;

        public void Solve(Vector3[] points, ICPShape targetShape)
        {
            Init(points, targetShape);

            float sqrTau = ErrorConvergenceThreshold * ErrorConvergenceThreshold;
            while (iterations < MaxIterations)
            {
                float prevMeanSquareError = meanSquareError;

                SolveStep();

                // Finish when MSE does not decrease significantly
                if (iterations > 1 && prevMeanSquareError - meanSquareError <= sqrTau)
                {
                    break;
                }
            }
        }

        public void Init(Vector3[] points, ICPShape targetShape)
        {
            this.targetShape = targetShape;
            this.points = points;

            closestPoints = new Vector3[points.Length];

            meanSquareError = 0.0f;
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

            meanSquareError = MathUtils.ComputeMeanError(points, closestPoints);

            ++iterations;
            return true;
        }

        private bool FindClosestPoints()
        {
            targetShape.FindClosestPoints(points, closestPoints);
            return true;
        }
    }
}