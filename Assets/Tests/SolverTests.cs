// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

using Pose = Microsoft.MixedReality.Toolkit.Utilities.MixedRealityPose;

namespace Microsoft.MixedReality.Toolkit.PoseMatching
{
    class LineShapeRandomizer
    {
        LineShape shape;
        System.Random rng;

        public LineShapeRandomizer(LineShape shape)
        {
            this.shape = shape;
            this.rng = new System.Random();
        }

        public Vector3[] GetRandomPoints(int count, float maxDist)
        {
            Vector3[] result = new Vector3[count];
            for (int i = 0; i < count; ++i)
            {
                int L = rng.Next(shape.Lines.Count);
                LineShape.Line line = shape.Lines[L];
                Vector3 a = line.end - line.start;
                Vector3 b = Vector3.Cross(Vector3.up, a).normalized;
                Vector3 c = Vector3.Cross(a, b).normalized;

                float u = (float)rng.NextDouble();
                float v = (float)rng.NextDouble();
                float w = (float)rng.NextDouble();
                float t = Mathf.Sqrt(v * v + w * w);
                float vw = t > 0.0f ? maxDist / t : 0.0f;
                result[i] = line.start + u * a + vw * v * b + vw * w * c;
            }
            return result;
        }
    }

    class SolverTests
    {
        LineShape triangleShape = LineShapeUtils.CreateTriangle(-0.3f, 0.2f, -0.5f, -0.1f, 1.6f, -0.4f);

        [Test]
        public void TestPCASolver()
        {
            LineShape shape = triangleShape;
            PCASolver pcaSolver = new PCASolver();
            var randomizer = new LineShapeRandomizer(shape);

            int N = 100;
            float maxDist = 0.01f;
            Vector3[] points = randomizer.GetRandomPoints(N, maxDist);

            Vector3 translation = new Vector3(3.6f, 0.2f, -86.2f);
            Quaternion rotation = Quaternion.Euler(92.0f, -33.4f, -284.0f);
            for (int i = 0; i < points.Length; ++i)
            {
                points[i] = rotation * points[i] + translation;
            }

            pcaSolver.Solve(points);
            
            Vector3 translationDelta = pcaSolver.CentroidOffset - shape.PrincipalComponentsTransform.Position;
            Quaternion rotationDelta = pcaSolver.RotationOffset * Quaternion.Inverse(shape.PrincipalComponentsTransform.Rotation);
            rotationDelta.ToAngleAxis(out float rotationDeltaAngle, out Vector3 rotationDeltaAxis);
            Debug.Log($"DELTA: {(translationDelta).magnitude} {rotationDeltaAngle}");
            // Debug.Assert(pcaSolver.CentroidOffset)

            // Pose inputPCAPose = new Pose(pcaSolver.CentroidOffset, pcaSolver.RotationOffset);
        }
    }
}