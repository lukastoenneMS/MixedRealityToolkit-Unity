// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Utilities.Gltf;
using Microsoft.MixedReality.Toolkit.Utilities.Gltf.Schema;
using Microsoft.MixedReality.Toolkit.Utilities.Gltf.Serialization;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using Utils = Microsoft.MixedReality.Toolkit.Input.InputAnimationGltfUtilities;

namespace Microsoft.MixedReality.Toolkit.Input
{
    public static class InputAnimationGltfUtilities
    {
        public const string SceneName = "Scene";
        public const string CameraName = "Scene";
        public const string AnimationName = "InputAction";
        public const string JointIdName = "JointID";

        public static readonly string[] TrackedHandJointNames = Enum.GetNames(typeof(TrackedHandJoint));
        public static readonly TrackedHandJoint[] TrackedHandJointValues = (TrackedHandJoint[])Enum.GetValues(typeof(TrackedHandJoint));
        private static readonly Dictionary<string, TrackedHandJoint> TrackedHandJointMap = TrackedHandJointValues.ToDictionary(j => Enum.GetName(typeof(TrackedHandJoint), j));

        public static readonly Handedness[] HandednessValues = (Handedness[])Enum.GetValues(typeof(Handedness));
        private static readonly Dictionary<string, Handedness> HandednessMap = HandednessValues.ToDictionary(h => Enum.GetName(typeof(Handedness), h));

        public static string GetHandNodeName(Handedness handedness)
        {
            return $"Hand.{handedness}";
        }

        public static string GetTrackingNodeName(Handedness handedness)
        {
            return $"Hand.{handedness}.Tracking";
        }

        public static bool TryParseTrackingNodeName(string name, out Handedness handedness)
        {
            string[] parts = name.Split('.');
            if (parts.Length == 3 && parts[0] == "Hand" && parts[2] == "Tracking")
            {
                if (HandednessMap.TryGetValue(parts[1], out handedness))
                {
                    return true;
                }
            }
            handedness = Handedness.None;
            return false;
        }

        public static string GetPinchingNodeName(Handedness handedness)
        {
            return $"Hand.{handedness}.Pinching";
        }

        public static bool TryParsePinchingNodeName(string name, out Handedness handedness)
        {
            string[] parts = name.Split('.');
            if (parts.Length == 3 && parts[0] == "Hand" && parts[2] == "Pinching")
            {
                if (HandednessMap.TryGetValue(parts[1], out handedness))
                {
                    return true;
                }
            }
            handedness = Handedness.None;
            return false;
        }

        public static string GetHandJointsNodeName(Handedness handedness)
        {
            return $"Hand.{handedness}.Joints";
        }

        public static string GetJointNodeName(Handedness handedness, TrackedHandJoint joint)
        {
            return $"Hand.{handedness}.Joint.{joint}";
        }

        public static bool TryParseJointNodeName(string name, out Handedness handedness, out TrackedHandJoint joint)
        {
            string[] parts = name.Split('.');
            if (parts.Length == 4 && parts[0] == "Hand" && parts[2] == "Joint")
            {
                if (HandednessMap.TryGetValue(parts[1], out handedness) && TrackedHandJointMap.TryGetValue(parts[3], out joint))
                {
                    return true;
                }
            }
            handedness = Handedness.None;
            joint = TrackedHandJoint.None;
            return false;
        }
    }

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
            // TODO how to specify copyright and generator strings?
            using (var builder = new GltfObjectBuilder("", "MRTK"))
            {
                int scene = builder.CreateScene(Utils.SceneName, true);

                int camera;
                if (CameraCache.Main)
                {
                    var cameraData = CameraCache.Main;
                    camera = builder.CreateCameraPerspective(Utils.CameraName, cameraData.aspect, cameraData.fieldOfView, cameraData.nearClipPlane, cameraData.farClipPlane);
                }
                else
                {
                    camera = builder.CreateCameraPerspective(Utils.CameraName, 4.0/3.0, 55.0, 0.1, 100.0);
                }

                CreateAnimation(builder, input, camera, out var nodeJointIds);

                exportedObject = builder.Build();

                // Add joint IDs as extras to nodes for identifying them
                foreach (var item in nodeJointIds)
                {
                    exportedObject.nodes[item.Key].extras.Add(Utils.JointIdName, item.Value);
                }
            }

