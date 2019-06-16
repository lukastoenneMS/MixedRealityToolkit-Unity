// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Utilities.Gltf.Schema;
using Microsoft.MixedReality.Toolkit.Utilities.Gltf.Serialization;
using System;
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
            exportedObject.animations = new GltfAnimation[0];
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

            CreateAnimationBuffer(animation, exportedObject);

            await GltfUtility.ExportGltfObjectToPathAsync(exportedObject, path);
        }

        private static GltfAssetInfo CreateAssetInfo(string copyright, string generator, string version = "2.0", string minVersion = "2.0")
        {
            GltfAssetInfo info = new GltfAssetInfo();
            info.copyright = copyright;
            info.generator = generator;
            info.version = version;
            info.minVersion = minVersion;
            return info;
        }

        private static GltfScene CreateScene(string name, IEnumerable<int> rootNodeIndices)
        {
            GltfScene scene = new GltfScene();
            scene.name = "Scene";

            scene.nodes = rootNodeIndices.ToArray();

            return scene;
        }

        private static GltfNode CreateNode(string name, MixedRealityPose pose, int numChildren, int cameraIndex = -1)
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

        private static GltfCamera CreateCameraPerspective(string name, double aspectRatio, double yFov, double zNear, double zFar)
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

        private static GltfCamera CreateCameraOrthographic(string name, double xMag, double yMag, double zNear, double zFar)
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

        private static TrackedHandJoint[] TrackedHandJointValues = (TrackedHandJoint[])Enum.GetValues(typeof(TrackedHandJoint));

        private static void CreateAnimationBuffer(InputAnimation animation, GltfObject gltfObject)
        {
            int totalSize = GetTotalAnimationBufferSize(animation);

            byte[] bufferData = new byte[totalSize];
            var stream = new MemoryStream(bufferData, true);
            var writer = new BinaryWriter(stream);
            var accessors = new List<GltfAccessor>();
            WriteAnimationBuffer(writer, accessors, animation);

            var buffer = new GltfBuffer();
            buffer.name = "AnimationData";
            buffer.uri = null; // Stored internally
            buffer.byteLength = totalSize;
            buffer.BufferData = bufferData;

            var bufferView = new GltfBufferView();
            bufferView.name = "BufferView";
            bufferView.buffer = 0;
            bufferView.byteLength = totalSize;
            bufferView.byteOffset = 0;
            bufferView.target = GltfBufferViewTarget.None;

            gltfObject.buffers = new GltfBuffer[1];
            gltfObject.buffers[0] = buffer;
            gltfObject.bufferViews = new GltfBufferView[1];
            gltfObject.bufferViews[0] = bufferView;
            gltfObject.accessors = accessors.ToArray();
        }

        private static void WriteAnimationBuffer(BinaryWriter writer, List<GltfAccessor> accessors, InputAnimation animation)
        {
            WritePoseCurvesBuffer(writer, accessors, animation.CameraCurves);

            WriteCurveBuffer(writer, accessors, animation.HandTrackedCurveLeft);
            WriteCurveBuffer(writer, accessors, animation.HandTrackedCurveRight);
            WriteCurveBuffer(writer, accessors, animation.HandPinchCurveLeft);
            WriteCurveBuffer(writer, accessors, animation.HandPinchCurveRight);

            foreach (var joint in TrackedHandJointValues)
            {
                InputAnimation.PoseCurves jointCurves;
                if (animation.TryGetHandJointCurves(Handedness.Left, joint, out jointCurves))
                {
                    WritePoseCurvesBuffer(writer, accessors, jointCurves);
                }
                if (animation.TryGetHandJointCurves(Handedness.Right, joint, out jointCurves))
                {
                    WritePoseCurvesBuffer(writer, accessors, jointCurves);
                }
            }
        }

        private static void WritePoseCurvesBuffer(BinaryWriter writer, List<GltfAccessor> accessors, InputAnimation.PoseCurves poseCurves)
        {
            WriteCurveBuffer(writer, accessors, poseCurves.PositionX);
        }

        private static void WriteCurveBuffer(BinaryWriter writer, List<GltfAccessor> accessors, AnimationCurve curve)
        {
            WriteTimesBuffer(writer, accessors, curve);
            WriteValuesBuffer(writer, accessors, curve);
        }

        private static void WriteTimesBuffer(BinaryWriter writer, List<GltfAccessor> accessors, AnimationCurve curve)
        {
            var acc = new GltfAccessor();
            acc.bufferView = 0;
            acc.byteOffset = (int)writer.BaseStream.Position;
            acc.componentType = GltfComponentType.Float;
            acc.normalized = false;
            acc.count = curve.length;
            acc.type = "SCALAR";

            Keyframe[] keys = curve.keys;
            for (int i = 0; i < keys.Length; ++i)
            {
                writer.Write(keys[i].time);
            }

            accessors.Add(acc);
        }

        private static void WriteValuesBuffer(BinaryWriter writer, List<GltfAccessor> accessors, AnimationCurve curve)
        {
            var acc = new GltfAccessor();
            acc.bufferView = 0;
            acc.byteOffset = (int)writer.BaseStream.Position;
            acc.componentType = GltfComponentType.Float;
            acc.normalized = false;
            acc.count = curve.length;
            acc.type = "SCALAR";

            Keyframe[] keys = curve.keys;
            for (int i = 0; i < keys.Length; ++i)
            {
                writer.Write(keys[i].value);
            }

            accessors.Add(acc);
        }

        private static int GetTotalAnimationBufferSize(InputAnimation animation)
        {
            int size = 0;

            size += GetTotalPoseCurvesBufferSize(animation.CameraCurves);

            size += GetTotalCurveBufferSize(animation.HandTrackedCurveLeft);
            size += GetTotalCurveBufferSize(animation.HandTrackedCurveRight);
            size += GetTotalCurveBufferSize(animation.HandPinchCurveLeft);
            size += GetTotalCurveBufferSize(animation.HandPinchCurveRight);

            foreach (var joint in TrackedHandJointValues)
            {
                InputAnimation.PoseCurves jointCurves;
                if (animation.TryGetHandJointCurves(Handedness.Left, joint, out jointCurves))
                {
                    size += GetTotalPoseCurvesBufferSize(jointCurves);
                }
                if (animation.TryGetHandJointCurves(Handedness.Right, joint, out jointCurves))
                {
                    size += GetTotalPoseCurvesBufferSize(jointCurves);
                }
            }
            return size;
        }

        private static int GetTotalPoseCurvesBufferSize(InputAnimation.PoseCurves poseCurves)
        {
            int size = 0;
            size += GetTotalCurveBufferSize(poseCurves.PositionX);
            size += GetTotalCurveBufferSize(poseCurves.PositionY);
            size += GetTotalCurveBufferSize(poseCurves.PositionZ);
            size += GetTotalCurveBufferSize(poseCurves.RotationX);
            size += GetTotalCurveBufferSize(poseCurves.RotationY);
            size += GetTotalCurveBufferSize(poseCurves.RotationZ);
            size += GetTotalCurveBufferSize(poseCurves.RotationW);
            return size;
        }

        private static int GetTotalCurveBufferSize(AnimationCurve curve)
        {
            return GetTimesBufferSize(curve) + GetValuesBufferSize(curve);
        }

        private static int GetTimesBufferSize(AnimationCurve curve)
        {
            return sizeof(float) * curve.length;
        }

        private static int GetValuesBufferSize(AnimationCurve curve)
        {
            return sizeof(float) * curve.length;
        }
    }
}