// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.PoseMatching
{
    public class PoseConfiguration
    {
        public Vector3[] Targets { get; private set; }
        public float[] Weights { get; private set; }
    }
}