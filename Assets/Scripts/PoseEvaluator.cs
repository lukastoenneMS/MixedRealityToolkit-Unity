// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;

using Pose = Microsoft.MixedReality.Toolkit.Utilities.MixedRealityPose;

namespace Microsoft.MixedReality.Toolkit.PoseMatching
{
    public class PoseEvaluator
    {
        public PoseMatch EvaluatePose(Vector3[] input, PoseConfiguration config)
        {
            if (input.Length != config.Length)
            {
                throw new ArgumentException($"Input size {input.Length} does not match configuration size {config.Length}");
            }

            var rng = new System.Random(4364);
            float[] residuals = new float[config.Length];
            float sum = 0.0f;
            for (int i = 0; i < config.Length; ++i)
            {
                residuals[i] = (float)rng.NextDouble();
                sum += residuals[i];
            }
            float MSE = sum / Mathf.Max(1, config.Length);

            var result = new PoseMatch(residuals, MSE, Pose.ZeroIdentity);
            return result;
        }
    }
}