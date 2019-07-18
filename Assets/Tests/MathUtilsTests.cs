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
    }
}