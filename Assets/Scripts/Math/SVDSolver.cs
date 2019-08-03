// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;
using System.Threading.Tasks;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Utilities.MathSolvers
{
    public class SVDSolver
    {
        public Matrix4x4 U;
        public Matrix4x4 S;
        public Matrix4x4 V;

        public float ConditionNumber;

#if false
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
#else
        public void Solve(Matrix4x4 A)
        {
            Matrix<float> X = GetNMatrixFromMatrix(A);
            var svdSolver = X.Svd();

            U = GetMatrixFromNMatrix(svdSolver.U);
            S = GetDiagonalMatrixFromNVector(svdSolver.S);
            V = GetMatrixFromNMatrix(svdSolver.VT.Transpose());

            ConditionNumber = svdSolver.ConditionNumber;
        }

        public void Solve(Vector3[] rows)
        {
            Matrix<float> X = GetNMatrixFromRows(rows);
            var svdSolver = X.Svd();

            U = GetMatrixFromNMatrix(svdSolver.U);
            S = GetDiagonalMatrixFromNVector(svdSolver.S);
            V = GetMatrixFromNMatrix(svdSolver.VT.Transpose());

            ConditionNumber = svdSolver.ConditionNumber;
        }

        private static Vector<float> GetNVectorFromVector(Vector3 v)
        {
            Vector<float> result = CreateVector.Dense<float>(3);
            result[0] = v.x;
            result[1] = v.y;
            result[2] = v.z;
            return result;
        }

        private static Vector3 GetVectorFromNVector(Vector<float> nv)
        {
            return new Vector3(nv[0], nv[1], nv[2]);
        }

        private static Vector<float> GetNVectorFromQuaternion(Quaternion q)
        {
            Vector<float> result = CreateVector.Dense<float>(3);
            result[0] = q.x;
            result[1] = q.y;
            result[2] = q.z;
            return result;
        }

        private static Quaternion GetQuaternionFromNMatrix(Matrix<float> m)
        {
#if false
            // Adapted from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
            Quaternion q = new Quaternion();
            q.w = Mathf.Sqrt(Mathf.Max(0, 1 + m[0,0] + m[1,1] + m[2,2])) / 2; 
            q.x = Mathf.Sqrt(Mathf.Max(0, 1 + m[0,0] - m[1,1] - m[2,2])) / 2; 
            q.y = Mathf.Sqrt(Mathf.Max(0, 1 - m[0,0] + m[1,1] - m[2,2])) / 2; 
            q.z = Mathf.Sqrt(Mathf.Max(0, 1 - m[0,0] - m[1,1] + m[2,2])) / 2; 
            q.x *= Mathf.Sign(q.x * (m[2,1] - m[1,2]));
            q.y *= Mathf.Sign(q.y * (m[0,2] - m[2,0]));
            q.z *= Mathf.Sign(q.z * (m[1,0] - m[0,1]));
            return q;
#else
            return GetMatrixFromNMatrix(m).rotation;
#endif
        }

        private static Matrix<float> GetNMatrixFromRows(Vector3[] rows)
        {
            Matrix<float> m = CreateMatrix.Dense<float>(rows.Length, 3);
            for (int i = 0; i < rows.Length; ++i)
            {
                Vector3 row = rows[i];
                m.SetRow(i, CreateVector.Dense<float>(new float[] {row.x, row.y, row.z}));
            }
            return m;
        }

        private static Matrix<float> GetNMatrixFromMatrix(Matrix4x4 m)
        {
            Matrix<float> r = CreateMatrix.Dense<float>(3, 3);
            r[0, 0] = m.m00;
            r[1, 0] = m.m10;
            r[2, 0] = m.m20;

            r[0, 1] = m.m01;
            r[1, 1] = m.m11;
            r[2, 1] = m.m21;

            r[0, 2] = m.m02;
            r[1, 2] = m.m12;
            r[2, 2] = m.m22;

            return r;
        }

        private static Matrix4x4 GetMatrixFromNMatrix(Matrix<float> m)
        {
            Matrix4x4 r = Matrix4x4.identity;
            r.m00 = m[0, 0];
            r.m10 = m[1, 0];
            r.m20 = m[2, 0];

            r.m01 = m[0, 1];
            r.m11 = m[1, 1];
            r.m21 = m[2, 1];

            r.m02 = m[0, 2];
            r.m12 = m[1, 2];
            r.m22 = m[2, 2];

            return r;
        }

        private static Matrix4x4 GetDiagonalMatrixFromNVector(Vector<float> v)
        {
            Matrix4x4 r = Matrix4x4.identity;
            r.m00 = v[0];
            r.m11 = v[1];
            r.m22 = v[2];
            return r;
        }
#endif
    }
}