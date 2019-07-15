// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;

using Pose = Microsoft.MixedReality.Toolkit.Utilities.MixedRealityPose;
using Joint = Microsoft.MixedReality.Toolkit.Utilities.TrackedHandJoint;

namespace Microsoft.MixedReality.Toolkit.PoseMatching
{
    public class GhostHand : MonoBehaviour
    {
        private GameObject root;
        private GameObject arm;
        private Dictionary<Joint, GameObject> fingers;
        private Dictionary<Joint, Vector3> fingerDeltas;

        const float ArmLength = 0.4f;
        const float ArmRadius = 0.035f;
        const float FingerRadius = 0.007f;

        public void SetArmDirection(Vector3 dir)
        {
            SetLimbDirection(arm, dir);
        }

        public void SetPose(IDictionary<Joint, Vector3> joints)
        {
            if (fingerDeltas == null)
            {
                fingerDeltas = new Dictionary<Joint, Vector3>();
            }

            fingerDeltas[Joint.ThumbMetacarpalJoint]   = joints[Joint.ThumbProximalJoint] - joints[Joint.ThumbMetacarpalJoint];
            fingerDeltas[Joint.IndexMetacarpal]        = joints[Joint.IndexKnuckle]       - joints[Joint.IndexMetacarpal];
            fingerDeltas[Joint.MiddleMetacarpal]       = joints[Joint.MiddleKnuckle]      - joints[Joint.MiddleMetacarpal];
            fingerDeltas[Joint.RingMetacarpal]         = joints[Joint.RingKnuckle]        - joints[Joint.RingMetacarpal];
            fingerDeltas[Joint.PinkyMetacarpal]        = joints[Joint.PinkyKnuckle]       - joints[Joint.PinkyMetacarpal];

            fingerDeltas[Joint.ThumbProximalJoint]     = joints[Joint.ThumbDistalJoint]   - joints[Joint.ThumbProximalJoint];
            fingerDeltas[Joint.IndexKnuckle]           = joints[Joint.IndexMiddleJoint]   - joints[Joint.IndexKnuckle];
            fingerDeltas[Joint.MiddleKnuckle]          = joints[Joint.MiddleMiddleJoint]  - joints[Joint.MiddleKnuckle];
            fingerDeltas[Joint.RingKnuckle]            = joints[Joint.RingMiddleJoint]    - joints[Joint.RingKnuckle];
            fingerDeltas[Joint.PinkyKnuckle]           = joints[Joint.PinkyMiddleJoint]   - joints[Joint.PinkyKnuckle];

            fingerDeltas[Joint.IndexMiddleJoint]       = joints[Joint.IndexDistalJoint]   - joints[Joint.IndexMiddleJoint];
            fingerDeltas[Joint.MiddleMiddleJoint]      = joints[Joint.MiddleDistalJoint]  - joints[Joint.MiddleMiddleJoint];
            fingerDeltas[Joint.RingMiddleJoint]        = joints[Joint.RingDistalJoint]    - joints[Joint.RingMiddleJoint];
            fingerDeltas[Joint.PinkyMiddleJoint]       = joints[Joint.PinkyDistalJoint]   - joints[Joint.PinkyMiddleJoint];

            fingerDeltas[Joint.ThumbDistalJoint]       = joints[Joint.ThumbTip]           - joints[Joint.ThumbDistalJoint];
            fingerDeltas[Joint.IndexDistalJoint]       = joints[Joint.IndexTip]           - joints[Joint.IndexDistalJoint];
            fingerDeltas[Joint.MiddleDistalJoint]      = joints[Joint.MiddleTip]          - joints[Joint.MiddleDistalJoint];
            fingerDeltas[Joint.RingDistalJoint]        = joints[Joint.RingTip]            - joints[Joint.RingDistalJoint];
            fingerDeltas[Joint.PinkyDistalJoint]       = joints[Joint.PinkyTip]           - joints[Joint.PinkyDistalJoint];

            foreach (var item in fingerDeltas)
            {
                SetLimbLength(fingers[item.Key], item.Value.magnitude);
            }
        }

