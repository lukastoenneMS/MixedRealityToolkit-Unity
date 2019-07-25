// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.PoseMatching
{
    public static class LineShapeUtils
    {
        public static LineShape CreateCircle(float radius, int numPoints)
        {
            Vector3[] points = new Vector3[numPoints];
            for (int i = 0; i < numPoints; ++i)
            {
                float a = 2.0f * Mathf.PI * (float)i / (float)(numPoints - 1);
                points[i] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * radius;
            }

            LineShape shape = new LineShape();
            shape.AddClosedShape(points);

            return shape;
        }
    }
}