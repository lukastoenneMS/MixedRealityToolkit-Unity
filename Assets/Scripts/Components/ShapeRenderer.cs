// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Utilities.ShapeMatching;
using System;
using System.Collections.Generic;
using UnityEngine;

using Pose = Microsoft.MixedReality.Toolkit.Utilities.MixedRealityPose;

namespace Microsoft.MixedReality.Toolkit.Examples.Demos.ShapeMatching
{
    [RequireComponent(typeof(MeshFilter))]
    public class ShapeRenderer : MonoBehaviour
    {
        public int RenderResolution = 6;
        public float RenderThickness = 0.003f;

        private MeshFilter meshFilter;

        public void UpdateShapeMesh(Shape shape)
        {
            meshFilter.mesh = new Mesh();

            var lineShape = shape as LineShape;
            if (lineShape != null)
            {
                CurveMeshUtils.GenerateLineShapeMesh(meshFilter.sharedMesh, lineShape, RenderResolution, RenderThickness);
            }
        }

        void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
        }
    }
}

