// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Utilities.Gltf;
using Microsoft.MixedReality.Toolkit.Utilities.Gltf.Schema;
using Microsoft.MixedReality.Toolkit.Utilities.Gltf.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Input
{
    /// <summary>
    /// Utility class for exporting input animation data in glTF format.
    /// </summary>
    /// <remarks>
    /// Input animation curves are converted into animation data. The camera as well as each hand joint included in the
    /// animation is represented as a node in the glTF file.
    /// </remarks>
    public static class InputAnimationGltfExporter
    {
        /// <summary>
        /// Serialize the given input animation and save it at the given path.
        /// </summary>
        public static async void OnExportInputAnimation(InputAnimation input, string path)
        {
            GltfObject exportedObject;
            using (var builder = new GltfObjectBuilder())
            {
                int scene = builder.CreateScene("Scene", true);

                int camera;
                if (CameraCache.Main)
                {
                    var cameraData = CameraCache.Main;
                    camera = builder.CreateCameraPerspective("Camera", cameraData.aspect, cameraData.fieldOfView, cameraData.nearClipPlane, cameraData.farClipPlane);
                }
                else
                {
                    camera = builder.CreateCameraPerspective("Camera", 4.0/3.0, 55.0, 0.1, 100.0);
                }

                CreateAnimation(builder, input, camera);

                exportedObject = builder.Build();
            }

            await GltfUtility.ExportGltfObjectToPathAsync(exportedObject, path);
        }

        private static TrackedHandJoint[] TrackedHandJointValues = (TrackedHandJoint[])Enum.GetValues(typeof(TrackedHandJoint));

        /// Create an animation from input data and return its index.
        private static int CreateAnimation(GltfObjectBuilder builder, InputAnimation input, int camera)
        {
            int index = builder.BeginAnimation();

            int cameraNode = builder.CreateRootNode("Camera", Vector3.zero, Quaternion.identity, Vector3.one, 0, camera);
            CreatePoseAnimation(builder, input.CameraCurves, GltfInterpolationType.LINEAR, cameraNode);

            int leftHandNode = builder.CreateRootNode("Hand.Left", Vector3.zero, Quaternion.identity, Vector3.one, 0);
            int rightHandNode = builder.CreateRootNode("Hand.Right", Vector3.zero, Quaternion.identity, Vector3.one, 0);

            int leftPinchNode = builder.CreateChildNode("Pinch", Vector3.zero, Quaternion.identity, Vector3.one, leftHandNode);
            int rightPinchNode = builder.CreateChildNode("Pinch", Vector3.zero, Quaternion.identity, Vector3.one, rightHandNode);

            // TODO: need morph targets to create weights animations - use fake position instead?
            // CreateWeightsAnimation(context, input.HandTrackedCurveLeft, GltfInterpolationType.STEP, leftHandNode);
            // CreateWeightsAnimation(context, input.HandTrackedCurveRight, GltfInterpolationType.STEP, rightHandNode);
            // CreateWeightsAnimation(context, input.HandPinchCurveLeft, GltfInterpolationType.STEP, leftPinchNode);
            // CreateWeightsAnimation(context, input.HandPinchCurveRight, GltfInterpolationType.STEP, rightPinchNode);

            foreach (var joint in TrackedHandJointValues)
            {
                string jointName = Enum.GetName(typeof(TrackedHandJoint), joint);
                int leftJointNode = builder.CreateChildNode(jointName, Vector3.zero, Quaternion.identity, Vector3.one, leftHandNode);
                int rightJointNode = builder.CreateChildNode(jointName, Vector3.zero, Quaternion.identity, Vector3.one, rightHandNode);

                InputAnimation.PoseCurves jointCurves;
                if (input.TryGetHandJointCurves(Handedness.Left, joint, out jointCurves))
                {
                    CreatePoseAnimation(builder, jointCurves, GltfInterpolationType.LINEAR, leftJointNode);
                }
                if (input.TryGetHandJointCurves(Handedness.Right, joint, out jointCurves))
                {
                    CreatePoseAnimation(builder, jointCurves, GltfInterpolationType.LINEAR, rightJointNode);
                }
            }

            return index;
        }

        private static void CreatePoseAnimation(GltfObjectBuilder builder, InputAnimation.PoseCurves poseCurves, GltfInterpolationType interpolation, int node)
        {
            var positionCurves = new AnimationCurve[] { poseCurves.PositionX, poseCurves.PositionY, poseCurves.PositionZ };
            var rotationCurves = new AnimationCurve[] { poseCurves.RotationX, poseCurves.RotationY, poseCurves.RotationZ, poseCurves.RotationW };
            builder.CreateTranslationAnimation(positionCurves, interpolation, node);
            builder.CreateRotationAnimation(rotationCurves, interpolation, node);
        }
    }
}