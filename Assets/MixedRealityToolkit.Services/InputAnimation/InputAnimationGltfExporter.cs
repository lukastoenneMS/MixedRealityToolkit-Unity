// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Utilities.Gltf.Schema;
using Microsoft.MixedReality.Toolkit.Utilities.Gltf.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Input
{
    public static class InputAnimationGltfExporter
    {
        public static async void OnExportInputAnimation(InputAnimation animation, string path)
        {
            GltfObject exportedObject = new GltfObject();

            exportedObject.extensionsUsed = null;
            exportedObject.extensionsRequired = null;
            exportedObject.accessors = new GltfAccessor[0];
            exportedObject.animations = new GltfAnimation[0];
            exportedObject.buffers = new GltfBuffer[0];
            exportedObject.bufferViews = new GltfBufferView[0];
            exportedObject.images = new GltfImage[0];
            exportedObject.materials = new GltfMaterial[0];
            exportedObject.meshes = new GltfMesh[0];
            exportedObject.samplers = new GltfSampler[0];
            exportedObject.skins = new GltfSkin[0];
            exportedObject.textures = new GltfTexture[0];

            exportedObject.asset = CreateAssetInfo("MIT", "MRTK");

            // Create a scene
            exportedObject.scenes = new GltfScene[1];
            exportedObject.scenes[0] = CreateScene("Scene", Enumerable.Range(0, 1));
            exportedObject.scene = 0;

            exportedObject.nodes = new GltfNode[1];
            exportedObject.cameras = new GltfCamera[1];

            exportedObject.nodes[0] = CreateNode("Camera", MixedRealityPose.ZeroIdentity, 0, 0);
            if (CameraCache.Main)
            {
                var camera = CameraCache.Main;
                exportedObject.cameras[0] = CreateCameraPerspective("Camera", camera.aspect, camera.fieldOfView, camera.nearClipPlane, camera.farClipPlane);
            }
            else
            {
                exportedObject.cameras[0] = CreateCameraPerspective("Camera", 4.0/3.0, 55.0, 0.1, 100.0);
            }

            await GltfUtility.ExportGltfObjectToPathAsync(exportedObject, path);
        }

        public static GltfAssetInfo CreateAssetInfo(string copyright, string generator, string version = "2.0", string minVersion = "2.0")
        {
            GltfAssetInfo info = new GltfAssetInfo();
            info.copyright = copyright;
            info.generator = generator;
            info.version = version;
            info.minVersion = minVersion;
            return info;
        }

        public static GltfScene CreateScene(string name, IEnumerable<int> rootNodeIndices)
        {
            GltfScene scene = new GltfScene();
            scene.name = "Scene";

            scene.nodes = rootNodeIndices.ToArray();

            return scene;
        }

        public static GltfNode CreateNode(string name, MixedRealityPose pose, int numChildren, int cameraIndex = -1)
        {
            GltfNode node = new GltfNode();
            node.name = name;

            node.useTRS = true;
            node.rotation = new float[4] { 0f, 0f, 0f, 1f };
            node.scale = new float[3] { 1f, 1f, 1f };
            node.translation = new float[3] { 0f, 0f, 0f };
            node.matrix = null;

            node.camera = cameraIndex;
            node.children = new int[0];
            node.weights = new double[0];

            return node;
        }

        public static GltfCamera CreateCameraPerspective(string name, double aspectRatio, double yFov, double zNear, double zFar)
        {
            GltfCamera camera = new GltfCamera();
            camera.name = name;

            camera = new GltfCamera();

            camera.perspective = new GltfCameraPerspective();
            camera.type = GltfCameraType.perspective;
            camera.perspective.aspectRatio = aspectRatio;
            camera.perspective.yfov = yFov;
            camera.perspective.znear = zNear;
            camera.perspective.zfar = zFar;

            camera.orthographic = null;

            return camera;
        }

        public static GltfCamera CreateCameraOrthographic(string name, double xMag, double yMag, double zNear, double zFar)
        {
            GltfCamera camera = new GltfCamera();
            camera.name = name;

            camera = new GltfCamera();

            camera.orthographic = new GltfCameraOrthographic();
            camera.type = GltfCameraType.orthographic;
            camera.orthographic.xmag = xMag;
            camera.orthographic.ymag = yMag;
            camera.orthographic.znear = zNear;
            camera.orthographic.zfar = zFar;

            camera.perspective = null;

            return camera;
        }
    }
}