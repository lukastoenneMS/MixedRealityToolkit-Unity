// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.PoseMatching
{
    public interface ICPShape
    {
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
        private List<Line> lines;
        public List<Line> Lines => lines;

        public void FindClosestPoints(Vector3[] points, Vector3[] result)
        {

        }

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
}