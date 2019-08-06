// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Utilities.MathSolvers;
using Microsoft.MixedReality.Toolkit.Utilities.ShapeMatching;
using System;
using System.Collections.Generic;
using UnityEngine;

using Pose = Microsoft.MixedReality.Toolkit.Utilities.MixedRealityPose;

namespace Microsoft.MixedReality.Toolkit.Examples.Demos.ShapeMatching
{
    [RequireComponent(typeof(MeshFilter))]
    public class ShapeTestVisualizer : ShapeRenderer
    {
        public enum ShapeType
        {
            Triangle1,
            Triangle2,
            Triangle3,
            Triangle4,
            Circle1,
            Circle2,
            Circle3,
            Circle4,
            TwistedCircle1,
            TwistedCircle2,
            TwistedCircle3,
            TwistedCircle4,
        }

        public ShapeType TestShape = ShapeType.Triangle1;

        private static LineShape baseTriangle = LineShapeUtils.CreateTriangle(-0.3f, 0.2f, -0.5f, -0.1f, 1.6f, -0.4f);
        private static LineShape baseCircle = LineShapeUtils.CreateCircle(1.0f, 17);
        private static LineShape baseCircle2 = CreateTwistedCircle(1.0f, 17, 60.0f);
        private static Pose pose0 = Pose.ZeroIdentity;
        private static Pose pose1 = new Pose(new Vector3(.3f, -.5f, -0.2f), Quaternion.identity);
        private static Pose pose2 = new Pose(new Vector3(.3f, -.5f, -0.2f), Quaternion.Euler(82, -20, 193));
        private static Vector3 scale0 = Vector3.one;
        private static Vector3 scale1 = new Vector3(0.6f, 3.6f, 0.1f);
        private static readonly Dictionary<ShapeType, Shape> shapes = new Dictionary<ShapeType, Shape>()
        {
            { ShapeType.Triangle1, baseTriangle },
            { ShapeType.Triangle2, CreateTransformedShape(baseTriangle, pose1, scale0) },
            { ShapeType.Triangle3, CreateTransformedShape(baseTriangle, pose2, scale0) },
            { ShapeType.Triangle4, CreateTransformedShape(baseTriangle, pose2, scale1) },
            { ShapeType.Circle1, baseCircle },
            { ShapeType.Circle2, CreateTransformedShape(baseCircle, pose1, scale0) },
            { ShapeType.Circle3, CreateTransformedShape(baseCircle, pose2, scale0) },
            { ShapeType.Circle4, CreateTransformedShape(baseCircle, pose2, scale1) },
            { ShapeType.TwistedCircle1, baseCircle2 },
            { ShapeType.TwistedCircle2, CreateTransformedShape(baseCircle2, pose1, scale0) },
            { ShapeType.TwistedCircle3, CreateTransformedShape(baseCircle2, pose2, scale0) },
            { ShapeType.TwistedCircle4, CreateTransformedShape(baseCircle2, pose2, scale1) },
        };

        public GameObject AxisPrefab;
        private GameObject transformViz;
        private GameObject axisX;
        private GameObject axisY;
        private GameObject axisZ;

        protected new void Awake()
        {
            base.Awake();

            if (shapes.TryGetValue(TestShape, out Shape shape))
            {
                UpdateShapeMesh(shape);

                if (AxisPrefab)
                {
                    transformViz = new GameObject("Transform Visualizer");
                    transformViz.transform.SetParent(transform, false);

                    axisX = GameObject.Instantiate(AxisPrefab, transformViz.transform);
                    axisY = GameObject.Instantiate(AxisPrefab, transformViz.transform);
                    axisZ = GameObject.Instantiate(AxisPrefab, transformViz.transform);
                    axisX.transform.localPosition = Vector3.zero;
                    axisY.transform.localPosition = Vector3.zero;
                    axisZ.transform.localPosition = Vector3.zero;
                    axisX.transform.localRotation = Quaternion.identity;
                    axisY.transform.localRotation = Quaternion.Euler(0, 0, 90);
                    axisZ.transform.localRotation = Quaternion.Euler(0, -90, 0);

                    var rendererX = axisX.GetComponentInChildren<Renderer>();
                    var rendererY = axisY.GetComponentInChildren<Renderer>();
                    var rendererZ = axisZ.GetComponentInChildren<Renderer>();

                    MaterialPropertyBlock props = new MaterialPropertyBlock();
                    props.SetColor("_Color", Color.red);
                    rendererX.SetPropertyBlock(props);
                    props.SetColor("_Color", Color.green);
                    rendererY.SetPropertyBlock(props);
                    props.SetColor("_Color", Color.blue);
                    rendererZ.SetPropertyBlock(props);
                }

                transformViz.transform.localPosition = shape.PrincipalComponentsTransform.Position;
                transformViz.transform.localRotation = shape.PrincipalComponentsTransform.Rotation;
                axisX.transform.localScale = new Vector3(shape.PrincipalComponentsMoments.x, 1, 1);
                axisY.transform.localScale = new Vector3(shape.PrincipalComponentsMoments.y, 1, 1);
                axisZ.transform.localScale = new Vector3(shape.PrincipalComponentsMoments.z, 1, 1);
            }
        }

        private static LineShape CreateTransformedShape(LineShape shape, Pose transform, Vector3 scale)
        {
            LineShape result = new LineShape();
            LineShape.Line[] newLines = new LineShape.Line[shape.Lines.Count];
            for (int i = 0; i < shape.Lines.Count; ++i)
            {
                Vector3 start = shape.Lines[i].start;
                Vector3 end = shape.Lines[i].end;

                start.Scale(scale);
                end.Scale(scale);

                start = transform.Multiply(start);
                end = transform.Multiply(end);

                newLines[i] = new LineShape.Line() {start=start, end=end};
            }

            result.AddLines(newLines);
            return result;
        }

        private static LineShape CreateTwistedCircle(float radius, int numPoints, float twistAngle)
        {
            Vector3[] points = new Vector3[numPoints];
            for (int i = 0; i < numPoints; ++i)
            {
                float a = 2.0f * Mathf.PI * (float)i / (float)(numPoints - 1);
                float b = Mathf.Cos(a) * Mathf.Deg2Rad * twistAngle;
                points[i] = new Vector3(Mathf.Cos(a), Mathf.Sin(a) * Mathf.Cos(b), Mathf.Sin(a) * Mathf.Sin(b)) * radius;
            }

            LineShape shape = new LineShape();
            shape.AddClosedShape(points);

            return shape;
        }
    }
}

