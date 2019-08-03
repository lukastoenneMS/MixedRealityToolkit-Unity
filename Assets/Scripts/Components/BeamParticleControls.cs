// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

using Pose = Microsoft.MixedReality.Toolkit.Utilities.MixedRealityPose;

namespace Microsoft.MixedReality.Toolkit.Examples.Demos.ShapeMatching
{
    [RequireComponent(typeof(ParticleSystem))]
    public class BeamParticleControls : MonoBehaviour
    {
        ParticleSystem psys;

        [Range(0.0f, 1.0f)]
        public float Strength = 0.0f;

        [Min(0.0f)]
        public float MaxEmission = 100.0f;

        void Awake()
        {
            psys = GetComponent<ParticleSystem>();
        }

        void Update()
        {
            var em = psys.emission;
            em.rateOverTime = Strength * MaxEmission;
        }
    }
}