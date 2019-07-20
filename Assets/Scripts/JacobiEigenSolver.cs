// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading.Tasks;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.PoseMatching
{
    /// <summary>
    /// Eigenvalue decomposition solver using the Jacobi method.
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
        public Matrix4x4 S { get; private set; }
        /// <remarks>
        /// The paper encourages the use of quaternion-based Givens rotations for efficiency.
        /// Since we are using Unity matrix and quaternion types we have to convert to matrix anyway,
        /// so the performance gain is not very significant.
        /// This will be more important for a C++ implementation with efficient math types.
        /// </remarks>
        public Quaternion Q { get; private set; }

        public int iterations { get; private set; }
        private int axisPair;

        public float residual { get; private set; }

        public float squaredErrorThreshold { get; set; } = 1.0e-6f;
        public int maxIterations { get; set; } = 30;

        public void Init(Matrix4x4 A)
        {
            this.S = A.transpose * A;
            Q = Quaternion.identity;

            residual = ComputeResidual(A);

            iterations = 0;
            axisPair = 0;
        }

        public void Solve()
        {
            while (iterations < maxIterations)
            {
                if (residual <= squaredErrorThreshold)
                {
                    return;
                }

                SolveStep();
            }
        }

        public void SolveStep()
        {
            Quaternion Qk = Quaternion.identity;
            switch (axisPair)
            {
                case 0:
                    MathUtils.ApproximateGivensRotationQuaternion(S.m00, S.m01, S.m11, out Qk.z, out Qk.w);
                    break;
                case 1:
                    MathUtils.ApproximateGivensRotationQuaternion(S.m22, S.m20, S.m00, out Qk.y, out Qk.w);
                    break;
                case 2:
                    MathUtils.ApproximateGivensRotationQuaternion(S.m11, S.m12, S.m22, out Qk.x, out Qk.w);
                    break;
            }

            Q = Q * Qk;
            Matrix4x4 MQk = Matrix4x4.Rotate(Qk);
            S = MQk.transpose * S * MQk;

            //string str = $"[{S.m00:F3} {S.m01:F3} {S.m02:F3}]\n[{S.m10:F3} {S.m11:F3} {S.m12:F3}]\n[{S.m20:F3} {S.m21:F3} {S.m22:F3}]";
            //Debug.Log(str);

            residual = ComputeResidual(S);

            ++axisPair;
            if (axisPair == 3)
            {
                axisPair = 0;
            }

            ++iterations;
        }

        public static float ComputeResidual(Matrix4x4 m)
        {
            return m.m10*m.m10 + m.m01*m.m01 + m.m20*m.m20 + m.m02*m.m02 + m.m21*m.m21 + m.m12*m.m12;
        }
    }
}