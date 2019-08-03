// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Utilities.MathSolvers
{
    class MathUtilsTests
    {
        const float ExpectedInvSqrtAccuracy = 0.01f;

        private static readonly float[] values = new float[]
        {
            1.0f, 2.0f, 3.0f, 4.0f,
            0.1f, 0.01f, 0.001f, 1.0e-12f,
            10.0f, 100.0f, 1000.0f, 1.0e12f,
            (float)Math.PI, (float)Math.E, 
        };

        private static readonly float[][] matrixValues = new float[][]
        {
            new float[] {1.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f},
            new float[] {1.0f, 10.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f},
            new float[] {1.0f, 10.0f, 10.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f},
            new float[] {1.0f, 0.0f, 0.0f, 10.0f, 5.0f, 10.0f, 0.0f, 10.0f, 1.0f},
            new float[] {100.0f, 0.0f, 0.0f, 100.0f, 0.001f, 0.0f, 0.0f, 2.0f, 1.0f},
            new float[] {10.0f, 5.0f, -3.0f, 100.0f, 0.1f, 1.0f, -20.0f, 2.0f, 1.0f},
        };

        private static IEnumerable<Matrix4x4> matrices => matrixValues.Select(m =>
        {
            Matrix4x4 A = Matrix4x4.identity;
            A.m00 = m[0];
            A.m10 = m[1];
            A.m20 = m[2];
            A.m01 = m[3];
            A.m11 = m[4];
            A.m21 = m[5];
            A.m02 = m[6];
            A.m12 = m[7];
            A.m22 = m[8];
            return A;
        });

        [Test]
        public void InvSqrt()
        {
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

        [Test]
        public void SortColumnsTest()
        {
            foreach (Matrix4x4 M in matrices)
            {
                Matrix4x4 Am = M;
                Matrix4x4 V = M;

                MathUtils.SortColumns(ref Am, ref V);

                Assert.GreaterOrEqual(Am.GetColumn(0).sqrMagnitude, Am.GetColumn(1).sqrMagnitude);
                Assert.GreaterOrEqual(Am.GetColumn(1).sqrMagnitude, Am.GetColumn(2).sqrMagnitude);

                Matrix4x4 Aq = M;
                Quaternion Q = M.rotation;
                MathUtils.SortColumns(ref Aq, ref Q);

                Assert.GreaterOrEqual(Aq.GetColumn(0).sqrMagnitude, Aq.GetColumn(1).sqrMagnitude);
                Assert.GreaterOrEqual(Aq.GetColumn(1).sqrMagnitude, Aq.GetColumn(2).sqrMagnitude);
            }
        }

        const float ExpectedEigenAccuracy = 1.0e-2f;

        [Test]
        public void EigenSolverTest()
        {
            JacobiEigenSolver solver = new JacobiEigenSolver();

            foreach (Matrix4x4 M in matrices)
            {
                Matrix4x4 A = M.transpose * M;

                solver.Solve(A);
                // string str = $"[{solver.S.m00:F3} {solver.S.m01:F3} {solver.S.m02:F3}]\n[{solver.S.m10:F3} {solver.S.m11:F3} {solver.S.m12:F3}]\n[{solver.S.m20:F3} {solver.S.m21:F3} {solver.S.m22:F3}]";
                // Debug.Log($"RES={solver.residual} | {str}");

                float max = Mathf.Sqrt(Mathf.Max(
                    A.GetColumn(0).sqrMagnitude,
                    A.GetColumn(1).sqrMagnitude,
                    A.GetColumn(2).sqrMagnitude));
                Matrix4x4 Qm = Matrix4x4.Rotate(solver.Q);
                Matrix4x4 R = Qm * solver.S * Qm.transpose;
                for (int i = 0; i < 3; ++i)
                {
                    for (int j = 0; j < 3; ++j)
                    {
                        Assert.AreEqual(A[i, j], R[i, j], max * ExpectedEigenAccuracy);
                    }
                }

                Assert.LessOrEqual(solver.residual, solver.squaredErrorThreshold);
            }
        }

        const float ExpectedQREpsilon = 1.0e-4f;
        const float ExpectedQRAccuracy = 1.0e-3f;

        [Test]
        public void QRDecompositionTest()
        {
            QRSolver solver = new QRSolver();

            foreach (Matrix4x4 M in matrices)
            {
                Matrix4x4 A = M;

                solver.Solve(A);
                // string str = $"[{solver.S.m00:F3} {solver.S.m01:F3} {solver.S.m02:F3}]\n[{solver.S.m10:F3} {solver.S.m11:F3} {solver.S.m12:F3}]\n[{solver.S.m20:F3} {solver.S.m21:F3} {solver.S.m22:F3}]";
                // Debug.Log($"RES={solver.residual} | {str}");

                float max = Mathf.Sqrt(Mathf.Max(
                    A.GetColumn(0).sqrMagnitude,
                    A.GetColumn(1).sqrMagnitude,
                    A.GetColumn(2).sqrMagnitude));
                Matrix4x4 Qm = Matrix4x4.Rotate(solver.Q);
                Matrix4x4 R = Qm * solver.R;
                for (int i = 0; i < 3; ++i)
                {
                    for (int j = 0; j < 3; ++j)
                    {
                        Assert.AreEqual(A[i, j], R[i, j], max * ExpectedQRAccuracy);
                    }
                }

                Assert.AreEqual(0.0f, Math.Abs(solver.R.m10), ExpectedQREpsilon);
                Assert.AreEqual(0.0f, Math.Abs(solver.R.m20), ExpectedQREpsilon);
                Assert.AreEqual(0.0f, Math.Abs(solver.R.m21), ExpectedQREpsilon);
            }
        }

        const float ExpectedSvdAccuracy = 1.0e-3f;

        [Test]
        public void SVDTest()
        {
            SVDSolver solver = new SVDSolver();

            foreach (Matrix4x4 M in matrices)
            {
                Matrix4x4 A = M;

                solver.Solve(A);

                float max = Mathf.Sqrt(Mathf.Max(
                    A.GetColumn(0).sqrMagnitude,
                    A.GetColumn(1).sqrMagnitude,
                    A.GetColumn(2).sqrMagnitude));
                Matrix4x4 R = solver.U * solver.S * solver.V.transpose;
                for (int i = 0; i < 3; ++i)
                {
                    for (int j = 0; j < 3; ++j)
                    {
                        Assert.AreEqual(A[i, j], R[i, j], max * ExpectedSvdAccuracy);
                    }
                }
            }
        }
    }
}