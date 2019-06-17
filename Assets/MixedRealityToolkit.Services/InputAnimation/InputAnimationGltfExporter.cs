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

        private struct AnimationContext
        {
            public BinaryWriter writer;
            public List<GltfAccessor> accessors;
            public List<GltfAnimationChannel> channels;
            public List<GltfAnimationSampler> samplers;
        }

        private static void CreateAnimationBuffer(InputAnimation animation, GltfObject gltfObject)
        {
            int totalSize = GetTotalAnimationBufferSize(animation);

            byte[] bufferData = new byte[totalSize];
            var stream = new MemoryStream(bufferData, true);
            var context = new AnimationContext();
            context.writer = new BinaryWriter(stream);
            context.accessors = new List<GltfAccessor>();
            context.channels = new List<GltfAnimationChannel>();
            context.samplers = new List<GltfAnimationSampler>();
            WriteAnimationBuffer(context, animation);

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
            gltfObject.accessors = context.accessors.ToArray();
        }

        private static void CreateInputAnimation(AnimationContext context, InputAnimation animation)
        {
            CreateNode()
            CreatePoseAnimation(context, animation.CameraCurves, GltfInterpolationType.LINEAR, );

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

        private static void CreatePoseAnimation(AnimationContext context, InputAnimation.PoseCurves poseCurves, GltfInterpolationType interpolation, int node)
        {
            CreateTranslationAnimation(context, poseCurves.PositionX, poseCurves.PositionY, poseCurves.PositionZ, interpolation, node);
            CreateRotationAnimation(context, poseCurves.RotationW, poseCurves.RotationY, poseCurves.RotationZ, poseCurves.RotationW, interpolation, node);
        }

        private static void CreateWeightsAnimation(AnimationContext context, AnimationCurve curve, GltfInterpolationType interpolation, int node)
        {
            int start = (int)context.writer.BaseStream.Position;
            int accTime = WriteTimesBuffer(context, curve);
            int accValue = WriteScalarBuffer(context, curve);
            if (accTime < 0 || accValue < 0)
            {
                // Reset writer if failed
                context.writer.Seek(start, SeekOrigin.Begin);
                return;
            }

            var sampler = new GltfAnimationSampler();
            sampler.input = accTime;
            sampler.output = accValue;
            sampler.interpolation = interpolation;
            context.samplers.Add(sampler);

            var channel = new GltfAnimationChannel();
            channel.sampler = context.samplers.Count - 1;
            channel.target = new GltfAnimationChannelTarget();
            channel.target.node = node;
            channel.target.path = GltfAnimationChannelPath.weights;
            context.channels.Add(channel);
        }

        private static void CreateTranslationAnimation(AnimationContext context, AnimationCurve curveX, AnimationCurve curveY, AnimationCurve curveZ, GltfInterpolationType interpolation, int node)
        {
            int start = (int)context.writer.BaseStream.Position;
            int accTime = WriteTimesBuffer(context, curveX);
            int accValue = WriteVec3Buffer(context, curveX, curveY, curveZ);
            if (accTime < 0 || accValue < 0)
            {
                // Reset writer if failed
                context.writer.Seek(start, SeekOrigin.Begin);
                return;
            }

            var sampler = new GltfAnimationSampler();
            sampler.input = accTime;
            sampler.output = accValue;
            sampler.interpolation = interpolation;
            context.samplers.Add(sampler);

            var channel = new GltfAnimationChannel();
            channel.sampler = context.samplers.Count - 1;
            channel.target = new GltfAnimationChannelTarget();
            channel.target.node = node;
            channel.target.path = GltfAnimationChannelPath.translation;
            context.channels.Add(channel);
        }

        private static void CreateRotationAnimation(AnimationContext context, AnimationCurve curveX, AnimationCurve curveY, AnimationCurve curveZ, AnimationCurve curveW, GltfInterpolationType interpolation, int node)
        {
            int start = (int)context.writer.BaseStream.Position;
            int accTime = WriteTimesBuffer(context, curveX);
            int accValue = WriteVec4Buffer(context, curveX, curveY, curveZ, curveW);
            if (accTime < 0 || accValue < 0)
            {
                // Reset writer if failed
                context.writer.Seek(start, SeekOrigin.Begin);
                return;
            }

            var sampler = new GltfAnimationSampler();
            sampler.input = accTime;
            sampler.output = accValue;
            sampler.interpolation = interpolation;
            context.samplers.Add(sampler);

            var channel = new GltfAnimationChannel();
            channel.sampler = context.samplers.Count - 1;
            channel.target = new GltfAnimationChannelTarget();
            channel.target.node = node;
            channel.target.path = GltfAnimationChannelPath.rotation;
            context.channels.Add(channel);
        }

        private static int WriteTimesBuffer(AnimationContext context, AnimationCurve curve)
        {
            var acc = new GltfAccessor();
            acc.bufferView = 0;
            acc.byteOffset = (int)context.writer.BaseStream.Position;
            acc.componentType = GltfComponentType.Float;
            acc.normalized = false;
            acc.count = curve.length;
            acc.type = "SCALAR";

            Keyframe[] keys = curve.keys;
            for (int i = 0; i < keys.Length; ++i)
            {
                context.writer.Write(keys[i].time);
            }

            context.accessors.Add(acc);
            return context.accessors.Count - 1;
        }

        private static int WriteScalarBuffer(AnimationContext context, AnimationCurve curve)
        {
            var acc = new GltfAccessor();
            acc.bufferView = 0;
            acc.byteOffset = (int)context.writer.BaseStream.Position;
            acc.componentType = GltfComponentType.Float;
            acc.normalized = false;
            acc.count = curve.length;
            acc.type = "SCALAR";

            Keyframe[] keys = curve.keys;
            for (int i = 0; i < keys.Length; ++i)
            {
                context.writer.Write(keys[i].value);
            }

            context.accessors.Add(acc);
            return context.accessors.Count - 1;
        }

        private static int WriteVec3Buffer(AnimationContext context, AnimationCurve curveX, AnimationCurve curveY, AnimationCurve curveZ)
        {
            if (!Vec3CurvesMatch(curveX, curveY, curveZ))
            {
                return -1;
            }

            var acc = new GltfAccessor();
            acc.bufferView = 0;
            acc.byteOffset = (int)context.writer.BaseStream.Position;
            acc.componentType = GltfComponentType.Float;
            acc.normalized = false;
            acc.count = curveX.length;
            acc.type = "VEC3";

            Keyframe[] keysX = curveX.keys;
            Keyframe[] keysY = curveY.keys;
            Keyframe[] keysZ = curveZ.keys;
            for (int i = 0; i < keysX.Length; ++i)
            {
                context.writer.Write(keysX[i].value);
                context.writer.Write(keysY[i].value);
                context.writer.Write(keysZ[i].value);
            }

            context.accessors.Add(acc);
            return context.accessors.Count - 1;
        }

        private static int WriteVec4Buffer(AnimationContext context, AnimationCurve curveX, AnimationCurve curveY, AnimationCurve curveZ, AnimationCurve curveW)
        {
            if (!Vec4CurvesMatch(curveX, curveY, curveZ, curveW))
            {
                return -1;
            }

            var acc = new GltfAccessor();
            acc.bufferView = 0;
            acc.byteOffset = (int)context.writer.BaseStream.Position;
            acc.componentType = GltfComponentType.Float;
            acc.normalized = false;
            acc.count = curveX.length;
            acc.type = "VEC4";

            Keyframe[] keysX = curveX.keys;
            Keyframe[] keysY = curveY.keys;
            Keyframe[] keysZ = curveZ.keys;
            Keyframe[] keysW = curveW.keys;
            for (int i = 0; i < keysX.Length; ++i)
            {
                context.writer.Write(keysX[i].value);
                context.writer.Write(keysY[i].value);
                context.writer.Write(keysZ[i].value);
                context.writer.Write(keysW[i].value);
            }

            context.accessors.Add(acc);
            return context.accessors.Count - 1;
        }

        private static int GetTotalAnimationBufferSize(InputAnimation animation)
        {
            int size = 0;

            size += GetPoseCurvesBufferSize(animation.CameraCurves);

            size += GetScalarBufferSize(animation.HandTrackedCurveLeft);
            size += GetScalarBufferSize(animation.HandTrackedCurveRight);
            size += GetScalarBufferSize(animation.HandPinchCurveLeft);
            size += GetScalarBufferSize(animation.HandPinchCurveRight);

            foreach (var joint in TrackedHandJointValues)
            {
                InputAnimation.PoseCurves jointCurves;
                if (animation.TryGetHandJointCurves(Handedness.Left, joint, out jointCurves))
                {
                    size += GetPoseCurvesBufferSize(jointCurves);
                }
                if (animation.TryGetHandJointCurves(Handedness.Right, joint, out jointCurves))
                {
                    size += GetPoseCurvesBufferSize(jointCurves);
                }
            }
            return size;
        }

        private static int GetScalarBufferSize(AnimationCurve curve)
        {
            int length = curve.length;
            // times + 1 component
            return sizeof(float) * 2 * length;
        }

        private static int GetPoseCurvesBufferSize(InputAnimation.PoseCurves poseCurves)
        {
            return GetVec3BufferSize(poseCurves.PositionX, poseCurves.PositionY, poseCurves.PositionZ)
                + GetVec4BufferSize(poseCurves.RotationX, poseCurves.RotationY, poseCurves.RotationZ, poseCurves.RotationW);
        }

        private static int GetVec3BufferSize(AnimationCurve curveX, AnimationCurve curveY, AnimationCurve curveZ)
        {
            if (Vec3CurvesMatch(curveX, curveY, curveZ))
            {
                Debug.LogWarning("Invalid Vector3 animation, component animation curves must have matching length");
                return 0;
            }

            int length = curveX.length;
            // times + 3 components
            return sizeof(float) * 4 * length;
        }

        private static int GetVec4BufferSize(AnimationCurve curveX, AnimationCurve curveY, AnimationCurve curveZ, AnimationCurve curveW)
        {
            if (!Vec4CurvesMatch(curveX, curveY, curveZ, curveW))
            {
                Debug.LogWarning("Invalid Quaternion animation, component animation curves must have matching length");
                return 0;
            }

            int length = curveX.length;
            // times + 4 components
            return sizeof(float) * 5 * length;
        }

        private static bool Vec3CurvesMatch(AnimationCurve curveX, AnimationCurve curveY, AnimationCurve curveZ)
        {
            return curveX.length == curveY.length && curveY.length == curveZ.length;
        }

        private static bool Vec4CurvesMatch(AnimationCurve curveX, AnimationCurve curveY, AnimationCurve curveZ, AnimationCurve curveW)
        {
            return curveX.length == curveY.length && curveY.length == curveZ.length && curveZ.length == curveW.length;
        }
    }
}