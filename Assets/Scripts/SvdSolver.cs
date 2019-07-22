// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading.Tasks;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.PoseMatching
{
    public class SvdSolver
    {
        public Quaternion U;
        public Matrix4x4 S;
        public Quaternion V;

        private readonly JacobiEigenSolver eigenSolver = new JacobiEigenSolver();
        private readonly QRSolver qrSolver = new QRSolver();

        public void Solve(Matrix4x4 A)
        {
            eigenSolver.Solve(A.transpose * A);

            V = eigenSolver.Q;
            Matrix4x4 B = A * Matrix4x4.Rotate(V);
            MathUtils.SortColumns(ref B, ref V);

            qrSolver.Solve(B);

            U = qrSolver.Q;

            S = Matrix4x4.identity;
            S.m00 = qrSolver.R.m00;
            S.m11 = qrSolver.R.m11;
            S.m22 = qrSolver.R.m22;
        }
    }
}