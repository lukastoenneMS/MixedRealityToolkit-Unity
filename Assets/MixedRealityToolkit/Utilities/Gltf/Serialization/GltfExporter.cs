// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Utilities.Gltf.Schema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Utilities.Gltf
{
    internal static class GltfBuilderUtils
    {
        public static void ExtendArray<T>(ref T[] data, T item)
        {
            data = data != null ? data.Append(item).ToArray() : new T[] { item };
        }
    }

    /// <summary>
    /// Utility class for constructing a GltfObject.
    /// </summary>
    public class GltfObjectBuilder : IDisposable
    {
        /// Context to hold generated glTF data structs.
        private class Context
        {
            public List<GltfScene> scenes = new List<GltfScene>();
            public int defaultScene = 0;

            public List<GltfNode> nodes = new List<GltfNode>();
            public List<GltfCamera> cameras = new List<GltfCamera>();
            public List<GltfAccessor> accessors = new List<GltfAccessor>();
            public List<AnimationData> animations = new List<AnimationData>();

            public int bufferSize = 0;
        }

        /// GltfAnimation with associated curves for deferred buffer filling
        private struct AnimationData
        {
            public GltfAnimation animation;
            // Array of curves per animation sampler
            public List<AnimationCurve[]> samplerCurves;
        }

        private Context context = new Context();

        public void Dispose()
        {
            context = null;
        }

        /// <summary>
        /// Construct a GltfObject from collected data.
        /// </summary>
        public GltfObject Build()
        {
            GltfObject exportedObject = new GltfObject();

            exportedObject.asset = CreateAssetInfo("MIT", "MRTK");

            exportedObject.scenes = context.scenes.ToArray();
            exportedObject.scene = context.defaultScene;
            exportedObject.nodes = context.nodes.ToArray();
            exportedObject.cameras = context.cameras.ToArray();
            exportedObject.accessors = context.accessors.ToArray();
            exportedObject.animations = context.animations.Select(animData => animData.animation).ToArray();

            // Fill the binary buffer
            exportedObject.buffers = new GltfBuffer[1];
            exportedObject.bufferViews = new GltfBufferView[1];
            FillBuffer(out exportedObject.buffers[0], out exportedObject.bufferViews[0]);

            return exportedObject;
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

        public int CreateScene(string name, bool setAsDefaultScene)
        {
            GltfScene scene = new GltfScene();
            scene.name = "Scene";

            context.scenes.Add(scene);
            int index = context.scenes.Count - 1;

            if (setAsDefaultScene)
            {
                context.defaultScene = index;
            }

            return index;
        }

        public int CreateRootNode(string name, Vector3 position, Quaternion rotation, Vector3 scale, int scene, int camera = -1)
        {
            int index = CreateNode(name, position, rotation, scale, camera);

            GltfScene gltfScene = context.scenes[scene];
            GltfBuilderUtils.ExtendArray(ref gltfScene.nodes, index);

            return index;
        }

        public int CreateChildNode(string name, Vector3 position, Quaternion rotation, Vector3 scale, int parent, int camera = -1)
        {
            int index = CreateNode(name, position, rotation, scale, camera);

            GltfNode gltfParentNode = context.nodes[parent];
            GltfBuilderUtils.ExtendArray(ref gltfParentNode.children, index);

            return index;
        }

        /// Create a node and return its index.
        private int CreateNode(string name, Vector3 position, Quaternion rotation, Vector3 scale, int camera)
        {
            GltfNode node = new GltfNode();
            node.name = name;

            node.useTRS = true;
            node.rotation = new float[4] { rotation.x, rotation.y, rotation.z, rotation.w };
            node.scale = new float[3] { position.x, position.y, position.z };
            node.translation = new float[3] { scale.x, scale.y, scale.z };
            node.matrix = null;

            node.camera = camera;

            context.nodes.Add(node);
            return context.nodes.Count - 1;
        }

        /// Create a perspective camera and return its index.
        public int CreateCameraPerspective(string name, double aspectRatio, double yFov, double zNear, double zFar)
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

        /// Create a orthographic camera and return its index.
        public int CreateCameraOrthographic(string name, double xMag, double yMag, double zNear, double zFar)
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

        internal int AddAnimationData(GltfAnimation animation, List<AnimationCurve[]> samplerCurves)
        {
            var animData = new AnimationData();
            animData.animation = animation;
            animData.samplerCurves = samplerCurves;
            context.animations.Add(animData);
            return context.animations.Count - 1;
        }

        private void FillBuffer(out GltfBuffer buffer, out GltfBufferView bufferView)
        {
            byte[] bufferData = new byte[context.bufferSize];

            foreach (var animData in context.animations)
            {
                Debug.Assert(animData.samplerCurves.Count == animData.animation.samplers.Length);

                for (int i = 0; i < animData.animation.samplers.Length; ++i)
                {
                    var sampler = animData.animation.samplers[i];
                    var input = context.accessors[sampler.input];
                    var output = context.accessors[sampler.output];

                    var curves = animData.samplerCurves[i];

                    WriteTimeBuffer(bufferData, input, curves);
                    WriteValueBuffer(bufferData, output, curves);
                }
            }

            buffer = new GltfBuffer();
            buffer.name = "AnimationData";
            buffer.uri = null; // Stored internally
            buffer.byteLength = context.bufferSize;
            buffer.BufferData = bufferData;

            bufferView = new GltfBufferView();
            bufferView.name = "BufferView";
            bufferView.buffer = 0;
            bufferView.byteLength = context.bufferSize;
            bufferView.byteOffset = 0;
            bufferView.target = GltfBufferViewTarget.None;
        }

        private static int GetAccessorByteSize(GltfAccessor accessor)
        {
            int stride = GetNumComponents(accessor.type) * GetComponentSize(accessor.componentType);
            return stride * accessor.count;
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

        public int CreateAccessor(string accType, GltfComponentType compType, int count)
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
    }

    /// <summary>
    /// Utility class for constructing a GltfAnimation.
    /// </summary>
    public class GltfAnimationBuilder : IDisposable
    {
        public int Index { get; private set; }

        private GltfObjectBuilder objBuilder;
        private GltfAnimation animation;
        private List<AnimationCurve[]> samplerCurves;

        public GltfAnimationBuilder(GltfObjectBuilder objBuilder, string name)
        {
            this.objBuilder = objBuilder;

            animation = new GltfAnimation();
            animation.name = name;
            samplerCurves = new List<AnimationCurve[]>();

            Index = objBuilder.AddAnimationData(animation, samplerCurves);
        }

        public void Dispose()
        {
            objBuilder = null;
            animation = null;
            samplerCurves = null;
        }

        public void CreateWeightsAnimation(AnimationCurve curve, GltfInterpolationType interpolation, int node)
        {
            int accTime = objBuilder.CreateAccessor("SCALAR", GltfComponentType.Float, curve.length);
            int accValue = objBuilder.CreateAccessor("SCALAR", GltfComponentType.Float, curve.length);

            var sampler = new GltfAnimationSampler();
            sampler.input = accTime;
            sampler.output = accValue;
            sampler.interpolation = interpolation;
            GltfBuilderUtils.ExtendArray(ref animation.samplers, sampler);

            var channel = new GltfAnimationChannel();
            channel.sampler = animation.samplers.Length - 1;
            channel.target = new GltfAnimationChannelTarget();
            channel.target.node = node;
            channel.target.path = GltfAnimationChannelPath.weights;
            GltfBuilderUtils.ExtendArray(ref animation.channels, channel);
            samplerCurves.Add(new AnimationCurve[] { curve });
        }

        public void CreateTranslationAnimation(AnimationCurve[] curves, GltfInterpolationType interpolation, int node)
        {
            if (!Vec3CurvesMatch(curves))
            {
                return;
            }

            int accTime = objBuilder.CreateAccessor("SCALAR", GltfComponentType.Float, curves[0].length);
            int accValue = objBuilder.CreateAccessor("VEC3", GltfComponentType.Float, curves[0].length);

            var sampler = new GltfAnimationSampler();
            sampler.input = accTime;
            sampler.output = accValue;
            sampler.interpolation = interpolation;
            GltfBuilderUtils.ExtendArray(ref animation.samplers, sampler);

            var channel = new GltfAnimationChannel();
            channel.sampler = animation.samplers.Length - 1;
            channel.target = new GltfAnimationChannelTarget();
            channel.target.node = node;
            channel.target.path = GltfAnimationChannelPath.translation;
            GltfBuilderUtils.ExtendArray(ref animation.channels, channel);
            samplerCurves.Add(curves);
        }

        public void CreateRotationAnimation(AnimationCurve[] curves, GltfInterpolationType interpolation, int node)
        {
            if (!Vec4CurvesMatch(curves))
            {
                return;
            }

            int accTime = objBuilder.CreateAccessor("SCALAR", GltfComponentType.Float, curves[0].length);
            int accValue = objBuilder.CreateAccessor("VEC4", GltfComponentType.Float, curves[0].length);

            var sampler = new GltfAnimationSampler();
            sampler.input = accTime;
            sampler.output = accValue;
            sampler.interpolation = interpolation;
            GltfBuilderUtils.ExtendArray(ref animation.samplers, sampler);

            var channel = new GltfAnimationChannel();
            channel.sampler = animation.samplers.Length - 1;
            channel.target = new GltfAnimationChannelTarget();
            channel.target.node = node;
            channel.target.path = GltfAnimationChannelPath.rotation;
            GltfBuilderUtils.ExtendArray(ref animation.channels, channel);
            samplerCurves.Add(curves);
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