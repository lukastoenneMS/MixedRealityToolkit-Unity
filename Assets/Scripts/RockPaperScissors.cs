// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections;
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
            None,
            Welcome,
            WaitForTracking,
            Start,
            Counting,
            Comparing,
            Timeout,
            Announce,
            Final,
        }
        public GameState State { get; private set; } = GameState.None;
        private IEnumerator StateCoroutine;

        public int NumberOfRounds = 3;
        public int Round { get; private set; } = 1;
        public PoseAction ChosenPose { get; private set; } = null;
        public PoseAction DetectedPose { get; private set; } = null;
        private System.Random rng;

        public float WelcomeTime = 2.0f;
        public AudioClip WelcomeClip;
        public float StartTime = 2.0f;
        public AudioClip StartClip;
        public float CountTime = 3.0f;
        public AudioClip[] CountClips;
        public float CompareTime = 0.8f;
        public float TimeoutTime = 2.5f;
        public AudioClip TimeoutClip;
        public float AnnounceTime = 6.0f;
        public float FinalTime = 8.0f;

        [Serializable]
        public class PoseAction
        {
            [NonSerialized]
            public string id;

            public string filename;
            public AudioClip clip;
            public AudioClip clipMyWin;
            public AudioClip clipYourWin;

            [NonSerialized]
            public PoseConfiguration poseConfig;

            public bool isLoaded => poseConfig != null;
        }

        public PoseAction[] poseActions = new PoseAction[]
        {
            new PoseAction() { id="rock", filename="PoseConfig_Rock.json" },
            new PoseAction() { id="paper", filename="PoseConfig_Paper.json" },
            new PoseAction() { id="scissors", filename="PoseConfig_Scissors.json" },
            new PoseAction() { id="greet", filename="PoseConfig_Greet.json" },
        };
        private PoseAction[] ValidPoses;

        public GhostHand GhostHand;
        public Vector3 GhostHandTarget { get; private set; } = Vector3.back;
        public float GhostHandVelocity { get; private set; } = 0.0f;
        public float GhostHandSmoothTime = 0.15f;
        public float GhostHandMaxSpeed = 150.0f;
        private static readonly Vector3 GhostTargetForward = new Vector3(0.0f, -0.2f, -0.6f);
        private static readonly Vector3 GhostTargetUp = new Vector3(0.0f, 0.8f, -0.1f);
        private static readonly Vector3 GhostTargetWaveLeft = new Vector3(0.3f, 0.8f, -0.1f);
        private static readonly Vector3 GhostTargetWaveRight = new Vector3(-0.3f, 0.8f, -0.1f);
        private static readonly Vector3 GhostTargetCountLeft = new Vector3(0.3f, 0.8f, -0.5f);
        private static readonly Vector3 GhostTargetCountRight = new Vector3(-0.3f, 0.8f, -0.5f);
        private static readonly Vector3 GhostTargetCountDown = new Vector3(0.0f, -0.2f, -0.5f);

        public const float MinimumError = 0.0001f;
        public float GoodMatchError = 0.01f;
        public float SloppyMatchError = 0.05f;
        private readonly PoseEvaluator evaluator = new PoseEvaluator();

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

            ValidPoses = new PoseAction[] { poseActions[0], poseActions[1], poseActions[2] };
            rng = new System.Random();

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

            GhostHandTarget = Vector3.back;
            SnapGhostHand();

            TransitionTo(GameState.Welcome);
        }

        void Update()
        {
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

            if (StateCoroutine != null)
            {
                StopCoroutine(StateCoroutine);
            }

            switch (newState)
            {
                case GameState.Welcome:
                    StateCoroutine = RunGameStateWelcome();
                    break;
                case GameState.WaitForTracking:
                    StateCoroutine = RunGameStateWaitForTracking();
                    break;
                case GameState.Start:
                    StateCoroutine = RunGameStateStart();
                    break;
                case GameState.Counting:
                    StateCoroutine = RunGameStateCounting();
                    break;
                case GameState.Comparing:
                    StateCoroutine = RunGameStateComparing();
                    break;
                case GameState.Timeout:
                    StateCoroutine = RunGameStateTimeout();
                    break;
                case GameState.Announce:
                    StateCoroutine = RunGameStateAnnounce();
                    break;
                case GameState.Final:
                    StateCoroutine = RunGameStateFinal();
                    break;
            }

            StartCoroutine(StateCoroutine);

            return true;
        }

        private IEnumerator RunGameStateWelcome()
        {
            SetGhostPose("greet");
            PlayMessage(WelcomeClip);

            float startTime = Time.time;
            float endTime = startTime + WelcomeTime;
            while (Time.time <= endTime)
            {
                int numWavings = 3;
                float relTime = (Time.time - startTime) / WelcomeTime * numWavings;
                if (relTime % 1.0f < 0.5f)  { GhostHandTarget = GhostTargetWaveRight; }
                else                        { GhostHandTarget = GhostTargetWaveLeft; }
    
                yield return null;
            }

            TransitionTo(GameState.Start);
        }

        private IEnumerator RunGameStateWaitForTracking()
        {
            GhostHandTarget = GhostTargetForward;

            while (!IsTracking)
            {
                yield return null;
            }

            TransitionTo(GameState.Start);
        }

        private IEnumerator RunGameStateStart()
        {
            ChosenPose = null;
            DetectedPose = null;

            SetGhostPose("rock");
            PlayMessage(StartClip);
            GhostHandTarget = GhostTargetCountRight;

            float startTime = Time.time;
            float endTime = startTime + StartTime;
            while (Time.time <= endTime)
            {
                if (!IsTracking)
                {
                    TransitionTo(GameState.WaitForTracking);
                }

                yield return null;
            }

            TransitionTo(GameState.Counting);
        }

        private IEnumerator RunGameStateCounting()
        {
            SetGhostPose("rock");

            float startTime = Time.time;
            float endTime = startTime + CountTime;
            while (Time.time <= endTime)
            {
                if (!IsTracking)
                {
                    TransitionTo(GameState.WaitForTracking);
                }

                float relTime = (Time.time - startTime) / CountTime * 5.0f;
                if (relTime < 1.0f)         { GhostHandTarget = GhostTargetCountDown; }
                else if (relTime < 2.0f)    { GhostHandTarget = GhostTargetCountLeft; }
                else if (relTime < 3.0f)    { GhostHandTarget = GhostTargetCountDown; }
                else if (relTime < 4.0f)    { GhostHandTarget = GhostTargetCountRight; }
                else                        { GhostHandTarget = GhostTargetCountDown; }

                yield return null;
            }

            TransitionTo(GameState.Comparing);
        }

        private IEnumerator RunGameStateComparing()
        {
            ChosenPose = ValidPoses[rng.Next() % 3];
            SetGhostPose(ChosenPose);
            GhostHandTarget = GhostTargetForward;

            float startTime = Time.time;
            float endTime = startTime + CompareTime;
            while (Time.time <= endTime)
            {
                if (!IsTracking)
                {
                    TransitionTo(GameState.WaitForTracking);
                }

                yield return null;
            }

            if (DetectedPose != null)
            {
                TransitionTo(GameState.Announce);
            }
            else
            {
                TransitionTo(GameState.Timeout);
            }
        }

        private IEnumerator RunGameStateTimeout()
        {
            SetGhostPose("greet");
            PlayMessage(TimeoutClip);
            GhostHandTarget = GhostTargetUp;

            float startTime = Time.time;
            float endTime = startTime + TimeoutTime;
            while (Time.time <= endTime)
            {
                yield return null;
            }

            TransitionTo(GameState.Counting);
        }

        private IEnumerator RunGameStateAnnounce()
        {
            SetGhostPose("greet");
            GhostHandTarget = GhostTargetUp;

            float startTime = Time.time;
            float endTime = startTime + AnnounceTime;
            while (Time.time <= endTime)
            {
                yield return null;
            }

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

        private IEnumerator RunGameStateFinal()
        {
            SetGhostPose("greet");
            GhostHandTarget = GhostTargetUp;

            float startTime = Time.time;
            float endTime = startTime + FinalTime;
            while (Time.time <= endTime)
            {
                yield return null;
            }

            TransitionTo(GameState.Start);
        }

        private bool TryFindPoseAction(string id, out PoseAction action)
        {
            action = Array.Find(poseActions, p => p.id == id);
            return action != null;
        }

        private void PlayMessage(AudioClip clip)
        {
            if (Voice)
            {
                Voice.PlayOneShot(clip);
            }
        }

        private bool IsPlayingMessage => Voice ? Voice.isPlaying : false;

        private void SetGhostPose(string id)
        {
            if (TryFindPoseAction(id, out PoseAction action))
            {
                SetGhostPose(action);
            }
        }

        private void SetGhostPose(PoseAction action)
        {
            if (GhostHand)
            {
                if (action != null && action.isLoaded)
                {
                    GhostHand.SetPose(GetJointsFromPose(action.poseConfig));
                }
            }
        }

        private void SnapGhostHand()
        {
            if (GhostHand)
            {
                GhostHand.ArmDirection = GhostHandTarget;
            }
        }

        private void AnimateGhostHand()
        {
            if (GhostHand)
            {
                Quaternion.FromToRotation(GhostHand.ArmDirection, GhostHandTarget).ToAngleAxis(out float angle, out Vector3 axis);

                float velocity = GhostHandVelocity;
                angle = Mathf.SmoothDampAngle(angle, 0.0f, ref velocity, GhostHandSmoothTime, GhostHandMaxSpeed);
                GhostHandVelocity = velocity;

                GhostHand.ArmDirection = Quaternion.AngleAxis(-angle, axis) * GhostHandTarget;
            }
        }

        protected override void UpdateHandMatch(Handedness handedness, IDictionary<TrackedHandJoint, Pose> joints)
        {
            if (State != GameState.Comparing)
            {
                return;
            }
            if (DetectedPose != null)
            {
                return;
            }

            Vector3[] points = GetPointsFromJoints(joints);
            float sqrMaxError = GoodMatchError * GoodMatchError;

            foreach (var action in ValidPoses)
            {
                PoseMatch match = evaluator.EvaluatePose(points, action.poseConfig);
                float MSE = evaluator.ComputeMeanError(points, action.poseConfig, match, true);

                if (MSE <= sqrMaxError)
                {
                    DetectedPose = action;
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