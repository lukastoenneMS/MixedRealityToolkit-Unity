using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
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
        public static bool FindShardGoalPose(PuzzleShard[] shards, out MixedRealityPose goalOffset, out MixedRealityPose goalCenter)
        {
            if (shards.Length == 0)
            {
                goalOffset = MixedRealityPose.ZeroIdentity;
                goalCenter = MixedRealityPose.ZeroIdentity;
                return false;
            }

            Vector3 centroid = Vector3.zero;
            Vector3 goalCentroid = Vector3.zero;
            for (int i = 0; i < shards.Length; ++i)
            {
                centroid += shards[i].transform.position;
                goalCentroid += shards[i].Goal.Position;
            }
            centroid /= shards.Length;
            goalCentroid /= shards.Length;

            // Build covariance matrix
            Matrix<float> H = CreateMatrix.Dense<float>(3, 3, 0.0f);
            for (int i = 0; i < shards.Length; ++i)
            {
                Vector<float> pa = GetNVectorFromVector(shards[i].Goal.Position - goalCentroid);
                Vector<float> pb = GetNVectorFromVector(shards[i].transform.position - centroid);

                H += Vector<float>.OuterProduct(pa, pb);
            }

            var svdSolver = H.Svd();
            Matrix<float> rotationMatrix = (svdSolver.U * svdSolver.VT).Transpose();
            Quaternion rotation = GetQuaternionFromNMatrix(rotationMatrix);

            goalOffset = new MixedRealityPose(centroid - rotation * goalCentroid, rotation);
            goalCenter = new MixedRealityPose(goalCentroid, Quaternion.identity);
            return true;
        }

        private static Vector<float> GetNVectorFromVector(Vector3 v)
        {
            Vector<float> result = CreateVector.Dense<float>(3);
            result[0] = v.x;
            result[1] = v.y;
            result[2] = v.z;
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