            await GltfUtility.ExportGltfObjectToPathAsync(exportedObject, path);
        }

        /// Create an animation from input data and return its index.
        private static int CreateAnimation(GltfObjectBuilder builder, InputAnimation input, int camera, out Dictionary<int, string> nodeJointIds)
        {
            nodeJointIds = new Dictionary<int, string>();

            using (var animBuilder = new GltfAnimationBuilder(builder, Utils.AnimationName))
            {
                int cameraNode = builder.CreateRootNode(Utils.CameraName, Vector3.zero, Quaternion.identity, Vector3.one, 0, camera);
                CreatePoseAnimation(animBuilder, input.CameraCurves, GltfInterpolationType.LINEAR, cameraNode);

                int leftHandNode = builder.CreateRootNode(Utils.GetHandNodeName(Handedness.Left), Vector3.zero, Quaternion.identity, Vector3.one, 0);
                int rightHandNode = builder.CreateRootNode(Utils.GetHandNodeName(Handedness.Right), Vector3.zero, Quaternion.identity, Vector3.one, 0);

                int leftTrackingNode = builder.CreateChildNode(Utils.GetTrackingNodeName(Handedness.Left), Vector3.zero, Quaternion.identity, Vector3.one, leftHandNode);
                int rightTrackingNode = builder.CreateChildNode(Utils.GetTrackingNodeName(Handedness.Right), Vector3.zero, Quaternion.identity, Vector3.one, rightHandNode);
                CreateBoolAnimation(animBuilder, input.HandTrackedCurveLeft, leftTrackingNode);
                CreateBoolAnimation(animBuilder, input.HandTrackedCurveRight, rightTrackingNode);

                int leftPinchingNode = builder.CreateChildNode(Utils.GetPinchingNodeName(Handedness.Left), Vector3.zero, Quaternion.identity, Vector3.one, leftHandNode);
                int rightPinchingNode = builder.CreateChildNode(Utils.GetPinchingNodeName(Handedness.Right), Vector3.zero, Quaternion.identity, Vector3.one, rightHandNode);
                CreateBoolAnimation(animBuilder, input.HandPinchCurveLeft, leftPinchingNode);
                CreateBoolAnimation(animBuilder, input.HandPinchCurveRight, rightPinchingNode);

                int leftJointsNode = builder.CreateChildNode(Utils.GetHandJointsNodeName(Handedness.Left), Vector3.zero, Quaternion.identity, Vector3.one, leftHandNode);
                int rightJointsNode = builder.CreateChildNode(Utils.GetHandJointsNodeName(Handedness.Right), Vector3.zero, Quaternion.identity, Vector3.one, rightHandNode);
                foreach (var joint in Utils.TrackedHandJointValues)
                {
                    string jointName = Utils.TrackedHandJointNames[(int)joint];
    
                    InputAnimation.PoseCurves jointCurves;
                    if (input.TryGetHandJointCurves(Handedness.Left, joint, out jointCurves))
                    {
                        int leftJointNode = builder.CreateChildNode(jointName, Vector3.zero, Quaternion.identity, Vector3.one, leftJointsNode);
                        nodeJointIds.Add(leftJointNode, $"Left.{jointName}");
                        CreatePoseAnimation(animBuilder, jointCurves, GltfInterpolationType.LINEAR, leftJointNode);
                    }
                    if (input.TryGetHandJointCurves(Handedness.Right, joint, out jointCurves))
                    {
                        int rightJointNode = builder.CreateChildNode(jointName, Vector3.zero, Quaternion.identity, Vector3.one, rightJointsNode);
                        nodeJointIds.Add(rightJointNode, $"Right.{jointName}");
                        CreatePoseAnimation(animBuilder, jointCurves, GltfInterpolationType.LINEAR, rightJointNode);
                    }
                }

                return animBuilder.Index;
            }
        }

        private static void CreatePoseAnimation(GltfAnimationBuilder builder, InputAnimation.PoseCurves poseCurves, GltfInterpolationType interpolation, int node)
        {
            var positionCurves = new AnimationCurve[] { poseCurves.PositionX, poseCurves.PositionY, poseCurves.PositionZ };
            var rotationCurves = new AnimationCurve[] { poseCurves.RotationX, poseCurves.RotationY, poseCurves.RotationZ, poseCurves.RotationW };
            builder.CreateTranslationAnimation(positionCurves, interpolation, node);
            builder.CreateRotationAnimation(rotationCurves, interpolation, node);
        }

        // Store a boolean animation as a translation curve.
        // glTF 2.0 does not support custom animation targets other than translation/rotation/scale/weights.
        private static void CreateBoolAnimation(GltfAnimationBuilder builder, AnimationCurve curve, int node)
        {
            var positionCurves = new AnimationCurve[] { curve, null, null };
            builder.CreateTranslationAnimation(positionCurves, GltfInterpolationType.STEP, node);
        }
    }
}