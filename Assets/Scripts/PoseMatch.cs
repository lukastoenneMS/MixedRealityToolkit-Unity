// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

using Pose = Microsoft.MixedReality.Toolkit.Utilities.MixedRealityPose;

namespace Microsoft.MixedReality.Toolkit.PoseMatching
{
    public class PoseMatch
    {
        public float[] Residuals;
        public float MeanSquaredError;

        public Pose Pose;

        public PoseMatch(float[] residuals, float MSE, Pose pose)
        {
            this.Residuals = residuals;
            this.MeanSquaredError = MSE;

            this.Pose = pose;
        }
    }
}