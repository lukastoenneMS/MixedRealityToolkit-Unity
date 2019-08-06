// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using MathNet.Numerics.LinearAlgebra;
using Microsoft.MixedReality.Toolkit.Utilities.MathSolvers;
using System;
using System.Collections.Generic;
using UnityEngine;

using Pose = Microsoft.MixedReality.Toolkit.Utilities.MixedRealityPose;

namespace Microsoft.MixedReality.Toolkit.Utilities.ShapeMatching
{
    [Serializable]
    public class LineShape : Shape
    {
        public struct Line
        {
            public Vector3 start;
            public Vector3 end;
        }

        private Pose principalComponentsTransform = Pose.ZeroIdentity;
        public Pose PrincipalComponentsTransform => principalComponentsTransform;
        private Vector3 principalComponentsMoments = Vector3.one;
        public Vector3 PrincipalComponentsMoments => principalComponentsMoments;

        [SerializeField]
        private readonly List<Line> lines = new List<Line>();
        public List<Line> Lines => lines;

        public ShapeClosestPointFinder CreateClosestPointFinder()
        {
            return new LineShapeClosestPointFinder(this);
        }

        public void GenerateSamples(float maxSampleDistance, ShapeSampleBuffer buffer)
        {
            int numSamples = 0;
            for (int i = 0; i < lines.Count; ++i)
            {
                Line line = lines[i];
                numSamples += GetNumLineSamples(line, maxSampleDistance);
            }

            buffer.samples = new Vector3[numSamples];

            int k = 0;
            for (int i = 0; i < lines.Count; ++i)
            {
                Line line = lines[i];
                int numLineSamples = GetNumLineSamples(line, maxSampleDistance);
                Vector3 dir = line.end - line.start;
                Vector3 delta = dir / (numLineSamples - 1);
                for (int j = 0; j < numLineSamples; ++j)
                {
                    buffer.samples[k++] = line.start + delta * j;
                }
            }
        }

        private static int GetNumLineSamples(Line line, float maxSampleDistance)
        {
            Vector3 dir = line.end - line.start;
            float length = dir.magnitude;
            return Mathf.Max(2, (int)Mathf.Ceil(length / maxSampleDistance));
        }

        public void AddLines(IEnumerable<Line> addedLines)
        {
            lines.AddRange(addedLines);

            Update();
        }

        public void AddClosedShape(IEnumerable<Vector3> points)
        {
            var iter = points.GetEnumerator();
            if (iter.MoveNext())
            {
                Vector3 first = iter.Current;
                Vector3 prev = first;
                while (iter.MoveNext())
                {
                    lines.Add(new Line() {start=prev, end=iter.Current});
                    prev = iter.Current;
                }
                lines.Add(new Line() {start=prev, end=first});
            }

            Update();
        }

        public void AddOpenShape(IEnumerable<Vector3> points)
        {
            var iter = points.GetEnumerator();
            if (iter.MoveNext())
            {
                Vector3 prev = iter.Current;
                while (iter.MoveNext())
                {
                    lines.Add(new Line() {start=prev, end=iter.Current});
                    prev = iter.Current;
                }
            }

            Update();
        }

        private void Update()
        {
            ComputePrincipalComponents(out principalComponentsTransform, out principalComponentsMoments);
        }

        private void ComputePrincipalComponents(out Pose transform, out Vector3 moments)
        {
            int count = lines.Count;
            if (count == 0)
            {
                transform = Pose.ZeroIdentity;
                moments = Vector3.one;
                return;
            }

            Vector3 centroid = Vector3.zero;
            foreach (var line in lines)
            {
                centroid += line.start;
                centroid += line.end;
            }
            centroid /= 2 * count;

            Matrix4x4 I = Matrix4x4.zero;
            foreach (var line in lines)
            {
                Vector3 c = 0.5f * (line.end + line.start);
                Vector3 d = 0.5f * (line.end - line.start);

                Matrix4x4 Idiag = MathUtils.ScalarMultiplyMatrix3x3(Matrix4x4.identity, d.sqrMagnitude / 3.0f);
                Matrix4x4 Iouter = MathUtils.ScalarMultiplyMatrix3x3(MathUtils.OuterProduct(d, d), 1.0f / 3.0f);
                Matrix4x4 Icenter = MathUtils.AddMatrix3x3(Idiag, Iouter);

                Matrix4x4 Icdiag = MathUtils.ScalarMultiplyMatrix3x3(Matrix4x4.identity, c.sqrMagnitude);
                Matrix4x4 Icouter = MathUtils.OuterProduct(c, c);
                Matrix4x4 Ioffset = MathUtils.AddMatrix3x3(Icdiag, Icouter);

                I = MathUtils.AddMatrix3x3(I, MathUtils.AddMatrix3x3(Icenter, Ioffset));
                // I = MathUtils.AddMatrix3x3(I, MathUtils.ScalarMultiplyMatrix3x3(MathUtils.OuterProduct(d, d), 1.0f / 3.0f));
                // Matrix4x4 centerCov = MathUtils.OuterProduct(line.start, line.end);
                // Matrix4x4 lineCov = MathUtils.ScalarMultiplyMatrix3x3(MathUtils.OuterProduct(d, d), 1.0f / 3.0f);
                // I = MathUtils.AddMatrix3x3(I, MathUtils.AddMatrix3x3(centerCov, lineCov));
                // int N = 1000;
                // for (int i = 0; i < N; ++i)
                // {
                //     float lambda = (float)i / (float)(N-1) - 0.5f;
                //     Vector3 q = centroid + lambda * d;
                //     I = MathUtils.AddMatrix3x3(I, MathUtils.OuterProduct(d, d));
                // }
            }

            JacobiEigenSolver eigenSolver = new JacobiEigenSolver();
            eigenSolver.Solve(I);

            transform = new Pose(centroid, eigenSolver.Q);
            moments = eigenSolver.S;
        }
    }

    public class LineShapeClosestPointFinder : ShapeClosestPointFinder
    {
        private LineShape shape;

        public LineShapeClosestPointFinder(LineShape shape)
        {
            this.shape = shape;
        }

        public void Reserve(int numPoints)
        {
        }

        public void FindClosestPoints(Vector3[] points, Vector3[] result)
        {
            // TODO:
            // - Optimize: cache line deltas, sqr lengths
            // - Parallelize: SIMD, threads (=> C++)
            for (int i = 0; i < points.Length; ++i)
            {
                Vector3 p = points[i];

                float minSqrDist = float.MaxValue;
                Vector3 closestPoint = Vector3.zero;
                for (int j = 0; j < shape.Lines.Count; ++j)
                {
                    LineShape.Line line = shape.Lines[j];
                    MathUtils.GetClosestPointOnLine(p, line.start, line.end, out Vector3 closestLinePoint, out float lambda);
                    float sqrDist = (p - closestLinePoint).sqrMagnitude;
                    if (sqrDist < minSqrDist)
                    {
                        minSqrDist = sqrDist;
                        closestPoint = closestLinePoint;
                    }
                }

                result[i] = closestPoint;
            }
        }
    }
}