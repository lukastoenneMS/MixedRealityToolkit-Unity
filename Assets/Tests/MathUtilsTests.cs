// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;

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
                (float)System.Math.PI, (float)System.Math.E, 
            };

            foreach (float v in values)
            {
                float expected = (float)(1.0 / System.Math.Sqrt(v));
                float actual = MathUtils.OneOverSqrt(v);
                float delta = ExpectedInvSqrtAccuracy * expected;
                Assert.AreEqual(expected, actual, delta, $"1/sqrt({v}): Expected value is {expected}, actual value is {actual}");
            }
        }

        const float ExpectedGivensEpsilon = 0.01f;

        [Test]
        public void ApproximateGivensRotation()
        {
            float[][] values = new float[][]
            {
                new float[] { 1.0f, 0.0f, 1.0f },
            };

            foreach (float[] v in values)
            {
                float a11 = v[0];
                float a12 = v[1];
                float a21 = a12;
                float a22 = v[2];

                MathUtils.ApproximateGivensRotation(a11, a12, a21, out float c, out float s);
                Assert.LessOrEqual(c, 1.0f);
                Assert.GreaterOrEqual(c, -1.0f);
                Assert.LessOrEqual(s, 1.0f);
                Assert.GreaterOrEqual(s, -1.0f);
                Assert.AreEqual(1.0f, c*c + s*s, ExpectedGivensEpsilon);

                float aa11 =  a11 * c + a12 * s;
                float aa12 = -a11 * s + a12 * c;
                float aa21 =  a21 * c + a22 * s;
                float aa22 = -a21 * s + a22 * c;

                float aaa11 = c * aa11 - s * aa21;
                float aaa12 = c * aa12 - s * aa22;
                float aaa21 = s * aa11 + c * aa21;
                float aaa22 = s * aa12 + c * aa22;

                Assert.AreEqual(0.0f, aaa12, ExpectedGivensEpsilon, $"Givens rotation for (({v[0]}, {v[1]}), ({v[1]}, {v[2]})) does not diagonalize matrix");
                Assert.AreEqual(0.0f, aaa21, ExpectedGivensEpsilon, $"Givens rotation for (({v[0]}, {v[1]}), ({v[1]}, {v[2]})) does not diagonalize matrix");
            }
        }
    }
}