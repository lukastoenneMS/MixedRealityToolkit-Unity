// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Utilities.ShapeMatching
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

        public static LineShape CreateArrow(float length, float width)
        {
            LineShape shape = new LineShape();
            shape.AddOpenShape(new Vector3[]
            {
                new Vector3(-length/2, width/2, 0),
                new Vector3(length/2, 0, 0),
                new Vector3(-length/2, -width/2, 0),
            });

            return shape;
        }

        public static LineShape CreateRectangle(float width, float height)
        {
            LineShape shape = new LineShape();
            shape.AddClosedShape(new Vector3[]
            {
                new Vector3(-width/2, -height/2, 0),
                new Vector3(width/2, -height/2, 0),
                new Vector3(width/2, height/2, 0),
                new Vector3(-width/2, height/2, 0),
            });

            return shape;
        }

        public static LineShape CreateTriangle(float x1, float y1, float x2, float y2, float x3, float y3)
        {
            LineShape shape = new LineShape();
            shape.AddClosedShape(new Vector3[]
            {
                new Vector3(x1, y1),
                new Vector3(x2, y2),
                new Vector3(x3, y3),
            });

            return shape;
        }
    }
}