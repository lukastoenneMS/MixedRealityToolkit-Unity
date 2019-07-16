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
        public enum GameState
        {
            Idle,
            Start,
            Counting,
            Comparing,
            Timeout,
            Announce,
            Final,
        }
        public GameState State { get; private set; } = GameState.Idle;

        public int NumberOfRounds = 3;
        public int Round { get; private set; } = 1;

        public float WaitTime = 2.0f;
        public float CountTime = 3.0f;
        public float CompareTime = 0.8f;
        public float TimeoutTime = 2.5f;
        public float AnnounceTime = 6.0f;
        public float FinalTime = 8.0f;

        public float StateTime { get; private set; } = 0.0f;

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

        public PoseAction[] poseActions = new PoseAction[]
        {
            new PoseAction() { id="rock", filename="PoseConfig_Rock.json" },
            new PoseAction() { id="paper", filename="PoseConfig_Paper.json" },
            new PoseAction() { id="scissors", filename="PoseConfig_Scissors.json" },
        };

        public GhostHand GhostHand;
        public Vector3 GhostHandTarget { get; private set; } = Vector3.back;
        public float GhostHandVelocity { get; private set; } = 0.0f;
        public float GhostHandSmoothTime = 0.15f;
        public float GhostHandMaxSpeed = 150.0f;
        private static readonly Vector3 GhostTargetCountLeft = new Vector3(0.3f, 0.8f, -0.5f);
        private static readonly Vector3 GhostTargetCountRight = new Vector3(-0.3f, 0.8f, -0.5f);
        private static readonly Vector3 GhostTargetCountDown = new Vector3(0.0f, -0.2f, -0.5f);

        public const float MinimumError = 0.0001f;
        public float GoodMatchError = 0.01f;
        public float SloppyMatchError = 0.05f;
        private readonly PoseEvaluator evaluator = new PoseEvaluator();
        private float lastMatchTime = 0.0f;

        public AudioSource Voice;
        public GameObject IndicatorPrefab;
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
                    var poseIndicator = GameObject.Instantiate(IndicatorPrefab, new Vector3(x * 0.15f, 0, 0.75f), Quaternion.identity, indicator.transform);
                    poseIndicator.name = action.id;
                }
            }

            materialProps = new MaterialPropertyBlock();

            if (GhostHand)
            {
                if (poseActions[0].isLoaded)
                {
                    GhostHand.SetPose(GetJointsFromPose(poseActions[0].poseConfig));
                }
                GhostHand.ArmDirection = GhostHandTarget;
            }
        }

        void Update()
        {
            StateTime += Time.deltaTime;

            if (!IsTracking)
            {
                TransitionTo(GameState.Idle);
                return;
            }

            switch (State)
            {
                case GameState.Idle:
                    if (IsTracking)
                    {
                        TransitionTo(GameState.Start);
                    }
                    break;

                case GameState.Start:
                    if (StateTime > WaitTime)
                    {
                        TransitionTo(GameState.Counting);
                    }

                    GhostHandTarget = GhostTargetCountRight;
                    break;

                case GameState.Counting:
                    if (StateTime > CountTime)
                    {
                        TransitionTo(GameState.Comparing);
                    }

                    float relTime = StateTime / CountTime * 5.0f;
                    if (relTime < 1.0f)
                    {
                        GhostHandTarget = GhostTargetCountDown;
                    }
                    else if (relTime < 2.0f)
                    {
                        GhostHandTarget = GhostTargetCountLeft;
                    }
                    else if (relTime < 3.0f)
                    {
                        GhostHandTarget = GhostTargetCountDown;
                    }
                    else if (relTime < 4.0f)
                    {
                        GhostHandTarget = GhostTargetCountRight;
                    }
                    else
                    {
                        GhostHandTarget = GhostTargetCountDown;
                    }
                    break;

                case GameState.Comparing:
                    if (StateTime > CompareTime)
                    {
                        TransitionTo(GameState.Timeout);
                    }
                    break;

                case GameState.Timeout:
                    if (StateTime > TimeoutTime)
                    {
                        TransitionTo(GameState.Counting);
                    }
                    break;

                case GameState.Announce:
                    if (StateTime > AnnounceTime)
                    {
                        if (Round < NumberOfRounds)
                        {
                            TransitionTo(GameState.Final);
                        }
                        else
                        {
                            Round += 1;
                            TransitionTo(GameState.Start);
                        }
                    }
                    break;

                case GameState.Final:
                    if (StateTime > FinalTime)
                    {
                        TransitionTo(GameState.Start);
                    }
                    break;
            }

            AnimateGhostHand();
        }

        private bool TransitionTo(GameState newState)
        {
            if (State == newState)
            {
                return false;
            }

            GameState oldState = State;
            State = newState;

            Debug.Log($"Transition from {oldState} to {newState}");

            switch (oldState)
            {
                case GameState.Final:
                    Round = 1;
                    break;
            }

            StateTime = 0.0f;
            switch (newState)
            {
                case GameState.Idle:
                    break;
            }

            return true;
        }

        private void AnimateGhostHand()
        {
            Quaternion.FromToRotation(GhostHand.ArmDirection, GhostHandTarget).ToAngleAxis(out float angle, out Vector3 axis);

            float velocity = GhostHandVelocity;
            angle = Mathf.SmoothDampAngle(angle, 0.0f, ref velocity, GhostHandSmoothTime, GhostHandMaxSpeed);
            GhostHandVelocity = velocity;

            GhostHand.ArmDirection = Quaternion.AngleAxis(-angle, axis) * GhostHandTarget;
        }

        protected override void UpdateHandMatch(Handedness handedness, IDictionary<TrackedHandJoint, Pose> joints)
        {
            Vector3[] points = GetPointsFromJoints(joints);
            float sqrMaxError = GoodMatchError * GoodMatchError;

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

                    materialProps.SetColor("_Color", MSE <= sqrMaxError ? Color.green : Color.red);

                    var renderer = poseIndicator.GetComponentInChildren<Renderer>();
                    if (renderer)
                    {
                        renderer.SetPropertyBlock(materialProps);
                    }
                }
            }
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