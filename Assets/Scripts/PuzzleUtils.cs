using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Parsley
{
    public static class PuzzleUtils
    {
        public static AnimationCurve CreateHighlightCurve(float duration)
        {
            float blendIn = 0.25f;
            float blendOut = duration * 0.4f;
            return new AnimationCurve(
                new Keyframe(0.0f, 0.0f),
                new Keyframe(blendIn, 1.0f),
                new Keyframe(duration * (1.0f - blendOut), 1.0f),
                new Keyframe(duration, 0.0f)
                );
        }

        /// Move a range of elements from srcStart to dstStart and shift surrounding values.
        public static void ListMoveRange<T>(List<T> array, int srcStart, int dstStart, int count)
        {
            if (srcStart == dstStart)
            {
                // Nothing to do
                return;
            }

            T[] tmp = new T[count];
            for (int i = 0; i < count; ++i)
            {
                tmp[i] = array[srcStart + i];
            }

            if (dstStart < srcStart)
            {
                int remainder = srcStart - dstStart;
                for (int i = 0; i < remainder; ++i)
                {
                    array[dstStart + count + i] = array[dstStart + i];
                }
            }
            else
            {
                int remainder = dstStart - srcStart;
                for (int i = remainder - 1; i >= 0; --i)
                {
                    array[srcStart + i] = array[srcStart + count + i];
                }
            }

            for (int i = 0; i < count; ++i)
            {
                array[dstStart + i] = tmp[i];
            }
        }

        /// Find a pose P such that distance of each shard from its transformed goal is minimal.
        public static bool FindShardCenter(IEnumerable<PuzzleShard> shards, out MixedRealityPose goalOffset, out Vector3 goalCentroid)
        {
            return FindMinErrorTransform(
                shards.Select((s) => Tuple.Create(s.Goal.Position, s.transform.position)),
                out goalOffset, out goalCentroid);
        }

        public static bool ComputeCentroid(IEnumerable<Vector3> points, out Vector3 result)
        {
            Vector3 centroid = Vector3.zero;
            int count = 0;
            foreach (var p in points)
            {
                centroid += p;
                ++count;
            }

            result = (count > 1 ? centroid / count : centroid);
            return count > 0;
        }

        public static bool FindMinErrorTransform(IEnumerable<Tuple<Vector3, Vector3>> points, out MixedRealityPose result, out Vector3 fromCentroid)
        {
            fromCentroid = Vector3.zero;
            Vector3 toCentroid = Vector3.zero;
            int count = 0;
            foreach (var p in points)
            {
                fromCentroid += p.Item1;
                toCentroid += p.Item2;
                ++count;
            }
            if (count == 0)
            {
                result = MixedRealityPose.ZeroIdentity;
                return false;
            }
            if (count == 1)
            {
                result = new MixedRealityPose(toCentroid - fromCentroid, Quaternion.identity);
                return true;
            }

            fromCentroid /= count;
            toCentroid /= count;

            if (count == 2)
            {
                var pointsArray = points.ToArray();
                Vector3 vFrom = pointsArray[1].Item1 - pointsArray[0].Item1;
                Vector3 vTo = pointsArray[1].Item2 - pointsArray[0].Item2;
                Quaternion rot = Quaternion.FromToRotation(vFrom, vTo);
                result = new MixedRealityPose(toCentroid - rot * fromCentroid, rot);
                return true;
            }

            // count >= 3, use Singular Value Decomposition to find least-squares solution

            // Build covariance matrix
            Matrix<float> H = CreateMatrix.Dense<float>(3, 3, 0.0f);
            foreach (var p in points)
            {
                Vector<float> pa = GetNVectorFromVector(p.Item1 - fromCentroid);
                Vector<float> pb = GetNVectorFromVector(p.Item2 - toCentroid);

                H += Vector<float>.OuterProduct(pa, pb);
            }

            var svdSolver = H.Svd();
            Matrix<float> rotationMatrix = (svdSolver.U * svdSolver.VT).Transpose();

            // Handle reflection case
            if (rotationMatrix.Determinant() < 0)
            {
                rotationMatrix.SetColumn(2, -rotationMatrix.Column(2));
            }

            Quaternion rotation = GetQuaternionFromNMatrix(rotationMatrix);

            result = new MixedRealityPose(toCentroid - fromCentroid, rotation);
            return true;
        }

#if true
        private static readonly Vector3 unitX = new Vector3(1, 0, 0);
        private static readonly Vector3 unitY = new Vector3(0, 1, 0);
        private static readonly Vector3 unitZ = new Vector3(0, 0, 1);

        public static bool FindMinErrorRotation(IEnumerable<Tuple<Quaternion, Quaternion>> rotations, out Quaternion result)
        {
            int count = Enumerable.Count(rotations);
            if (count == 0)
            {
                result = Quaternion.identity;
                return false;
            }
            if (count == 1)
            {
                result = rotations.First().Item2 * Quaternion.Inverse(rotations.First().Item1);
                return true;
            }

            // count >= 2, use Singular Value Decomposition to find least-squares solution

            // Build covariance matrix
            Matrix<float> H = CreateMatrix.Dense<float>(3, 3, 0.0f);
            foreach (var r in rotations)
            {
                Vector<float> xa = GetNVectorFromVector(r.Item1 * unitX);
                Vector<float> ya = GetNVectorFromVector(r.Item1 * unitY);
                // Vector<float> za = GetNVectorFromVector(r.Item1 * unitZ);
                Vector<float> xb = GetNVectorFromVector(r.Item2 * unitX);
                Vector<float> yb = GetNVectorFromVector(r.Item2 * unitY);
                // Vector<float> zb = GetNVectorFromVector(r.Item2 * unitZ);

                H += Vector<float>.OuterProduct(xa, xb);
                H += Vector<float>.OuterProduct(ya, yb);
                // H += Vector<float>.OuterProduct(za, zb);
            }

            var svdSolver = H.Svd();
            Matrix<float> rotationMatrix = (svdSolver.U * svdSolver.VT).Transpose();

            // Handle reflection case
            if (rotationMatrix.Determinant() < 0)
            {
                rotationMatrix.SetColumn(2, -rotationMatrix.Column(2));
            }

            Quaternion rotation = GetQuaternionFromNMatrix(rotationMatrix).normalized;

            result = rotation;
            return true;
        }
#endif

#if false
        public static bool FindMinErrorRotation(IEnumerable<Tuple<Quaternion, Quaternion>> rotations, out Quaternion result)
        {
            int count = Enumerable.Count(rotations);
            if (count == 0)
            {
                result = Quaternion.identity;
                return false;
            }
            if (count == 1)
            {
                result = rotations.First().Item2 * Quaternion.Inverse(rotations.First().Item1);
                return true;
            }

            // count >= 2, use Singular Value Decomposition to find least-squares solution

            // Build covariance matrix
            Matrix<float> H = CreateMatrix.Dense<float>(3, 3, 0.0f);
            foreach (var r in rotations)
            {
                Vector<float> ra = GetNVectorFromQuaternion(r.Item1);
                Vector<float> rb = GetNVectorFromQuaternion(r.Item2);

                H += Vector<float>.OuterProduct(ra, rb);
            }

            var svdSolver = H.Svd();
            Matrix<float> rotationMatrix = (svdSolver.U * svdSolver.VT).Transpose();
            if (svdSolver.ConditionNumber > 10.0f)
            {
                Debug.Log($"ugh {svdSolver.ConditionNumber}");
            }

            // Handle reflection case
            if (rotationMatrix.Determinant() < 0)
            {
                rotationMatrix.SetColumn(2, -rotationMatrix.Column(2));
            }

            Quaternion rotation = GetQuaternionFromNMatrix(rotationMatrix).normalized;

            result = rotation;
            return true;
        }
#endif

#if false
        public static bool FindMinErrorTransform(IEnumerable<Tuple<MixedRealityPose, MixedRealityPose>> points, out MixedRealityPose result, out Vector3 fromCentroid)
        {
            fromCentroid = Vector3.zero;
            Vector3 toCentroid = Vector3.zero;
            int count = 0;
            foreach (var p in points)
            {
                fromCentroid += p.Item1.Position;
                toCentroid += p.Item2.Position;
                ++count;
            }
            if (count == 0)
            {
                result = MixedRealityPose.ZeroIdentity;
                return false;
            }
            if (count == 1)
            {
                result = new MixedRealityPose(toCentroid - fromCentroid, Quaternion.identity);
                return true;
            }

            fromCentroid /= count;
            toCentroid /= count;

            if (count == 2)
            {
                var pointsArray = points.ToArray();
                Vector3 vFrom = pointsArray[1].Item1.Position - pointsArray[0].Item1.Position;
                Vector3 vTo = pointsArray[1].Item2.Position - pointsArray[0].Item2.Position;
                Quaternion rot = Quaternion.FromToRotation(vFrom, vTo);
                result = new MixedRealityPose(toCentroid - rot * fromCentroid, rot);
                return true;
            }

            // count >= 3, use Singular Value Decomposition to find least-squares solution

            // Build covariance matrix
            Matrix<float> H = CreateMatrix.Dense<float>(3, 3, 0.0f);
            foreach (var p in points)
            {
                Vector<float> pa = GetNVectorFromVector(p.Item1.Position - fromCentroid);
                Vector<float> pb = GetNVectorFromVector(p.Item2.Position - toCentroid);

                H += Vector<float>.OuterProduct(pa, pb);
            }

            var svdSolver = H.Svd();
            Matrix<float> rotationMatrix = (svdSolver.U * svdSolver.VT).Transpose();

            // Handle reflection case
            if (rotationMatrix.Determinant() < 0)
            {
                rotationMatrix.SetColumn(2, -rotationMatrix.Column(2));
            }

            Quaternion rotation = GetQuaternionFromNMatrix(rotationMatrix);

            result = new MixedRealityPose(toCentroid - fromCentroid, rotation);
            return true;
        }
#endif

        private static Vector<float> GetNVectorFromVector(Vector3 v)
        {
            Vector<float> result = CreateVector.Dense<float>(3);
            result[0] = v.x;
            result[1] = v.y;
            result[2] = v.z;
            return result;
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
        }
    }
}
