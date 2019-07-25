// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.PoseMatching
{
    public interface ICPClosestPointFinder
    {
        void Reserve(int numPoints);

        void FindClosestPoints(Vector3[] points, Vector3[] result);
    }

    [Serializable]
    public class LineShape : ICPShape
    {
        public struct Line
        {
            public Vector3 start;
            public Vector3 end;
        }

        [SerializeField]
        private readonly List<Line> lines = new List<Line>();
        public List<Line> Lines => lines;

        public ICPClosestPointFinder CreateClosestPointFinder()
        {
            return new LineShapeClosestPointFinder(this);
        }

        // XXX arbitrary?
        public int MinimumPointCount => 8;

        public void AddLines(IEnumerable<Line> addedLines)
        {
            lines.AddRange(addedLines);
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
        }
    }

    public class LineShapeClosestPointFinder : ICPClosestPointFinder
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
                    GetClosestPointOnLine(p, shape.Lines[j], out Vector3 closestLinePoint, out float lambda);
                    float sqrDist = (p - closestPoint).sqrMagnitude;
                    if (sqrDist < minSqrDist)
                    {
                        minSqrDist = sqrDist;
                        closestPoint = closestLinePoint;
                    }
                }

                result[i] = closestPoint;
            }
        }

        private static void GetClosestPointOnLine(Vector3 point, LineShape.Line line, out Vector3 closestPoint, out float lambda)
        {
            Vector3 dir = line.end - line.start;
            Vector3 q = point - line.start;
            float sqrLen = dir.sqrMagnitude;
            lambda = sqrLen > 0.0f ? Vector3.Dot(dir, q) / sqrLen : 0.0f;
            closestPoint = lambda <= 0.0f ? line.start : (lambda >= 1.0f ? line.end : line.start + lambda * dir);
        }
    }
}