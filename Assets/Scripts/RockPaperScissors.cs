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

namespace Microsoft.MixedReality.Toolkit.PoseMatching
{
    public class RockPaperScissors : HandTracker
    {
        public const float MinimumError = 0.0001f;
        public float GoodMatchError = 0.01f;
        public float SloppyMatchError = 0.05f;

        public AudioSource Voice;

        public GameObject IndicatorPrefab;

        [Serializable]
        public class PoseAction
        {
            [NonSerialized]
            public string id;

            public string filename;
            public AudioClip clip;

            [NonSerialized]
            public PoseConfiguration poseConfig;

            public bool isLoaded => poseConfig != null;
        }

        private float lastMatchTime = 0.0f;

        public PoseAction[] poseActions = new PoseAction[]
        {
            new PoseAction() { id="rock", filename="PoseConfig_Rock.json" },
            new PoseAction() { id="paper", filename="PoseConfig_Paper.json" },
            new PoseAction() { id="scissors", filename="PoseConfig_Scissors.json" },
        };

        private readonly PoseEvaluator evaluator = new PoseEvaluator();

        private GameObject indicator;
        private MaterialPropertyBlock materialProps;

        void OnValidate()
        {
            if (GoodMatchError <= MinimumError)
            {
                GoodMatchError = MinimumError;
            }
            if (SloppyMatchError <= MinimumError)
            {
                SloppyMatchError = MinimumError;
            }
        }

        void Awake()
        {
            foreach (var action in poseActions)
            {
                LoadPoseAction(action);
                Debug.Assert(action.isLoaded);
            }

            if (IndicatorPrefab)
            {
                indicator = new GameObject("Indicator");
                for (int i = 0; i < poseActions.Length; ++i)
                {
                    var action = poseActions[i];
                    float x = (float)i / (float)(poseActions.Length - 1) * 2.0f - 1.0f;
                    var poseIndicator = GameObject.Instantiate(IndicatorPrefab, new Vector3(x * 0.25f, 0, 0.5f), Quaternion.identity, indicator.transform);
                    poseIndicator.name = action.id;
                }
            }

            materialProps = new MaterialPropertyBlock();
        }

        protected override void UpdateHandMatch(Handedness handedness, IDictionary<TrackedHandJoint, Pose> joints)
        {
            Vector3[] points = GetPointsFromJoints(joints);
            float sqrMaxError = GoodMatchError * GoodMatchError;

            string summary = "";
            for (int i = 0; i < poseActions.Length; ++i)
            {
                var action = poseActions[i];
                PoseMatch match = evaluator.EvaluatePose(points, action.poseConfig);
                float MSE = evaluator.ComputeMeanError(points, action.poseConfig, match, true);

                float time = Time.time;
                if (time > lastMatchTime + 1.2f)
                {
                    if (MSE <= sqrMaxError)
                    {
                        lastMatchTime = time;

                        if (Voice)
                        {
                            Voice.PlayOneShot(action.clip);
                        }
                    }
                }

                if (indicator)
                {
                    var poseIndicator = indicator.transform.Find(action.id).gameObject;
                    float x = Mathf.Sqrt(MSE / sqrMaxError);
                    poseIndicator.transform.localScale = Vector3.one * Sigmoid(1.0f - x);

                    // float a = -Mathf.Log(0.2f);
                    // float s = Mathf.Exp(-x * a);
                    // summary += $"{action.id}: SE={Mathf.Sqrt(MSE)}, x={x}, mix={1.0f-s}\n";
                    // materialProps.SetColor("_Color", Color.red * s + Color.green * (1.0f - s));
                    materialProps.SetColor("_Color", MSE <= sqrMaxError ? Color.green : Color.red);

                    var renderer = poseIndicator.GetComponentInChildren<Renderer>();
                    if (renderer)
                    {
                        renderer.SetPropertyBlock(materialProps);
                    }
                }
            }
            Debug.Log(summary);
        }

        private static float Sigmoid(float x)
        {
            return 1.0f / (1.0f + Mathf.Exp(-x));
        }

        protected override void ClearHandMatch()
        {
        }

        private void LoadPoseAction(PoseAction action)
        {
            string filepath = Path.Combine(Application.streamingAssetsPath, action.filename);
            action.poseConfig = PoseSerializationUtils.Deserialize(filepath, out string[] identifiers);
        }
    }
}