// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.PoseMatching
{
    public class QRSolver
    {
        public Quaternion Q;
        public Matrix4x4 R;

        float epsilon = 1.0e-6f;

        public void Solve(Matrix4x4 A)
        {
            Init(A);

            {
                Quaternion Qk = Quaternion.identity;
                QRGivensQuaternion(R.m00, R.m10, out Qk.z, out Qk.w, epsilon);
                Quaternion invQk = Quaternion.Inverse(Qk);

                Q = Q * Qk;
                R = Matrix4x4.Rotate(invQk) * R;
            }
            {
                Quaternion invQk = Quaternion.identity;
                QRGivensQuaternion(R.m00, R.m20, out invQk.y, out invQk.w, epsilon);
                Quaternion Qk = Quaternion.Inverse(invQk);

                Q = Q * Qk;
                R = Matrix4x4.Rotate(invQk) * R;
            }
            {
                Quaternion Qk = Quaternion.identity;
                QRGivensQuaternion(R.m11, R.m21, out Qk.x, out Qk.w, epsilon);
                Quaternion invQk = Quaternion.Inverse(Qk);

                Q = Q * Qk;
                R = Matrix4x4.Rotate(invQk) * R;
            }
        }

        public void Init(Matrix4x4 A)
        {
            R = A;
            Q = Quaternion.identity;
        }

        public static void QRGivensQuaternion(float a1, float a2, out float r, out float w, float epsilon)
        {
            // XXX needs accurate sqrt here, paper suggests using platform rsqrt, but isn't implenented for standard C#
            // float p = MathUtils.Sqrt(a1*a1 + a2*a2);
            float p = Mathf.Sqrt(a1*a1 + a2*a2);
            float sh = (p > epsilon) ? a2 : 0.0f;
            float ch = Mathf.Abs(a1) + Mathf.Max(p, epsilon);
            bool b = (a1 < 0.0f);
            MathUtils.CondSwap(b, ref sh, ref ch);
            // float omega = MathUtils.OneOverSqrt(ch*ch + sh*sh);
            float omega = 1.0f / Mathf.Sqrt(ch*ch + sh*sh);
            r = omega * sh;
            w = omega * ch;
        }
    }
}