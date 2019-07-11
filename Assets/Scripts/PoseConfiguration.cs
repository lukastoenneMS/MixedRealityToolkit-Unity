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

        public void Init(IEnumerable<Vector3> targets)
        {
            Targets = targets.ToArray();
            Weights = Enumerable.Repeat(1.0f, Targets.Length).ToArray();
        }
    }
}