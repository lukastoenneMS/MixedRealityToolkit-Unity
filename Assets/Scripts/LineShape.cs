// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Pose = Microsoft.MixedReality.Toolkit.Utilities.MixedRealityPose;

namespace Microsoft.MixedReality.Toolkit.PoseMatching
{
    [Serializable]
    public class LineShape : ICPShape
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

        public ICPClosestPointFinder CreateClosestPointFinder()
        {
            return new LineShapeClosestPointFinder(this);
        }

        public void GenerateSamples(float maxSampleDistance, ICPSampleBuffer buffer)
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

        private void Update()
        {
            

            principalComponentsTransform = Pose.ZeroIdentity;
            principalComponentsMoments = Vector3.one;
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