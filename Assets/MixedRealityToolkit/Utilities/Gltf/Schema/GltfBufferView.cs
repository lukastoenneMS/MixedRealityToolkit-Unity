﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Utilities.Json;
using System;

namespace Microsoft.MixedReality.Toolkit.Utilities.Gltf.Schema
{
    /// <summary>
    /// A view into a buffer generally representing a subset of the buffer.
    /// https://github.com/KhronosGroup/glTF/blob/master/specification/2.0/schema/bufferView.schema.json
    /// </summary>
    [Serializable]
    public class GltfBufferView : GltfChildOfRootProperty
    {
        /// <summary>
        /// The index of the buffer.
        /// </summary>
        public int buffer = -1;

        /// <summary>
        /// The offset into the buffer in bytes.
        /// <minimum>0</minimum>
        /// </summary>
        public int byteOffset = 0;

        /// <summary>
        /// The length of the bufferView in bytes.
        /// <minimum>0</minimum>
        /// </summary>
        public int byteLength = 0;

        /// <summary>
        /// The stride, in bytes, between vertex attributes or other interleavable data.
        /// When this is zero, data is tightly packed.
        /// <minimum>0</minimum>
        /// <maximum>255</maximum>
        /// </summary>
        [JSONInteger(4)]
        public int byteStride = 0;

        /// <summary>
        /// The target that the WebGL buffer should be bound to.
        /// All valid values correspond to WebGL enums.
        /// When this is not provided, the bufferView contains animation or skin data.
        /// </summary>
        [JSONEnum(true, new object[] {GltfBufferViewTarget.None})]
        public GltfBufferViewTarget target = GltfBufferViewTarget.None;

        /// <summary>
        /// https://github.com/KhronosGroup/glTF/blob/master/specification/2.0/schema/buffer.schema.json
        /// </summary>
        public GltfBuffer Buffer { get; internal set; }
    }
}