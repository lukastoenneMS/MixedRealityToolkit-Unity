// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;
using System;
using UnityEngine;

using Pose = Microsoft.MixedReality.Toolkit.Utilities.MixedRealityPose;

namespace Microsoft.MixedReality.Toolkit.PoseMatching
{
    public class PoseEvaluator
    {
        public PoseMatch EvaluatePose(Vector3[] input, PoseConfiguration config)
        {
            if (input.Length != config.Length)
            {
                throw new ArgumentException($"Input size {input.Length} does not match configuration size {config.Length}");
            }

            FindMinErrorTransform(input, config, out PoseMatch result);

            return result;
        }

        public void ComputeResiduals(Vector3[] input, PoseConfiguration config, PoseMatch match, out float[] residuals, out float MSE)
        {
            residuals = new float[config.Length];

            float sse = 0.0f;
            for (int i = 0; i < config.Length; ++i)
            {
                Vector3 p = match.Offset.Multiply(config.Targets[i]);
                residuals[i] = (p - input[i]).sqrMagnitude;
                sse += residuals[i];
            }

            MSE = sse / Mathf.Max(1, config.Length);
        }

        private bool FindMinErrorTransform(Vector3[] input, PoseConfiguration config, out PoseMatch match)
        {
            Pose pose;

            if (config.Length == 1)
            {
                pose = new Pose(input[0] - config.Targets[0], Quaternion.identity);
                match = new PoseMatch(pose, 0.0f);
                return true;
            }

            Vector3 fromCentroid = GetCentroid(config.Targets);
            Vector3 toCentroid = GetCentroid(input);

            if (config.Length == 2)
            {
                Vector3 vFrom = config.Targets[1] - config.Targets[0];
                Vector3 vTo = input[1] - input[0];
                Quaternion rot = Quaternion.FromToRotation(vFrom, vTo);
                pose = new Pose(toCentroid - rot * fromCentroid, rot);
                match = new PoseMatch(pose, 0.0f);
                return true;
            }

            // count >= 3, use Singular Value Decomposition to find least-squares solution

            // Build covariance matrix
            Matrix<float> H = CreateMatrix.Dense<float>(3, 3, 0.0f);
            for (int i = 0; i < config.Length; ++i)
            {
                Vector<float> pa = GetNVectorFromVector(config.Targets[i] - fromCentroid);
                Vector<float> pb = GetNVectorFromVector(input[i] - toCentroid);

                H += Vector<float>.OuterProduct(pa, pb);
            }

            var svdSolver = H.Svd();

            Matrix<float> rotationMatrix = (svdSolver.U * svdSolver.VT).Transpose();
            // Handle reflection case
            if (rotationMatrix.Determinant() < 0)
            {
                rotationMatrix.SetColumn(0, -rotationMatrix.Column(0));
                rotationMatrix.SetColumn(1, -rotationMatrix.Column(1));
                rotationMatrix.SetColumn(2, -rotationMatrix.Column(2));
            }

            Quaternion rotation = GetQuaternionFromNMatrix(rotationMatrix);

            pose = new Pose(toCentroid - rotation * fromCentroid, rotation);
            match = new PoseMatch(pose, svdSolver.ConditionNumber);
            return true;
        }

        private float GetMean(float[] values)
        {
            float sum = 0.0f;
            foreach (var v in values)
            {
                sum += v;
            }
            return sum / Mathf.Max(1, values.Length);
        }

        private Vector3 GetCentroid(Vector3[] points)
        {
            Vector3 sum = Vector3.zero;
            foreach (Vector3 p in points)
            {
                sum += p;
            }
            return sum / Mathf.Max(1, points.Length);
        }

        private static Vector<float> GetNVectorFromVector(Vector3 v)
        {
            Vector<float> result = CreateVector.Dense<float>(3);
            result[0] = v.x;
            result[1] = v.y;
            result[2] = v.z;
            return result;
        }

        private static Vector<float> GetNVectorFromQuaternion(Quaternion q)
        {
            Vector<float> result = CreateVector.Dense<float>(3);
            result[0] = q.x;
            result[1] = q.y;
            result[2] = q.z;
            return result;
        }

        private static Quaternion GetQuaternionFromNMatrix(Matrix<float> m)
        {
            // Adapted from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
            Quaternion q = new Quaternion();
            q.w = Mathf.Sqrt(Mathf.Max(0, 1 + m[0,0] + m[1,1] + m[2,2])) / 2; 
            q.x = Mathf.Sqrt(Mathf.Max(0, 1 + m[0,0] - m[1,1] - m[2,2])) / 2; 
            q.y = Mathf.Sqrt(Mathf.Max(0, 1 - m[0,0] + m[1,1] - m[2,2])) / 2; 
            q.z = Mathf.Sqrt(Mathf.Max(0, 1 - m[0,0] - m[1,1] + m[2,2])) / 2; 
            q.x *= Mathf.Sign(q.x * (m[2,1] - m[1,2]));
            q.y *= Mathf.Sign(q.y * (m[0,2] - m[2,0]));
            q.z *= Mathf.Sign(q.z * (m[1,0] - m[0,1]));
            return q;
        }
    }
}