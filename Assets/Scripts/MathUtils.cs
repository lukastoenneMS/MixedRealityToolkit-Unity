// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Runtime.InteropServices;

namespace Microsoft.MixedReality.Toolkit.PoseMatching
{
    // Based on
    // http://blog.wouldbetheologian.com/2011/11/fast-approximate-sqrt-method-in-c.html
    public static class MathUtils
    {
        public static float OneOverSqrt(float z)
        {
            FloatIntUnion u;
            u.tmp = 0;
            float xhalf = 0.5f * z;
            u.f = z;
            u.tmp = 0x5f375a86 - (u.tmp >> 1);
            u.f = u.f * (1.5f - xhalf * u.f * u.f);
            return u.f;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct FloatIntUnion
        {
            [FieldOffset(0)]
            public float f;

            [FieldOffset(0)]
            public int tmp;
        }

        private const float sqrtOneHalf = 0.70710678118654752440084436210485f;

        /// <summary>
        /// Compute an approximate Givens rotation on a 2x2 symmetric matrix A.
        /// </summary>
        /// <remarks>
        /// Let `A = [[a11, a12], [a12, a22]]` be a symmetric 2x2 matrix.
        /// Then the Givens rotation diagonalizes A by finding a rotation matrix `Q = [[c, -s], [s, c]]`
        /// such that `B = Q^T * A * Q` is a diagonal matrix.
        ///
        /// This method computes approximate cosine and sine factors, based on:
        ///
        /// McAdams, Aleka, et al.
        /// Computing the singular value decomposition of 3x3 matrices with minimal branching
        /// and elementary floating point operations.
        /// University of Wisconsin-Madison Department of Computer Sciences, 2011.
        /// </remarks>
        public static void ApproximateGivensRotation(float a11, float a12, float a22, out float c, out float s)
        {
            float p = a12;
            float pp = p * p;
            float q = a11 - a22;
            float qq = q * q;
            bool b = pp < qq;
            float w = OneOverSqrt(pp + qq);
            s = b ? w * p : sqrtOneHalf;
            c = b ? w * q : sqrtOneHalf;
        }
    }
}