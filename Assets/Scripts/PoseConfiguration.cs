// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.PoseMatching
{
    public class PoseConfiguration
    {
        public Vector3[] Targets { get; private set; }
        public float[] Weights { get; private set; }

        public int Length => Targets.Length;

        public PoseConfiguration(Vector3[] targets, float[] weights)
        {
            Debug.Assert(weights.Length == targets.Length);
            this.Targets = targets;
            this.Weights = weights;
        }

        public PoseConfiguration(Vector3[] targets)
        {
            this.Targets = targets;
            this.Weights = new float[targets.Length];
            for (int i = 0; i < targets.Length; ++i)
            {
                Weights[i] = 1.0f;
            }
        }
    }
}