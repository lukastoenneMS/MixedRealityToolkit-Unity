// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.PoseMatching
{
    [Serializable]
    public class SplineCurve
    {
        [Serializable]
        public class ControlPoint
        {
            public Vector3 position;

            [SerializeField]
            internal float segmentStart;
            [SerializeField]
            internal float segmentLength;
        }

        [SerializeField]
        private readonly List<ControlPoint> controlPoints = new List<ControlPoint>();
        public List<ControlPoint> ControlPoints => controlPoints;

        [SerializeField]
        private float arcLength = 0.0f;
        public float ArcLength => arcLength;

        public int Count => controlPoints.Count;

        public void Append(Vector3 point)
        {
            var cp = new ControlPoint();
            cp.position = point;
            controlPoints.Add(cp);
            UpdateArcLength();
        }

        public void RemoveAt(int index)
        {
            controlPoints.RemoveAt(index);
            UpdateArcLength();
        }

        public void RemoveAll(Predicate<ControlPoint> pred)
        {
            controlPoints.RemoveAll(pred);
            UpdateArcLength();
        }

        public void RemoveRange(int index, int count)
        {
            controlPoints.RemoveRange(index, count);
            UpdateArcLength();
        }

        public void Clear()
        {
            controlPoints.Clear();
            UpdateArcLength();
        }

        private void UpdateArcLength()
        {
            float d = 0.0f;
            if (controlPoints.Count > 0)
            {
                ControlPoint prevCp = controlPoints[0];
                for (int i = 1; i < controlPoints.Count; ++i)
                {
                    ControlPoint cp = controlPoints[i];
                    float delta = (cp.position - prevCp.position).magnitude;

                    prevCp.segmentStart = d;
                    prevCp.segmentLength = delta;

                    d += delta;
                    prevCp = cp;
                }
                prevCp.segmentStart = d;
                prevCp.segmentLength = 0.0f;
            }
            arcLength = d;
        }

        public bool TryFindControlPoint(float arcLength, out int index)
        {
            int lowIdx = -1;
            int highIdx = controlPoints.Count;
            while (lowIdx < highIdx - 1)
            {
                int midIdx = (lowIdx + highIdx) >> 1;
                if (arcLength >= controlPoints[midIdx].segmentStart)
                {
                    lowIdx = midIdx;
                }
                else
                {
                    highIdx = midIdx;
                }
            }

            index = lowIdx;
            return lowIdx >= 0 && lowIdx < controlPoints.Count;
        }
    }
}
