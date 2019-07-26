// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.PoseMatching
{
    public class PCASolver
    {
        public Vector3 CentroidOffset;
        public Quaternion RotationOffset;
        public bool ReflectionCase;
        public float ConditionNumber;

        private Vector3[] inputPoints;

        public bool Solve(Vector3[] input)
        {
            Init(input);

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
                X.SetRow(i, GetNVectorFromVector(inputPoints[i]));
            }

            var svdSolver = X.Svd();
            // {
            //     Vector3 x = GetVectorFromNVector(svdSolver.VT.Row(0));
            //     Vector3 y = GetVectorFromNVector(svdSolver.VT.Row(1));
            //     Vector3 z = GetVectorFromNVector(svdSolver.VT.Row(2));
            //     Debug.Log($"X = {x}, {x.magnitude}");
            //     Debug.DrawLine(toCentroid, toCentroid + 0.2f * x, Color.red);
            //     Debug.DrawLine(toCentroid, toCentroid + 0.2f * y, Color.green);
            //     Debug.DrawLine(toCentroid, toCentroid + 0.2f * z, Color.blue);
            // }

            Matrix<float> rotationMatrix = svdSolver.VT.Transpose();
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