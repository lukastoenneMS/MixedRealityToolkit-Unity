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
    public class SplineCurveRenderer : MonoBehaviour
    {
        public int RenderResolution = 6;
        public float RenderThickness = 0.003f;

        private MeshFilter meshFilter;
        private MaterialPropertyBlock materialProps;

        public void UpdateCurveMesh(SplineCurve curve)
        {
            CurveMeshUtils.GenerateCurveMesh(meshFilter.sharedMesh, curve, RenderResolution, RenderThickness);
        }

        public void ClearCurveMesh()
        {
            meshFilter.sharedMesh.Clear();
        }

        void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            materialProps = new MaterialPropertyBlock();

            if (meshFilter.sharedMesh == null)
            {
                meshFilter.mesh = new Mesh();
            }
        }
    }
}

