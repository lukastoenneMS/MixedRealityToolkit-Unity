// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Utilities.ShapeMatching;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

using Pose = Microsoft.MixedReality.Toolkit.Utilities.MixedRealityPose;

namespace Microsoft.MixedReality.Toolkit.Utilities.MathSolvers
{
    class LineShapeRandomizer
    {
        LineShape shape;
        System.Random rng;

        public LineShapeRandomizer(LineShape shape, int seed)
        {
            this.shape = shape;
            this.rng = new System.Random(seed);
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

        int seed = 2296343;
        float expectedPositionEpsilon = 0.2f;
        float expectedAngleEpsilon = 1.0e-1f;

        // Hurwitz units that form the icosahedral group
        Quaternion[] initialRotations = new Quaternion[]
        {
            new Quaternion(+1, 0, 0, 0),
            new Quaternion(-1, 0, 0, 0),
            new Quaternion(0, +1, 0, 0),
            new Quaternion(0, -1, 0, 0),
            new Quaternion(0, 0, +1, 0),
            new Quaternion(0, 0, -1, 0),
            new Quaternion(0, 0, 0, +1),
            new Quaternion(0, 0, 0, -1),

            new Quaternion(+.5f, +.5f, +.5f, +.5f),
            new Quaternion(-.5f, +.5f, +.5f, +.5f),
            new Quaternion(+.5f, -.5f, +.5f, +.5f),
            new Quaternion(-.5f, -.5f, +.5f, +.5f),
            new Quaternion(+.5f, +.5f, -.5f, +.5f),
            new Quaternion(-.5f, +.5f, -.5f, +.5f),
            new Quaternion(+.5f, -.5f, -.5f, +.5f),
            new Quaternion(-.5f, -.5f, -.5f, +.5f),

            new Quaternion(+.5f, +.5f, +.5f, -.5f),
            new Quaternion(-.5f, +.5f, +.5f, -.5f),
            new Quaternion(+.5f, -.5f, +.5f, -.5f),
            new Quaternion(-.5f, -.5f, +.5f, -.5f),
            new Quaternion(+.5f, +.5f, -.5f, -.5f),
            new Quaternion(-.5f, +.5f, -.5f, -.5f),
            new Quaternion(+.5f, -.5f, -.5f, -.5f),
            new Quaternion(-.5f, -.5f, -.5f, -.5f),
        };

        [Test]
        public void TestShapePrincipalComponents()
        {
            LineShape shape = triangleShape;

            foreach (Quaternion initRot in initialRotations)
            {
                
            }
            // shape.PrincipalComponentsTransform
        }

        [Test]
        public void TestPCASolver()
        {
            LineShape shape = triangleShape;
            PCASolver pcaSolver = new PCASolver();
            var randomizer = new LineShapeRandomizer(shape, seed);

            int N = 100;
            float maxDist = 0.01f;
            Vector3[] points = randomizer.GetRandomPoints(N, maxDist);

            Pose offset = new Pose(new Vector3(3.6f, 0.2f, -86.2f), Quaternion.Euler(92.0f, -33.4f, -284.0f));
            for (int i = 0; i < points.Length; ++i)
            {
                points[i] = offset.Multiply(points[i]);
            }

            pcaSolver.Solve(points);

            Vector3 expectedCentroid = offset.Multiply(shape.PrincipalComponentsTransform.Position);
            Quaternion expectedRotation = offset.Multiply(shape.PrincipalComponentsTransform.Rotation);

            Vector3 pointsCentroid = pcaSolver.CentroidOffset;
            // Rotation can be rotated 180 on any axis, copy sign from expected rotation to match axis directions
            Quaternion pointsRotation = MathUtils.CopySign(pcaSolver.RotationOffset, expectedRotation);

            Vector3 centroidDelta = pointsCentroid - expectedCentroid;
            Quaternion rotationDelta = pointsRotation * Quaternion.Inverse(expectedRotation);
            rotationDelta.ToAngleAxis(out float rotationDeltaAngle, out Vector3 rotationDeltaAxis);
            // Move angle to -180..180 range instead of 0..360 for comparisons
            rotationDeltaAngle = rotationDeltaAngle < 180.0f ? rotationDeltaAngle : rotationDeltaAngle - 360.0f;

            Assert.AreEqual(0.0f, centroidDelta.magnitude, expectedPositionEpsilon);
            Assert.AreEqual(0.0f, rotationDeltaAngle, expectedAngleEpsilon);
            Debug.Log($"DELTA: {centroidDelta.magnitude} {rotationDeltaAngle}");
        }
    }
}