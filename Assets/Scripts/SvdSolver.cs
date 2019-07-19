// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading.Tasks;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.PoseMatching
{
    public class JacobiEigenSolver
    {
        public Matrix4x4 S { get; private set; }
        public Matrix4x4 Q { get; private set; }

        public int iterations { get; private set; }
        private int axisPair;

        public float residual { get; private set; }

        public float squaredErrorThreshold { get; set; } = 0.005f;
        public int maxIterations { get; set; } = 30;

        public void Init(Matrix4x4 A)
        {
            this.S = A.transpose * A;
            Q = Matrix4x4.identity;

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
            float c, s;
            Matrix4x4 Qk = Matrix4x4.identity;
            switch (axisPair)
            {
                case 0:
                    MathUtils.ApproximateGivensRotationCosSin(S.m00, S.m01, S.m11, out c, out s);
                    Qk.m00 = c;
                    Qk.m10 = s;
                    Qk.m01 = -s;
                    Qk.m11 = c;
                    break;
                case 1:
                    MathUtils.ApproximateGivensRotationCosSin(S.m00, S.m02, S.m22, out c, out s);
                    Qk.m00 = c;
                    Qk.m20 = s;
                    Qk.m02 = -s;
                    Qk.m22 = c;
                    break;
                case 2:
                    MathUtils.ApproximateGivensRotationCosSin(S.m11, S.m12, S.m22, out c, out s);
                    Qk.m11 = c;
                    Qk.m21 = s;
                    Qk.m12 = -s;
                    Qk.m22 = c;
                    break;
            }

            Q = Q * Qk;
            S = Qk.transpose * S * Qk;

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

    public class SvdSolver
    {
    }
}