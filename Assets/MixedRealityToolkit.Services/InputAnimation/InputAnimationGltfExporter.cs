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
        private class Context
        {
            public List<GltfNode> nodes;
            public List<GltfCamera> cameras;
            public List<GltfAccessor> accessors;
            public List<GltfAnimation> animations;
            public List<GltfBuffer> buffers;
            public List<GltfBufferView> bufferViews;

            public List<GltfAnimationChannel> animationChannels;
            // Animation curves for each channel
            // for deferred writing of the buffer after all animation channels are created.
            public List<AnimationCurve[]> animationCurves;
            public List<GltfAnimationSampler> animationSamplers;
            public int bufferSize;
        }

        public static async void OnExportInputAnimation(InputAnimation input, string path)
        {
            var context = new Context();
            context.nodes = new List<GltfNode>();
            context.cameras = new List<GltfCamera>();
            context.accessors = new List<GltfAccessor>();
            context.animations = new List<GltfAnimation>();
            context.buffers = new List<GltfBuffer>();
            context.bufferViews = new List<GltfBufferView>();

            context.animationChannels = new List<GltfAnimationChannel>();
            context.animationCurves = new List<AnimationCurve[]>();
            context.animationSamplers = new List<GltfAnimationSampler>();
            context.bufferSize = 0;

            GltfObject exportedObject = new GltfObject();

            exportedObject.asset = CreateAssetInfo("MIT", "MRTK");

            // Create a scene
            exportedObject.scenes = new GltfScene[1];
            exportedObject.scenes[0] = CreateScene("Scene");
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

            CreateAnimation(context, input, camera);

            CreateBuffer(context);

            // Add all nodes to the scene
            exportedObject.scenes[0].nodes = Enumerable.Range(0, context.nodes.Count).ToArray();

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

        private static GltfScene CreateScene(string name)
        {
            GltfScene scene = new GltfScene();
            scene.name = "Scene";

            return scene;
        }

        private static int CreateNode(Context context, string name, MixedRealityPose pose, int parent = -1, int camera = -1)
        {
            GltfNode node = new GltfNode();
            node.name = name;

            node.useTRS = true;
            node.rotation = new float[4] { 0f, 0f, 0f, 1f };
            node.scale = new float[3] { 1f, 1f, 1f };
            node.translation = new float[3] { 0f, 0f, 0f };
            node.matrix = null;

            node.camera = camera;

            context.nodes.Add(node);
            int index = context.nodes.Count - 1;

            if (parent >= 0)
            {
                GltfNode parentNode = context.nodes[parent];
                parentNode.children = parentNode.children != null ? parentNode.children.Append(index).ToArray() : new int[] { index };
            }

            return index;
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

        private static int CreateAnimation(Context context, InputAnimation input, int camera)
        {
            int cameraNode = CreateNode(context, "Camera", MixedRealityPose.ZeroIdentity, -1, camera);
            CreatePoseAnimation(context, input.CameraCurves, GltfInterpolationType.LINEAR, cameraNode);

            int leftHandNode = CreateNode(context, "Hand.Left", MixedRealityPose.ZeroIdentity);
            int rightHandNode = CreateNode(context, "Hand.Right", MixedRealityPose.ZeroIdentity);

            int leftPinchNode = CreateNode(context, "Pinch", MixedRealityPose.ZeroIdentity, leftHandNode);
            int rightPinchNode = CreateNode(context, "Pinch", MixedRealityPose.ZeroIdentity, rightHandNode);

            // TODO: need morph targets to create weights animations - use fake position instead?
            // CreateWeightsAnimation(context, input.HandTrackedCurveLeft, GltfInterpolationType.STEP, leftHandNode);
            // CreateWeightsAnimation(context, input.HandTrackedCurveRight, GltfInterpolationType.STEP, rightHandNode);
            // CreateWeightsAnimation(context, input.HandPinchCurveLeft, GltfInterpolationType.STEP, leftPinchNode);
            // CreateWeightsAnimation(context, input.HandPinchCurveRight, GltfInterpolationType.STEP, rightPinchNode);

            foreach (var joint in TrackedHandJointValues)
            {
                string jointName = Enum.GetName(typeof(TrackedHandJoint), joint);
                int leftJointNode = CreateNode(context, jointName, MixedRealityPose.ZeroIdentity, leftHandNode);
                int rightJointNode = CreateNode(context, jointName, MixedRealityPose.ZeroIdentity, rightHandNode);

                InputAnimation.PoseCurves jointCurves;
                if (input.TryGetHandJointCurves(Handedness.Left, joint, out jointCurves))
                {
                    CreatePoseAnimation(context, jointCurves, GltfInterpolationType.LINEAR, leftJointNode);
                }
                if (input.TryGetHandJointCurves(Handedness.Right, joint, out jointCurves))
                {
                    CreatePoseAnimation(context, jointCurves, GltfInterpolationType.LINEAR, rightJointNode);
                }
            }

            var animation = new GltfAnimation();
            animation.channels = context.animationChannels.ToArray();
            animation.samplers = context.animationSamplers.ToArray();

            context.animations.Add(animation);
            return context.animations.Count - 1;
        }

        private static void CreatePoseAnimation(Context context, InputAnimation.PoseCurves poseCurves, GltfInterpolationType interpolation, int node)
        {
            CreateTranslationAnimation(context, new AnimationCurve[] { poseCurves.PositionX, poseCurves.PositionY, poseCurves.PositionZ }, interpolation, node);
            CreateRotationAnimation(context, new AnimationCurve[] { poseCurves.RotationX, poseCurves.RotationY, poseCurves.RotationZ, poseCurves.RotationW }, interpolation, node);
        }

        private static void CreateWeightsAnimation(Context context, AnimationCurve curve, GltfInterpolationType interpolation, int node)
        {
            int accTime = CreateAccessor(context, "SCALAR", GltfComponentType.Float, curve.length);
            int accValue = CreateAccessor(context, "SCALAR", GltfComponentType.Float, curve.length);

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
            context.animationCurves.Add(new AnimationCurve[] { curve });
        }

        private static void CreateTranslationAnimation(Context context, AnimationCurve[] curves, GltfInterpolationType interpolation, int node)
        {
            if (!Vec3CurvesMatch(curves))
            {
                return;
            }

            int accTime = CreateAccessor(context, "SCALAR", GltfComponentType.Float, curves[0].length);
            int accValue = CreateAccessor(context, "VEC3", GltfComponentType.Float, curves[0].length);

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
            context.animationCurves.Add(curves);
        }

        private static void CreateRotationAnimation(Context context, AnimationCurve[] curves, GltfInterpolationType interpolation, int node)
        {
            if (!Vec4CurvesMatch(curves))
            {
                return;
            }

            int accTime = CreateAccessor(context, "SCALAR", GltfComponentType.Float, curves[0].length);
            int accValue = CreateAccessor(context, "VEC4", GltfComponentType.Float, curves[0].length);

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
            context.animationCurves.Add(curves);
        }

        private static int GetNumComponents(string type)
        {
            switch (type)
            {
                case "SCALAR": return 1;
                case "VEC2": return 2;
                case "VEC3": return 3;
                case "VEC4": return 4;
                case "MAT2": return 4;
                case "MAT3": return 9;
                case "MAT4": return 16;
            }
            return 0;
        }

        private static int GetComponentSize(GltfComponentType type)
        {
            switch (type)
            {
                case GltfComponentType.Byte: return 1;
                case GltfComponentType.UnsignedByte: return 1;
                case GltfComponentType.Short: return 2;
                case GltfComponentType.UnsignedShort: return 2;
                case GltfComponentType.UnsignedInt: return 4;
                case GltfComponentType.Float : return 4;
            }
            return 0;
        }

        private static int CreateAccessor(Context context, string accType, GltfComponentType compType, int count)
        {
            int stride = GetNumComponents(accType) * GetComponentSize(compType);
            // Align to full element size
            int byteOffset = context.bufferSize == 0 ? 0 : context.bufferSize + stride - (context.bufferSize % stride);
            int byteSize = stride * count;
            context.bufferSize = byteOffset + byteSize;

            var acc = new GltfAccessor();
            acc.bufferView = 0;
            acc.byteOffset = byteOffset;
            acc.type = accType;
            acc.componentType = compType;
            acc.normalized = false;
            acc.count = count;

            context.accessors.Add(acc);
            return context.accessors.Count - 1;
        }

        private static int GetAccessorByteSize(GltfAccessor accessor)
        {
            int stride = GetNumComponents(accessor.type) * GetComponentSize(accessor.componentType);
            return stride * accessor.count;
        }

        private static void CreateBuffer(Context context)
        {
            byte[] bufferData = new byte[context.bufferSize];

            Debug.Assert(context.animationChannels.Count == context.animationCurves.Count);
            for (int i = 0; i < context.animationChannels.Count; ++i)
            {
                var channel = context.animationChannels[i];
                var sampler = context.animationSamplers[channel.sampler];
                var input = context.accessors[sampler.input];
                var output = context.accessors[sampler.output];

                var curves = context.animationCurves[i];

                WriteTimeBuffer(bufferData, input, curves);
                WriteValueBuffer(bufferData, output, curves);
            }

            var buffer = new GltfBuffer();
            buffer.name = "AnimationData";
            buffer.uri = null; // Stored internally
            buffer.byteLength = context.bufferSize;
            buffer.BufferData = bufferData;

            var bufferView = new GltfBufferView();
            bufferView.name = "BufferView";
            bufferView.buffer = 0;
            bufferView.byteLength = context.bufferSize;
            bufferView.byteOffset = 0;
            bufferView.target = GltfBufferViewTarget.None;

            context.buffers.Add(buffer);
            context.bufferViews.Add(bufferView);
        }

        private static void WriteTimeBuffer(byte[] bufferData, GltfAccessor accessor, AnimationCurve[] curves)
        {
            int byteSize = GetAccessorByteSize(accessor);
            var stream = new MemoryStream(bufferData, accessor.byteOffset, byteSize, true);
            var writer = new BinaryWriter(stream);

            Debug.Assert(curves[0].keys.Length == accessor.count);

            accessor.min = new double[] { float.MaxValue };
            accessor.max = new double[] { float.MinValue };
            for (int i = 0; i < accessor.count; ++i)
            {
                float time = curves[0].keys[i].time;

                // Slightly expensive ...
                // Debug.Assert(curves.All((curve) => curve.keys[i].time == time));

                writer.Write(time);

                accessor.min[0] = Math.Min(accessor.min[0], time);
                accessor.max[0] = Math.Max(accessor.max[0], time);
            }
        }

        private static void WriteValueBuffer(byte[] bufferData, GltfAccessor accessor, AnimationCurve[] curves)
        {
            int byteSize = GetAccessorByteSize(accessor);
            var stream = new MemoryStream(bufferData, accessor.byteOffset, byteSize, true);
            var writer = new BinaryWriter(stream);

            int numComponents = GetNumComponents(accessor.type);
            Debug.Assert(curves.Length == numComponents);
            Debug.Assert(curves.All((curve) => curve.keys.Length == accessor.count));

            accessor.min = new double[numComponents];
            accessor.max = new double[numComponents];
            for (int c = 0; c < numComponents; ++c)
            {
                accessor.min[c] = float.MaxValue;
                accessor.max[c] = float.MinValue;
            }
            for (int i = 0; i < accessor.count; ++i)
            {
                for (int c = 0; c < numComponents; ++c)
                {
                    float value = curves[c].keys[i].value;

                    writer.Write(value);

                    accessor.min[c] = Math.Min(accessor.min[c], value);
                    accessor.max[c] = Math.Max(accessor.max[c], value);
                }
            }
        }

        private static bool Vec3CurvesMatch(AnimationCurve[] curves)
        {
            Debug.Assert(curves.Length == 3);
            return curves[0].length == curves[1].length && curves[1].length == curves[2].length;
        }

        private static bool Vec4CurvesMatch(AnimationCurve[] curves)
        {
            Debug.Assert(curves.Length == 4);
            return curves[0].length == curves[1].length && curves[1].length == curves[2].length && curves[2].length == curves[3].length;
        }
    }
}