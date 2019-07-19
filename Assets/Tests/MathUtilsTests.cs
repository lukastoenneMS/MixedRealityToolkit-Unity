// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;
using System;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.PoseMatching
{
    class MathUtilsTests
    {
        const float ExpectedInvSqrtAccuracy = 0.01f;

        [Test]
        public void InvSqrt()
        {
            float[] values = new float[]
            {
                1.0f, 2.0f, 3.0f, 4.0f,
                0.1f, 0.01f, 0.001f, 1.0e-12f,
                10.0f, 100.0f, 1000.0f, 1.0e12f,
                (float)Math.PI, (float)Math.E, 
            };

            foreach (float v in values)
            {
                float expected = (float)(1.0 / Math.Sqrt(v));
                float actual = MathUtils.OneOverSqrt(v);
                float delta = ExpectedInvSqrtAccuracy * expected;
                Assert.AreEqual(expected, actual, delta, $"1/sqrt({v}): Expected value is {expected}, actual value is {actual}");
            }
        }

        const float ExpectedGivensEpsilon = 0.01f;

        [Test]
        public void ApproximateGivensRotation()
        {
            float[] values = new float[]
            {
                1.0f, 2.0f, 3.0f, 4.0f,
                0.1f, 0.01f, 0.001f, 1.0e-12f,
                10.0f, 100.0f, 1000.0f, 1.0e12f,
                (float)Math.PI, (float)Math.E, 
            };

            foreach (float a11 in values)
            {
                foreach (float a12 in values)
                {
                    foreach (float a22 in values)
                    {
                        TestGivensApproximationCosSin( a11,  a12,  a22);
                        TestGivensApproximationCosSin(-a11,  a12,  a22);
                        TestGivensApproximationCosSin( a11, -a12,  a22);
                        TestGivensApproximationCosSin(-a11, -a12,  a22);
                        TestGivensApproximationCosSin( a11,  a12, -a22);
                        TestGivensApproximationCosSin(-a11,  a12, -a22);
                        TestGivensApproximationCosSin( a11, -a12, -a22);
                        TestGivensApproximationCosSin(-a11, -a12, -a22);

                        TestGivensApproximationQuaternion( a11,  a12,  a22);
                        TestGivensApproximationQuaternion(-a11,  a12,  a22);
                        TestGivensApproximationQuaternion( a11, -a12,  a22);
                        TestGivensApproximationQuaternion(-a11, -a12,  a22);
                        TestGivensApproximationQuaternion( a11,  a12, -a22);
                        TestGivensApproximationQuaternion(-a11,  a12, -a22);
                        TestGivensApproximationQuaternion( a11, -a12, -a22);
                        TestGivensApproximationQuaternion(-a11, -a12, -a22);
                    }
                }
            }
        }

        void TestGivensApproximationCosSin(float a11, float a12, float a22)
        {
            MathUtils.ApproximateGivensRotationCosSin(a11, a12, a22, out float c, out float s);
            Assert.LessOrEqual(c, 1.0f);
            Assert.GreaterOrEqual(c, -1.0f);
            Assert.LessOrEqual(s, 1.0f);
            Assert.GreaterOrEqual(s, -1.0f);
            Assert.AreEqual(1.0f, c*c + s*s, ExpectedGivensEpsilon);

            float a21 = a12;

            float aa11 =  a11 * c + a12 * s;
            float aa12 = -a11 * s + a12 * c;
            float aa21 =  a21 * c + a22 * s;
            float aa22 = -a21 * s + a22 * c;

            float aaa11 =  c * aa11 + s * aa21;
            float aaa12 =  c * aa12 + s * aa22;
            float aaa21 = -s * aa11 + c * aa21;
            float aaa22 = -s * aa12 + c * aa22;

            Assert.LessOrEqual(Math.Abs(aaa12), Math.Abs(a12), $"Givens rotation for (({a11}, {a12}), ({a21}, {a22})) does not diagonalize matrix");
            Assert.LessOrEqual(Math.Abs(aaa21), Math.Abs(a21), $"Givens rotation for (({a11}, {a12}), ({a21}, {a22})) does not diagonalize matrix");
        }

        void TestGivensApproximationQuaternion(float a11, float a12, float a22)
        {
            MathUtils.ApproximateGivensRotationQuaternion(a11, a12, a22, out float x, out float w);
            Assert.LessOrEqual(x, 1.0f);
            Assert.GreaterOrEqual(x, -1.0f);
            Assert.LessOrEqual(w, 1.0f);
            Assert.GreaterOrEqual(w, -1.0f);
            Assert.AreEqual(1.0f, x*x + w*w, ExpectedGivensEpsilon);

            float a21 = a12;
            Matrix4x4 m = new Matrix4x4(new Vector4(a11, a21, 0, 0), new Vector4(a12, a22, 0, 0), new Vector4(0, 0, 1, 0), new Vector4(0, 0, 0, 1));

            Matrix4x4 qm = Matrix4x4.Rotate(new Quaternion(x, 0, 0, w));
            Matrix4x4 rm = qm.transpose * m * qm;

            Assert.LessOrEqual(Math.Abs(rm.m10), Math.Abs(a21), $"Givens rotation for (({a11}, {a12}), ({a21}, {a22})) does not diagonalize matrix");
            Assert.LessOrEqual(Math.Abs(rm.m01), Math.Abs(a12), $"Givens rotation for (({a11}, {a12}), ({a21}, {a22})) does not diagonalize matrix");
        }
    }
}