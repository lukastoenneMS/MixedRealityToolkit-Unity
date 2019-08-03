// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.PoseMatching
{
    public class PointSetTransformSolver
    {
        public enum ScaleSolverMode
        {
            Fixed,
            Uniform,
            NonUniform,
        }

        public ScaleSolverMode ScaleMode = ScaleSolverMode.Fixed;

        public Vector3 CentroidOffset;
        public Quaternion RotationOffset;
        public Vector3 Scale;
        public bool ReflectionCase;
        public float ConditionNumber;

        private Vector3[] inputPoints;
        private Vector3[] targetPoints;
        private float[] targetWeights;

        public PointSetTransformSolver(ScaleSolverMode scaleMode)
        {
            this.ScaleMode = scaleMode;
        }

        public bool Solve(Vector3[] input, Vector3[] targets, float[] weights = null)
        {
            Init(input, targets, weights);

            if (inputPoints.Length == 1)
            {
                CentroidOffset = inputPoints[0] - targetPoints[0];
                RotationOffset =  Quaternion.identity;
                Scale = Vector3.one;
                ConditionNumber = 0.0f;
                ReflectionCase = false;
                return true;
            }

            Vector3 fromCentroid = MathUtils.GetCentroid(targetPoints);
            Vector3 toCentroid = MathUtils.GetCentroid(inputPoints);

            if (inputPoints.Length == 2)
            {
                Vector3 vFrom = targetPoints[1] - targetPoints[0];
                Vector3 vTo = inputPoints[1] - inputPoints[0];
                Quaternion rot = Quaternion.FromToRotation(vFrom, vTo);

                CentroidOffset = toCentroid - rot * fromCentroid;
                RotationOffset = rot;
                Scale = Vector3.one * (vFrom.magnitude > 0.0f ? vTo.magnitude / vFrom.magnitude : 1.0f);
                ConditionNumber = 0.0f;
                ReflectionCase = false;
                return true;
            }

            // count >= 3, use Singular Value Decomposition to find least-squares solution based on:
            // “Least-Squares Fitting of Two 3-D Point Sets”, Arun, K. S. and Huang, T. S. and Blostein, S. D,
            // IEEE Transactions on Pattern Analysis and Machine Intelligence, Volume 9 Issue 5, May 1987

            // Solve scale
            switch (ScaleMode)
            {
                case ScaleSolverMode.Fixed:
                    Scale = Vector3.one;
                    break;
                
                case ScaleSolverMode.Uniform:
                    break;

                case ScaleSolverMode.NonUniform:
                    // TODO norm of target points can be computed in advance, target points don't change during solve
                    // Vector3 norm = Vector3.zero;
                    // for (int i = 0; i < targetPoints.Length; ++i)
                    // {
                    //     Vector3 m = targetPoints[i] - fromCentroid;
                    //     norm += new Vector3(m.x * m.x, m.y * m.y, m.z * m.z);
                    // }
                    // Scale = Vector3.zero;
                    // for (int i = 0; i < inputPoints.Length; ++i)
                    // {
                    //     Vector3 p = inputPoints[i] - toCentroid;
                    //     Vector3 m = targetPoints[i] - fromCentroid;
                    //     Scale += new Vector3(m.x * p.x, m.y * p.y, m.z * p.z);
                    // }
                    Vector3 norm = Vector3.zero;
                    for (int i = 0; i < inputPoints.Length; ++i)
                    {
                        Vector3 p = inputPoints[i] - toCentroid;
                        norm += new Vector3(p.x * p.x, p.y * p.y, p.z * p.z);
                    }
                    Scale = Vector3.zero;
                    for (int i = 0; i < targetPoints.Length; ++i)
                    {
                        Vector3 p = inputPoints[i] - toCentroid;
                        Vector3 m = targetPoints[i] - fromCentroid;
                        Scale += new Vector3(m.x * p.x, m.y * p.y, m.z * p.z);
                    }
                    Scale = MathUtils.RScale(Scale, norm);
                    break;
            }
            Scale = Vector3.one;

            // Build covariance matrix
            Matrix<float> H = CreateMatrix.Dense<float>(3, 3, 0.0f);
            for (int i = 0; i < inputPoints.Length; ++i)
            {
                Vector3 m = targetPoints[i] - fromCentroid;
                Vector3 p = inputPoints[i] - toCentroid;
                p.Scale(Scale);

                Vector<float> pa = GetNVectorFromVector(m);
                Vector<float> pb = GetNVectorFromVector(p);

                float weight = targetWeights != null ? targetWeights[i] : 1.0f;
                H += Vector<float>.OuterProduct(pa, pb) * weight;
            }

            var svdSolver = H.Svd();

            Matrix<float> rotationMatrix = (svdSolver.U * svdSolver.VT).Transpose();
            // Handle reflection case
            ReflectionCase = (rotationMatrix.Determinant() < 0);
            if (ReflectionCase)
            {
                svdSolver.VT.SetRow(2, -svdSolver.VT.Row(2));
                rotationMatrix = (svdSolver.U * svdSolver.VT).Transpose();
            }

            Quaternion rotation = GetQuaternionFromNMatrix(rotationMatrix);

            CentroidOffset = toCentroid - rotation * fromCentroid;
            RotationOffset = rotation;
            ConditionNumber = svdSolver.ConditionNumber;
            return true;
        }

        private void Init(Vector3[] input, Vector3[] targets, float[] weights = null)
        {
            this.inputPoints = input;
            this.targetPoints = targets;
            this.targetWeights = weights;
            Debug.Assert(input.Length == targets.Length);
            Debug.Assert(weights == null || input.Length == weights.Length);
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