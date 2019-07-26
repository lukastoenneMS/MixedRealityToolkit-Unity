// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;

using Pose = Microsoft.MixedReality.Toolkit.Utilities.MixedRealityPose;

namespace Microsoft.MixedReality.Toolkit.PoseMatching
{
    public class LifeTimer : MonoBehaviour
    {
        public AnimationCurve curve;

        private Renderer theRenderer;
        private MaterialPropertyBlock materialProps;

        private float localTime;

        void Awake()
        {
            theRenderer = GetComponent<Renderer>();
            materialProps = new MaterialPropertyBlock();

            localTime = 0.0f;
        }

        void Update()
        {
            localTime += Time.deltaTime;

            bool hasEnded = true;
            if (curve != null)
            {
                if (localTime > curve.Duration())
                {
                    hasEnded = true;
                }
                else
                {
                    hasEnded = false;
                    float weight = curve.Evaluate(localTime);
                    if (theRenderer)
                    {
                        Color matColor = theRenderer.material.GetColor("_Color");
                        materialProps.SetColor("_Color", matColor * weight);
                        theRenderer.SetPropertyBlock(materialProps);
                    }
                }
            }

            if (hasEnded)
            {
                Destroy(gameObject);
            }
        }
    }
}

