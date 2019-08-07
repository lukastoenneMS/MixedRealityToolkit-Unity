// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Utilities.MathSolvers
{
    public class PCASolver
    {
        public Vector3 CentroidOffset;
        public Quaternion RotationOffset;
        public Vector3 Scale;
        public bool ReflectionCase;
        public float ConditionNumber;

        private Vector3[] inputPoints;

        public bool Solve(Vector3[] input)
        {
            Init(input);

            int count = input.Length;
            if (count == 0)
            {
                CentroidOffset = Vector3.zero;
                RotationOffset =  Quaternion.identity;
                Scale = Vector3.one;
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
                Vector3 d = p - centroid;
                Matrix4x4 Ipoint = MathUtils.OuterProduct(d, d);
                I = MathUtils.AddMatrix3x3(I, Ipoint);
            }
            I = MathUtils.ScalarMultiplyMatrix3x3(I, 1.0f / count);

            JacobiEigenSolver eigenSolver = new JacobiEigenSolver();
            eigenSolver.Solve(I);
            eigenSolver.SortEigenValues();

            CentroidOffset = centroid;
            RotationOffset =  eigenSolver.Q;
            Scale = MathUtils.VSqrt(eigenSolver.S);
            ConditionNumber = 0.0f;
            ReflectionCase = false;

            return true;
        }

        private void Init(Vector3[] input)
        {
            this.inputPoints = input;
        }
    }
}