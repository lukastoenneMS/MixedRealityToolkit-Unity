// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

using Pose = Microsoft.MixedReality.Toolkit.Utilities.MixedRealityPose;

namespace Microsoft.MixedReality.Toolkit.Utilities.ShapeMatching
{
    public class PoseMatch
    {
        public Pose Offset;
        public float ConditionNumber;

        public PoseMatch(Pose offset, float conditionNumber)
        {
            this.Offset = offset;
            this.ConditionNumber = conditionNumber;
        }
    }
}