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

        public void Solve(Matrix4x4 A)
        {
            eigenSolver.Solve(A);

            MathUtils.SortColumns(ref S, ref V);


        }
    }
}