// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
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

        private const float oneOverSqrtTwo = 0.70710678118654752440084436210485f;

        /// <summary>
        /// Compute an approximate Givens rotation on a 2x2 symmetric matrix A.
        /// </summary>
        /// <returns>The approximated Givens rotation as cosine and sine of the Givens angle</returns>
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
        public static void ApproximateGivensRotationCosSin(float a11, float a12, float a22, out float c, out float s)
        {
            float p = a12;
            float pp = p * p;
            float q = a11 - a22;
            float qq = q * q;
            bool b = pp < qq;
            float omega = OneOverSqrt(pp + qq);
            s = b ? omega * p : oneOverSqrtTwo;
            c = b ? omega * q : oneOverSqrtTwo;
        }

        // 3 + 2*sqrt(2)
        private const float GivensApproxGamma = 5.8284271247461900976033774484194f;
        // cos(pi/8)
        private const float GivensApproxCosPi8 = 0.99997651217454865478849954406816f;
        // sin(pi/8)
        private const float GivensApproxSinPi8 = 0.00685383828411102723918684180104f;

        /// <summary>
        /// Compute an approximate Givens rotation on a 2x2 symmetric matrix A.
        /// </summary>
        /// <returns>The approximated Givens rotation as a quaternion</returns>
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
        public static void ApproximateGivensRotationQuaternion(float a11, float a12, float a22, out float x, out float w)
        {
            float ch = 2.0f * (a11 - a22);
            float cch = ch * ch;
            float sh = a12;
            float ssh = sh * sh;
            bool b = GivensApproxGamma * ssh < cch;
            float omega = OneOverSqrt(cch + ssh);
            x = b ? omega * ch : GivensApproxCosPi8;
            w = b ? omega * sh : GivensApproxSinPi8;
        }
    }
}