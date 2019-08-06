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
        }

        public ShapeType TestShape = ShapeType.Triangle1;

        private static LineShape baseTriangle = LineShapeUtils.CreateTriangle(-0.3f, 0.2f, -0.5f, -0.1f, 1.6f, -0.4f);
        private static LineShape baseCircle = LineShapeUtils.CreateCircle(1.0f, 17);
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
        };

        public GameObject PrincipalComponentsPrefab;
        private GameObject principalComponents;

        protected new void Awake()
        {
            base.Awake();

            if (shapes.TryGetValue(TestShape, out Shape shape))
            {
                UpdateShapeMesh(shape);

                if (PrincipalComponentsPrefab)
                {
                    principalComponents = GameObject.Instantiate(PrincipalComponentsPrefab, transform);
                }
                principalComponents.transform.localPosition = shape.PrincipalComponentsTransform.Position;
                principalComponents.transform.localRotation = shape.PrincipalComponentsTransform.Rotation;
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
    }
}

