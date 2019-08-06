// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading.Tasks;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Utilities.MathSolvers
{
    /// <summary>
    /// Eigenvalue decomposition of a symmetric matrix using the Jacobi method.
    /// </summary>
    /// <remarks>
    /// Based on:
    ///
    /// McAdams, Aleka, et al.
    /// Computing the singular value decomposition of 3x3 matrices with minimal branching
    /// and elementary floating point operations.
    /// University of Wisconsin-Madison Department of Computer Sciences, 2011.
    /// </remarks>
    public class JacobiEigenSolver
    {
        public Matrix4x4 W = Matrix4x4.identity;
        public Vector3 S => new Vector3(W.m00, W.m11, W.m22);
        /// <remarks>
        /// The paper encourages the use of quaternion-based Givens rotations for efficiency.
        /// Since we are using Unity matrix and quaternion types we have to convert to matrix anyway,
        /// so the performance gain is likely not very significant.
        /// This will be more important for a C++ implementation with efficient math types.
        /// </remarks>
        public Quaternion Q = Quaternion.identity;

        public int iterations = 0;
        private int axisPair = 0;

        public float residual = 0.0f;

        public float squaredErrorThreshold = 1.0e-6f;
        public int maxIterations = 30;

        public void Solve(Matrix4x4 A)
        {
            Init(A);

            while (iterations < maxIterations)
            {
                if (residual <= squaredErrorThreshold)
                {
                    break;
                }

                SolveStep();
            }

            Q.Normalize();
        }

        public void Init(Matrix4x4 A)
        {
            W = A;
            Q = Quaternion.identity;

            residual = ComputeResidual(W);

            iterations = 0;
            axisPair = 0;
        }

        public void SolveStep()
        {
            Quaternion Qk = Quaternion.identity;
            switch (axisPair)
            {
                case 0:
                    MathUtils.ApproximateGivensRotationQuaternion(W.m00, W.m10, W.m11, out Qk.z, out Qk.w);
                    axisPair = 1;
                    break;
                case 1:
                    MathUtils.ApproximateGivensRotationQuaternion(W.m22, W.m02, W.m00, out Qk.y, out Qk.w);
                    axisPair = 2;
                    break;
                case 2:
                    MathUtils.ApproximateGivensRotationQuaternion(W.m11, W.m21, W.m22, out Qk.x, out Qk.w);
                    axisPair = 0;
                    break;
            }

            Q = Q * Qk;
            Matrix4x4 MQk = Matrix4x4.Rotate(Qk);
            W = MQk.transpose * W * MQk;

            // XXX residual could be computed cheaply by subtracting s^2 from current residual instead (see Section 2.1 of the paper)
            residual = ComputeResidual(W);
            ++iterations;
        }

        public static float ComputeResidual(Matrix4x4 m)
        {
            return m.m10*m.m10 + m.m01*m.m01 + m.m20*m.m20 + m.m02*m.m02 + m.m21*m.m21 + m.m12*m.m12;
        }
    }
}