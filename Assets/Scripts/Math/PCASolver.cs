// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Utilities.MathSolvers
{
    public class PCASolver
    {
        public Vector3 CentroidOffset;
        public Quaternion RotationOffset;
        public bool ReflectionCase;
        public float ConditionNumber;

        private Vector3[] inputPoints;

#if false
        public bool Solve(Vector3[] input)
        {
            Init(input);

            int count = input.Length;
            if (count == 0)
            {
                CentroidOffset = Vector3.zero;
                RotationOffset =  Quaternion.identity;
                ConditionNumber = 0.0f;
                ReflectionCase = false;
                return false;
            }

            Vector3 centroid = Vector3.zero;
            foreach (var p in input)
            {
                centroid += p;
            }
            centroid /= count;

            Matrix4x4 I = Matrix4x4.zero;
            foreach (var p in input)
            {
                Vector3 d = line.end - line.start;
                Matrix4x4 diag = MathUtils.ScalarMultiplyMatrix3x3(Matrix4x4.identity, d.sqrMagnitude);
                I = MathUtils.AddMatrix3x3(I, MathUtils.SubMatrix3x3(diag, MathUtils.OuterProduct(d, d)));
            }

            JacobiEigenSolver eigenSolver = new JacobiEigenSolver();
            eigenSolver.Solve(I);

            transform = new Pose(centroid, eigenSolver.Q.normalized);
            moments = eigenSolver.S;

            return true;
        }
#else
        public bool Solve(Vector3[] input)
        {
            Init(input);

            if (inputPoints.Length == 0)
            {
                CentroidOffset = Vector3.zero;
                RotationOffset =  Quaternion.identity;
                ConditionNumber = 0.0f;
                ReflectionCase = false;
                return false;
            }

            if (inputPoints.Length == 1)
            {
                CentroidOffset = inputPoints[0];
                RotationOffset =  Quaternion.identity;
                ConditionNumber = 0.0f;
                ReflectionCase = false;
                return true;
            }

            Vector3 toCentroid = MathUtils.GetCentroid(inputPoints);

            if (inputPoints.Length == 2)
            {
                Vector3 vFrom = new Vector3(1, 0, 0);
                Vector3 vTo = inputPoints[1] - inputPoints[0];

                CentroidOffset = toCentroid;
                RotationOffset = Quaternion.FromToRotation(vFrom, vTo);
                ConditionNumber = 0.0f;
                ReflectionCase = false;
                return true;
            }

            // count >= 3, use Singular Value Decomposition to find least-squares solution based on:
            // “Least-Squares Fitting of Two 3-D Point Sets”, Arun, K. S. and Huang, T. S. and Blostein, S. D,
            // IEEE Transactions on Pattern Analysis and Machine Intelligence, Volume 9 Issue 5, May 1987

            // Build covariance matrix
            Matrix<float> X = CreateMatrix.Dense<float>(inputPoints.Length, 3);
            for (int i = 0; i < inputPoints.Length; ++i)
            {
                X.SetRow(i, GetNVectorFromVector(inputPoints[i] - toCentroid));
            }

            var svdSolver = X.Svd();
            // {
            //     Vector3 x = GetVectorFromNVector(svdSolver.VT.Row(0));
            //     Vector3 y = GetVectorFromNVector(svdSolver.VT.Row(1));
            //     Vector3 z = GetVectorFromNVector(svdSolver.VT.Row(2));
            //     // Vector3 x = GetVectorFromNVector(svdSolver.VT.Column(0));
            //     // Vector3 y = GetVectorFromNVector(svdSolver.VT.Column(1));
            //     // Vector3 z = GetVectorFromNVector(svdSolver.VT.Column(2));
            //     float s = 0.1f;
            //     Vector3 c = toCentroid;
            //     float dur = 5.0f;
            //     Debug.DrawLine(c, c + x * s, Color.red, dur);
            //     Debug.DrawLine(c, c + y * s, Color.green, dur);
            //     Debug.DrawLine(c, c + z * s, Color.blue, dur);
            // }

            Matrix<float> rotationMatrix = svdSolver.VT.Transpose();
            // Debug.Log($"x={svdSolver.W[0, 0]:F4}, y={svdSolver.W[1, 1]:F4}, z={svdSolver.W[2, 2]:F4}");
            // Handle reflection case
            ReflectionCase = (rotationMatrix.Determinant() < 0);
            if (ReflectionCase)
            {
                svdSolver.VT.SetRow(2, -svdSolver.VT.Row(2));
                rotationMatrix = svdSolver.VT.Transpose();
            }

            CentroidOffset = toCentroid;
            RotationOffset = GetQuaternionFromNMatrix(rotationMatrix);
            ConditionNumber = svdSolver.ConditionNumber;
            return true;
        }
#endif

        private void Init(Vector3[] input)
        {
            this.inputPoints = input;
        }

        private static Vector<float> GetNVectorFromVector(Vector3 v)
        {
            Vector<float> result = CreateVector.Dense<float>(3);
            result[0] = v.x;
            result[1] = v.y;
            result[2] = v.z;
            return result;
        }

        private static Vector3 GetVectorFromNVector(Vector<float> nv)
        {
            return new Vector3(nv[0], nv[1], nv[2]);
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