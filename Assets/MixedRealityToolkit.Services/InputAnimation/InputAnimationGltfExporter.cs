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
        private struct Context
        {
            public List<GltfNode> nodes;
            public List<GltfCamera> cameras;
            public List<GltfAccessor> accessors;
            public List<GltfAnimation> animations;
            public List<GltfAnimationChannel> animationChannels;
            public List<GltfAnimationSampler> animationSamplers;
            public List<GltfBuffer> buffers;
            public List<GltfBufferView> bufferViews;

            public BinaryWriter writer;
        }

        public static async void OnExportInputAnimation(InputAnimation animation, string path)
        {
            var context = new Context();
            context.nodes = new List<GltfNode>();
            context.cameras = new List<GltfCamera>();
            context.accessors = new List<GltfAccessor>();
            context.animations = new List<GltfAnimation>();
            context.buffers = new List<GltfBuffer>();
            context.bufferViews = new List<GltfBufferView>();

            GltfObject exportedObject = new GltfObject();

            exportedObject.extensionsUsed = null;
            exportedObject.extensionsRequired = null;
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

            exportedObject.cameras = new GltfCamera[1];

            int camera;
            if (CameraCache.Main)
            {
                var cameraData = CameraCache.Main;
                camera = CreateCameraPerspective(context, "Camera", cameraData.aspect, cameraData.fieldOfView, cameraData.nearClipPlane, cameraData.farClipPlane);
            }
            else
            {
                camera = CreateCameraPerspective(context, "Camera", 4.0/3.0, 55.0, 0.1, 100.0);
            }

            CreateAnimationBuffer(context, animation, camera);

            exportedObject.nodes = context.nodes.ToArray();
            exportedObject.cameras = context.cameras.ToArray();
            exportedObject.accessors = context.accessors.ToArray();
            exportedObject.animations = context.animations.ToArray();
            exportedObject.buffers = context.buffers.ToArray();
            exportedObject.bufferViews = context.bufferViews.ToArray();

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

        private static int CreateNode(Context context, string name, MixedRealityPose pose, int numChildren, int camera = -1)
        {
            GltfNode node = new GltfNode();
            node.name = name;

            node.useTRS = true;
            node.rotation = new float[4] { 0f, 0f, 0f, 1f };
            node.scale = new float[3] { 1f, 1f, 1f };
            node.translation = new float[3] { 0f, 0f, 0f };
            node.matrix = null;

            node.camera = camera;
            node.children = new int[0];
            node.weights = new double[0];

            context.nodes.Add(node);
            return context.nodes.Count - 1;
        }

        private static int CreateCameraPerspective(Context context, string name, double aspectRatio, double yFov, double zNear, double zFar)
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

            context.cameras.Add(camera);
            return context.cameras.Count - 1;
        }

        private static int CreateCameraOrthographic(Context context, string name, double xMag, double yMag, double zNear, double zFar)
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

            context.cameras.Add(camera);
            return context.cameras.Count - 1;
        }

        private static TrackedHandJoint[] TrackedHandJointValues = (TrackedHandJoint[])Enum.GetValues(typeof(TrackedHandJoint));

        private static void CreateAnimationBuffer(Context context, InputAnimation input, int camera)
        {
            int totalSize = GetTotalAnimationBufferSize(input);

            byte[] bufferData = new byte[totalSize];
            var stream = new MemoryStream(bufferData, true);
            context.writer = new BinaryWriter(stream);
            CreateAnimation(context, input, camera);

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

            context.buffers.Add(buffer);
            context.bufferViews.Add(bufferView);
        }

        private static int CreateAnimation(Context context, InputAnimation input, int camera)
        {
            context.animationChannels = new List<GltfAnimationChannel>();
            context.animationSamplers = new List<GltfAnimationSampler>();

            int cameraNode = CreateNode(context, "Camera", MixedRealityPose.ZeroIdentity, 0, camera);
            CreatePoseAnimation(context, input.CameraCurves, GltfInterpolationType.LINEAR, cameraNode);

            // WriteCurveBuffer(writer, accessors, animation.HandTrackedCurveLeft);
            // WriteCurveBuffer(writer, accessors, animation.HandTrackedCurveRight);
            // WriteCurveBuffer(writer, accessors, animation.HandPinchCurveLeft);
            // WriteCurveBuffer(writer, accessors, animation.HandPinchCurveRight);

            // foreach (var joint in TrackedHandJointValues)
            // {
            //     InputAnimation.PoseCurves jointCurves;
            //     if (animation.TryGetHandJointCurves(Handedness.Left, joint, out jointCurves))
            //     {
            //         WritePoseCurvesBuffer(writer, accessors, jointCurves);
            //     }
            //     if (animation.TryGetHandJointCurves(Handedness.Right, joint, out jointCurves))
            //     {
            //         WritePoseCurvesBuffer(writer, accessors, jointCurves);
            //     }
            // }

            var animation = new GltfAnimation();
            animation.channels = context.animationChannels.ToArray();
            animation.samplers = context.animationSamplers.ToArray();
            context.animationChannels = null;
            context.animationSamplers = null;

            context.animations.Add(animation);
            return context.animations.Count - 1;
        }

        private static void CreatePoseAnimation(Context context, InputAnimation.PoseCurves poseCurves, GltfInterpolationType interpolation, int node)
        {
            CreateTranslationAnimation(context, poseCurves.PositionX, poseCurves.PositionY, poseCurves.PositionZ, interpolation, node);
            CreateRotationAnimation(context, poseCurves.RotationW, poseCurves.RotationY, poseCurves.RotationZ, poseCurves.RotationW, interpolation, node);
        }

        private static void CreateWeightsAnimation(Context context, AnimationCurve curve, GltfInterpolationType interpolation, int node)
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
            context.animationSamplers.Add(sampler);

            var channel = new GltfAnimationChannel();
            channel.sampler = context.animationSamplers.Count - 1;
            channel.target = new GltfAnimationChannelTarget();
            channel.target.node = node;
            channel.target.path = GltfAnimationChannelPath.weights;
            context.animationChannels.Add(channel);
        }

        private static void CreateTranslationAnimation(Context context, AnimationCurve curveX, AnimationCurve curveY, AnimationCurve curveZ, GltfInterpolationType interpolation, int node)
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
            context.animationSamplers.Add(sampler);

            var channel = new GltfAnimationChannel();
            channel.sampler = context.animationSamplers.Count - 1;
            channel.target = new GltfAnimationChannelTarget();
            channel.target.node = node;
            channel.target.path = GltfAnimationChannelPath.translation;
            context.animationChannels.Add(channel);
        }

        private static void CreateRotationAnimation(Context context, AnimationCurve curveX, AnimationCurve curveY, AnimationCurve curveZ, AnimationCurve curveW, GltfInterpolationType interpolation, int node)
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
            context.animationSamplers.Add(sampler);

            var channel = new GltfAnimationChannel();
            channel.sampler = context.animationSamplers.Count - 1;
            channel.target = new GltfAnimationChannelTarget();
            channel.target.node = node;
            channel.target.path = GltfAnimationChannelPath.rotation;
            context.animationChannels.Add(channel);
        }

        private static int WriteTimesBuffer(Context context, AnimationCurve curve)
        {
            var acc = new GltfAccessor();
            acc.bufferView = 0;
            acc.byteOffset = (int)context.writer.BaseStream.Position;
            acc.componentType = GltfComponentType.Float;
            acc.normalized = false;
            acc.count = curve.length;
            acc.type = "SCALAR";

            acc.min = new double[] { float.MaxValue };
            acc.max = new double[] { float.MinValue };
            Keyframe[] keys = curve.keys;
            for (int i = 0; i < keys.Length; ++i)
            {
                context.writer.Write(keys[i].time);
                acc.min[0] = Math.Min(acc.min[0], keys[i].time);
                acc.max[0] = Math.Max(acc.max[0], keys[i].time);
            }

            context.accessors.Add(acc);
            return context.accessors.Count - 1;
        }

        private static int WriteScalarBuffer(Context context, AnimationCurve curve)
        {
            var acc = new GltfAccessor();
            acc.bufferView = 0;
            acc.byteOffset = (int)context.writer.BaseStream.Position;
            acc.componentType = GltfComponentType.Float;
            acc.normalized = false;
            acc.count = curve.length;
            acc.type = "SCALAR";

            acc.min = new double[] { float.MaxValue };
            acc.max = new double[] { float.MinValue };
            Keyframe[] keys = curve.keys;
            for (int i = 0; i < keys.Length; ++i)
            {
                context.writer.Write(keys[i].value);
                acc.min[0] = Math.Min(acc.min[0], keys[i].value);
                acc.max[0] = Math.Max(acc.max[0], keys[i].value);
            }

            context.accessors.Add(acc);
            return context.accessors.Count - 1;
        }

        private static int WriteVec3Buffer(Context context, AnimationCurve curveX, AnimationCurve curveY, AnimationCurve curveZ)
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

            acc.min = new double[] { float.MaxValue, float.MaxValue, float.MaxValue };
            acc.max = new double[] { float.MinValue, float.MinValue, float.MinValue };
            Keyframe[] keysX = curveX.keys;
            Keyframe[] keysY = curveY.keys;
            Keyframe[] keysZ = curveZ.keys;
            for (int i = 0; i < keysX.Length; ++i)
            {
                context.writer.Write(keysX[i].value);
                context.writer.Write(keysY[i].value);
                context.writer.Write(keysZ[i].value);
                acc.min[0] = Math.Min(acc.min[0], keysX[i].value);
                acc.min[1] = Math.Min(acc.min[1], keysY[i].value);
                acc.min[2] = Math.Min(acc.min[2], keysZ[i].value);
                acc.max[0] = Math.Max(acc.max[0], keysX[i].value);
                acc.max[1] = Math.Max(acc.max[1], keysY[i].value);
                acc.max[2] = Math.Max(acc.max[2], keysZ[i].value);
            }

            context.accessors.Add(acc);
            return context.accessors.Count - 1;
        }

        public enum GltfAccessorType
        {
            SCALAR,
            VEC2,
            VEC3,
            VEC4,
            MAT2,
            MAT3,
            MAT4,
        }

        private static int CreateAccessor(Context context, GltfAccessorType type, int count, double[] min, double[] max)
        {
            var acc = new GltfAccessor();
            acc.bufferView = 0;
            acc.byteOffset = (int)context.writer.BaseStream.Position;
            acc.componentType = GltfComponentType.Float;
            acc.normalized = false;
            acc.count = count;
            acc.type = Enum.GetName(typeof(GltfAccessorType), type);
            acc.min = min;
            acc.max = max;

            context.accessors.Add(acc);
            return context.accessors.Count - 1;
        }

        private static int WriteVec4Buffer(Context context, AnimationCurve curveX, AnimationCurve curveY, AnimationCurve curveZ, AnimationCurve curveW)
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

            acc.min = new double[] { float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue };
            acc.max = new double[] { float.MinValue, float.MinValue, float.MinValue, float.MinValue };
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
                acc.min[0] = Math.Min(acc.min[0], keysX[i].value);
                acc.min[1] = Math.Min(acc.min[1], keysY[i].value);
                acc.min[2] = Math.Min(acc.min[2], keysZ[i].value);
                acc.min[3] = Math.Min(acc.min[3], keysW[i].value);
                acc.max[0] = Math.Max(acc.max[0], keysX[i].value);
                acc.max[1] = Math.Max(acc.max[1], keysY[i].value);
                acc.max[2] = Math.Max(acc.max[2], keysZ[i].value);
                acc.max[3] = Math.Max(acc.max[3], keysW[i].value);
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
            if (!Vec3CurvesMatch(curveX, curveY, curveZ))
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