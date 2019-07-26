// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;

using Pose = Microsoft.MixedReality.Toolkit.Utilities.MixedRealityPose;

namespace Microsoft.MixedReality.Toolkit.PoseMatching
{
    [RequireComponent(typeof(MeshFilter))]
    public class ICPShapeRenderer : MonoBehaviour
    {
        public int RenderResolution = 6;
        public float RenderThickness = 0.003f;

        private MeshFilter meshFilter;
        private MaterialPropertyBlock materialProps;

        public void UpdateShapeMesh(ICPShape shape)
        {
            meshFilter.mesh = new Mesh();

            var lineShape = shape as LineShape;
            if (lineShape != null)
            {
                CurveMeshUtils.GenerateLineShapeMesh(meshFilter.sharedMesh, lineShape, RenderResolution, RenderThickness);
            }
        }

        public void SetColorMix(float value)
        {
            var renderer = GetComponent<MeshRenderer>();
            if (renderer)
            {
                materialProps.SetColor("_Color", Color.green * value + Color.red * (1.0f - value));
                renderer.SetPropertyBlock(materialProps);
            }
        }

        void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            materialProps = new MaterialPropertyBlock();
        }
    }
}

