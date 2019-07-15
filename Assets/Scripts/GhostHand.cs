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
        private List<Tuple<Joint, GameObject>> fingers;

        const float ArmLength = 0.30f;
        const float WristLength = 0.05f;
        const float ArmRadius = 0.022f;
        const float FingerRadius = 0.007f;

        public void SetArmDirection(Vector3 dir)
        {
            SetLimbDirection(arm, dir);
        }

        public void SetPose(IDictionary<Joint, Vector3> joints)
        {
            Vector3 offset = Vector3.zero;
            offset -= joints[Joint.ThumbMetacarpalJoint];
            offset -= joints[Joint.IndexMetacarpal];
            offset -= joints[Joint.MiddleMetacarpal];
            offset -= joints[Joint.RingMetacarpal];
            offset -= joints[Joint.PinkyMetacarpal];
            offset /= 5;
            offset += Vector3.forward * (ArmLength + WristLength);

            foreach (var item in fingers)
            {
                Joint joint = item.Item1;
                GameObject obj = item.Item2;

                Vector3 fingerPos = joints[joint];
                Vector3 fingerDir = joints[FingerEnd[joint]] - joints[joint];
                fingerPos = arm.transform.TransformPoint(fingerPos + offset);
                fingerDir = arm.transform.TransformVector(fingerDir);

                SetLimbLength(obj, fingerDir.magnitude);
                SetLimbPosition(obj, fingerPos);
                SetLimbDirection(obj, fingerDir);
            }
        }

        void Awake()
        {
            root = new GameObject("GhostHand");

            arm = CreateLimbObject(root, "Arm", ArmRadius, ArmLength);

            fingers = new List<Tuple<Joint, GameObject>>();
            CreateFinger(Joint.ThumbMetacarpalJoint,      "ThumbMetacarpalJoint", FingerRadius, 1);
            CreateFinger(Joint.IndexMetacarpal,           "IndexMetacarpal",      FingerRadius, 1);
            CreateFinger(Joint.MiddleMetacarpal,          "MiddleMetacarpal",     FingerRadius, 1);
            CreateFinger(Joint.RingMetacarpal,            "RingMetacarpal",       FingerRadius, 1);
            CreateFinger(Joint.PinkyMetacarpal,           "PinkyMetacarpal",      FingerRadius, 1);

            CreateFinger(Joint.ThumbProximalJoint,        "ThumbProximalJoint",   FingerRadius, 1);
            CreateFinger(Joint.IndexKnuckle,              "IndexKnuckle",         FingerRadius, 1);
            CreateFinger(Joint.MiddleKnuckle,             "MiddleKnuckle",        FingerRadius, 1);
            CreateFinger(Joint.RingKnuckle,               "RingKnuckle",          FingerRadius, 1);
            CreateFinger(Joint.PinkyKnuckle,              "PinkyKnuckle",         FingerRadius, 1);

            CreateFinger(Joint.IndexMiddleJoint,          "IndexMiddleJoint",     FingerRadius, 1);
            CreateFinger(Joint.MiddleMiddleJoint,         "MiddleMiddleJoint",    FingerRadius, 1);
            CreateFinger(Joint.RingMiddleJoint,           "RingMiddleJoint",      FingerRadius, 1);
            CreateFinger(Joint.PinkyMiddleJoint,          "PinkyMiddleJoint",     FingerRadius, 1);

            CreateFinger(Joint.ThumbDistalJoint,          "ThumbDistalJoint",     FingerRadius, 1);
            CreateFinger(Joint.IndexDistalJoint,          "IndexDistalJoint",     FingerRadius, 1);
            CreateFinger(Joint.MiddleDistalJoint,         "MiddleDistalJoint",    FingerRadius, 1);
            CreateFinger(Joint.RingDistalJoint,           "RingDistalJoint",      FingerRadius, 1);
            CreateFinger(Joint.PinkyDistalJoint,          "PinkyDistalJoint",     FingerRadius, 1);
        }

        private GameObject CreateFinger(Joint joint, string name, float radius, float length)
        {
            GameObject parentObj;
            if (FingerParent.TryGetValue(joint, out Joint parentJoint))
            {
                parentObj = fingers.Find(f => f.Item1 == parentJoint).Item2;
            }
            else
            {
                parentObj = arm;
            }

            GameObject obj = CreateLimbObject(parentObj, name, radius, length);

            fingers.Add(Tuple.Create(joint, obj));

            return obj;
        }

        private static GameObject CreateLimbObject(GameObject parentObj, string name, float radius, float length)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parentObj.transform, false);

            var meshObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(meshObj.GetComponent<Collider>());
            meshObj.transform.localScale = new Vector3(radius * 2.0f, ArmLength * 0.5f, radius * 2.0f);
            meshObj.transform.localPosition = Vector3.forward * length * 0.5f;
            meshObj.transform.localRotation = Quaternion.FromToRotation(new Vector3(0, 1, 0), Vector3.forward);
            meshObj.transform.SetParent(obj.transform, false);

            return obj;
        }

        private static void SetLimbLength(GameObject obj, float length)
        {
            var meshObj = obj.GetComponentInChildren<Renderer>();
            if (meshObj)
            {
                Vector3 curScale = meshObj.transform.localScale;
                meshObj.transform.localScale = new Vector3(curScale.x, length * 0.5f, curScale.z);
                meshObj.transform.localPosition = Vector3.forward * length * 0.5f;
            }
        }

        private static void SetLimbPosition(GameObject obj, Vector3 pos)
        {
            obj.transform.position = pos;
        }

        private static void SetLimbDirection(GameObject obj, Vector3 dir)
        {
            obj.transform.rotation = Quaternion.LookRotation(dir);
        }

        private static Dictionary<Joint, Joint> FingerEnd = new Dictionary<Joint, Joint>()
        {
            { Joint.ThumbMetacarpalJoint,   Joint.ThumbProximalJoint },
            { Joint.IndexMetacarpal,        Joint.IndexKnuckle },
            { Joint.MiddleMetacarpal,       Joint.MiddleKnuckle },
            { Joint.RingMetacarpal,         Joint.RingKnuckle },
            { Joint.PinkyMetacarpal,        Joint.PinkyKnuckle },

            { Joint.ThumbProximalJoint,     Joint.ThumbDistalJoint },
            { Joint.IndexKnuckle,           Joint.IndexMiddleJoint },
            { Joint.MiddleKnuckle,          Joint.MiddleMiddleJoint },
            { Joint.RingKnuckle,            Joint.RingMiddleJoint },
            { Joint.PinkyKnuckle,           Joint.PinkyMiddleJoint },

            { Joint.IndexMiddleJoint,       Joint.IndexDistalJoint },
            { Joint.MiddleMiddleJoint,      Joint.MiddleDistalJoint },
            { Joint.RingMiddleJoint,        Joint.RingDistalJoint },
            { Joint.PinkyMiddleJoint,       Joint.PinkyDistalJoint },

            { Joint.ThumbDistalJoint,       Joint.ThumbTip },
            { Joint.IndexDistalJoint,       Joint.IndexTip },
            { Joint.MiddleDistalJoint,      Joint.MiddleTip },
            { Joint.RingDistalJoint,        Joint.RingTip },
            { Joint.PinkyDistalJoint,       Joint.PinkyTip },
        };

        private static Dictionary<Joint, Joint> FingerParent = new Dictionary<Joint, Joint>()
        {
            { Joint.ThumbProximalJoint,     Joint.ThumbMetacarpalJoint },
            { Joint.IndexKnuckle,           Joint.IndexMetacarpal },
            { Joint.MiddleKnuckle,          Joint.MiddleMetacarpal },
            { Joint.RingKnuckle,            Joint.RingMetacarpal },
            { Joint.PinkyKnuckle,           Joint.PinkyMetacarpal },

            { Joint.IndexMiddleJoint,       Joint.IndexKnuckle },
            { Joint.MiddleMiddleJoint,      Joint.MiddleKnuckle },
            { Joint.RingMiddleJoint,        Joint.RingKnuckle },
            { Joint.PinkyMiddleJoint,       Joint.PinkyKnuckle },

            { Joint.ThumbDistalJoint,       Joint.ThumbProximalJoint },
            { Joint.IndexDistalJoint,       Joint.IndexMiddleJoint },
            { Joint.MiddleDistalJoint,      Joint.MiddleMiddleJoint },
            { Joint.RingDistalJoint,        Joint.RingMiddleJoint },
            { Joint.PinkyDistalJoint,       Joint.PinkyMiddleJoint },

            { Joint.ThumbTip,               Joint.ThumbDistalJoint },
            { Joint.IndexTip,               Joint.IndexDistalJoint },
            { Joint.MiddleTip,              Joint.MiddleDistalJoint },
            { Joint.RingTip,                Joint.RingDistalJoint },
            { Joint.PinkyTip,               Joint.PinkyDistalJoint },
        };
    }
}