        void Awake()
        {
            root = new GameObject("GhostHand");

            arm = CreateLimb(root, "Arm", ArmRadius, ArmLength);

            fingers = new Dictionary<Joint, GameObject>();
            fingers.Add(Joint.ThumbMetacarpalJoint,     CreateLimb(arm,                         "ThumbMetacarpalJoint", FingerRadius, 1));
            fingers.Add(Joint.IndexMetacarpal,          CreateLimb(arm,                         "IndexMetacarpal",      FingerRadius, 1));
            fingers.Add(Joint.MiddleMetacarpal,         CreateLimb(arm,                         "MiddleMetacarpal",     FingerRadius, 1));
            fingers.Add(Joint.RingMetacarpal,           CreateLimb(arm,                         "RingMetacarpal",       FingerRadius, 1));
            fingers.Add(Joint.PinkyMetacarpal,          CreateLimb(arm,                         "PinkyMetacarpal",      FingerRadius, 1));

            fingers.Add(Joint.ThumbProximalJoint,       CreateLimb(Joint.ThumbMetacarpalJoint,  "ThumbProximalJoint",   FingerRadius, 1));
            fingers.Add(Joint.IndexKnuckle,             CreateLimb(Joint.IndexMetacarpal,       "IndexKnuckle",         FingerRadius, 1));
            fingers.Add(Joint.MiddleKnuckle,            CreateLimb(Joint.MiddleMetacarpal,      "MiddleKnuckle",        FingerRadius, 1));
            fingers.Add(Joint.RingKnuckle,              CreateLimb(Joint.RingMetacarpal,        "RingKnuckle",          FingerRadius, 1));
            fingers.Add(Joint.PinkyKnuckle,             CreateLimb(Joint.PinkyMetacarpal,       "PinkyKnuckle",         FingerRadius, 1));

            fingers.Add(Joint.IndexMiddleJoint,         CreateLimb(Joint.IndexKnuckle,          "IndexMiddleJoint",     FingerRadius, 1));
            fingers.Add(Joint.MiddleMiddleJoint,        CreateLimb(Joint.MiddleKnuckle,         "MiddleMiddleJoint",    FingerRadius, 1));
            fingers.Add(Joint.RingMiddleJoint,          CreateLimb(Joint.RingKnuckle,           "RingMiddleJoint",      FingerRadius, 1));
            fingers.Add(Joint.PinkyMiddleJoint,         CreateLimb(Joint.PinkyKnuckle,          "PinkyMiddleJoint",     FingerRadius, 1));

            fingers.Add(Joint.ThumbDistalJoint,         CreateLimb(Joint.ThumbProximalJoint,    "ThumbDistalJoint",     FingerRadius, 1));
            fingers.Add(Joint.IndexDistalJoint,         CreateLimb(Joint.IndexMiddleJoint,      "IndexDistalJoint",     FingerRadius, 1));
            fingers.Add(Joint.MiddleDistalJoint,        CreateLimb(Joint.MiddleMiddleJoint,     "MiddleDistalJoint",    FingerRadius, 1));
            fingers.Add(Joint.RingDistalJoint,          CreateLimb(Joint.RingMiddleJoint,       "RingDistalJoint",      FingerRadius, 1));
            fingers.Add(Joint.PinkyDistalJoint,         CreateLimb(Joint.PinkyMiddleJoint,      "PinkyDistalJoint",     FingerRadius, 1));
        }

        private static GameObject CreateLimb(GameObject parent, string name, float radius, float length)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);

            var meshObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(meshObj.GetComponent<Collider>());
            meshObj.transform.localScale = new Vector3(radius * 2.0f, ArmLength * 0.5f, radius * 2.0f);
            meshObj.transform.localPosition = new Vector3(0, 0, length * 0.5f);
            meshObj.transform.localRotation = Quaternion.Euler(90.0f, 0, 0);
            meshObj.transform.SetParent(obj.transform, false);

            return obj;
        }

        private GameObject CreateLimb(Joint fingerParent, string name, float radius, float length)
        {
            return CreateLimb(fingers[fingerParent], name, radius, length);
        }

        private static void SetLimbLength(GameObject obj, float length)
        {
            var meshObj = obj.GetComponentInChildren<Renderer>();
            if (meshObj)
            {
                Vector3 curScale = meshObj.transform.localScale;
                meshObj.transform.localScale = new Vector3(curScale.x, length * 0.5f, curScale.z);
            }
        }

        private static void SetLimbDirection(GameObject obj, Vector3 dir)
        {
            obj.transform.rotation = Quaternion.LookRotation(dir);
        }
    }
}