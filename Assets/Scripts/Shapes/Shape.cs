// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;
using System;
using System.Collections.Generic;
using UnityEngine;

using Pose = Microsoft.MixedReality.Toolkit.Utilities.MixedRealityPose;

namespace Microsoft.MixedReality.Toolkit.PoseMatching
{
    public interface Shape
    {
        ShapeClosestPointFinder CreateClosestPointFinder();

        void GenerateSamples(float maxSampleDistance, ShapeSampleBuffer buffer);

        Pose PrincipalComponentsTransform { get; }
        Vector3 PrincipalComponentsMoments { get; }
    }

    public interface ShapeClosestPointFinder
    {
        void Reserve(int numPoints);

        void FindClosestPoints(Vector3[] points, Vector3[] result);
    }

    public class ShapeSampleBuffer
    {
        public Vector3[] samples = new Vector3[0];

        public void Transform(Pose offset)
        {
            for (int i = 0; i < samples.Length; ++i)
            {
                samples[i] = offset.Multiply(samples[i]);
            }
        }
    }
}