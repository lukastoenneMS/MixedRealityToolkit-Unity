// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Runtime.InteropServices;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.PoseMatching
{
    public static class MathUtils
    {
        // Based on
        // http://blog.wouldbetheologian.com/2011/11/fast-approximate-sqrt-method-in-c.html
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
        private const float GivensApproxCosPi8 = 0.92387953251128675612818318939679f;
        // sin(pi/8)
        private const float GivensApproxSinPi8 = 0.3826834323650897717284599840304f;

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
        public static void ApproximateGivensRotationQuaternion(float a11, float a12, float a22, out float r, out float w)
        {
            float ch = 2.0f * (a11 - a22);
            float cch = ch * ch;
            float sh = a12; 
            float ssh = sh * sh;
            bool b = (GivensApproxGamma * ssh) < cch;
            float omega = OneOverSqrt(cch + ssh);
            w = b ? omega * ch : GivensApproxCosPi8;
            r = b ? omega * sh : GivensApproxSinPi8;
        }

        private static void CondSwap(bool c, ref float x, ref float y)
        {
            float t = x;
            x = c ? y : x;
            y = c ? t : y;
        }

        private static void CondNegSwap(bool c, ref Vector3 x, ref Vector3 y)
        {
            Vector3 t = -x;
            x = c ? y : x;
            y = c ? t : y;
        }

        /// <summary>
        /// Sorts columns in both B and V based on magnitude of columns in B in descending order.
        /// </summary>
        public static void SortColumns(ref Matrix4x4 B, ref Matrix4x4 V)
        {
            Vector3 b1 = B.GetColumn(0);
            Vector3 b2 = B.GetColumn(1);
            Vector3 b3 = B.GetColumn(2);
            Vector3 v1 = V.GetColumn(0);
            Vector3 v2 = V.GetColumn(1);
            Vector3 v3 = V.GetColumn(2);
            float p1 = b1.sqrMagnitude;
            float p2 = b2.sqrMagnitude;
            float p3 = b3.sqrMagnitude;
            bool c;
            
            c = p1 < p2;
            CondNegSwap(c, ref b1, ref b2);
            CondNegSwap(c, ref v1, ref v2);
            CondSwap(c, ref p1, ref p2);

            c = p1 < p3;
            CondNegSwap(c, ref b1, ref b3);
            CondNegSwap(c, ref v1, ref v3);
            CondSwap(c, ref p1, ref p3);

            c = p2 < p3;
            CondNegSwap(c, ref b2, ref b3);
            CondNegSwap(c, ref v2, ref v3);
            CondSwap(c, ref p2, ref p3);

            B.SetColumn(0, b1);
            B.SetColumn(1, b2);
            B.SetColumn(2, b3);
            V.SetColumn(0, v1);
            V.SetColumn(1, v2);
            V.SetColumn(2, v3);
        }

        /// <summary>
        /// Sorts columns in both B and V based on magnitude of columns in B in descending order.
        /// </summary>
        /// <remarks>
        /// Quaternion variant that applies matrix-equivalent operation on V.
        /// </remarks>
        public static void SortColumns(ref Matrix4x4 B, ref Quaternion V)
        {
            Vector3 b1 = B.GetColumn(0);
            Vector3 b2 = B.GetColumn(1);
            Vector3 b3 = B.GetColumn(2);
            float p1 = b1.sqrMagnitude;
            float p2 = b2.sqrMagnitude;
            float p3 = b3.sqrMagnitude;
            bool c;
            
            c = p1 < p2;
            CondNegSwap(c, ref b1, ref b2);
            V = V * new Quaternion(0, 0, c ? 1 : 0, 1);
            CondSwap(c, ref p1, ref p2);

            c = p1 < p3;
            CondNegSwap(c, ref b1, ref b3);
            V = V * new Quaternion(0, c ? 1 : 0, 0, 1);
            CondSwap(c, ref p1, ref p3);

            c = p2 < p3;
            CondNegSwap(c, ref b2, ref b3);
            V = V * new Quaternion(c ? 1 : 0, 0, 0, 1);
            CondSwap(c, ref p2, ref p3);

            B.SetColumn(0, b1);
            B.SetColumn(1, b2);
            B.SetColumn(2, b3);
        }
    }
}