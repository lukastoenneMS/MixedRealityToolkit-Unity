using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Parsley
{
    [Serializable]
    public class PuzzlePiece : EffectManager
    {
        public MixedRealityPose Goal = MixedRealityPose.ZeroIdentity;

        private Rigidbody body = null;
        public Rigidbody Body => body;

        private PuzzleGame puzzleGame = null;

        private AudioSource snapAudioSource = null;

        void Awake()
        {
            puzzleGame = gameObject.FindAncestorComponent<PuzzleGame>();
            if (puzzleGame == null)
            {
                Debug.LogWarning("PuzzlePiece has no Puzzle parent component");
                gameObject.SetActive(false);
                return;
            }

            body = GetComponent<Rigidbody>();
            if (body == null)
            {
                body = gameObject.AddComponent<Rigidbody>();
            }

            var manip = GetComponent<ManipulationHandler>();
            if (manip == null)
            {
                manip = gameObject.AddComponent<ManipulationHandler>();
                manip.ManipulationType = ManipulationHandler.HandMovementType.OneAndTwoHanded;
                manip.TwoHandedManipulationType = ManipulationHandler.TwoHandedManipulation.MoveRotate;

                manip.OnHoverEntered.AddListener(OnHoverEntered);
                manip.OnHoverExited.AddListener(OnHoverExited);
                manip.OnManipulationStarted.AddListener(OnManipulationStarted);
                manip.OnManipulationEnded.AddListener(OnManipulationEnded);
            }

            snapAudioSource = gameObject.AddComponent<AudioSource>();
            snapAudioSource.playOnAwake = false;
            snapAudioSource.spatialBlend = 1.0f;

            var collSound = gameObject.AddComponent<PlaySoundOnCollision>();
            collSound.Clip = puzzleGame.CollisionAudioClip;
        }

        public void OnHoverEntered(ManipulationEventData evt)
        {
        }

        public void OnHoverExited(ManipulationEventData evt)
        {
        }

        public void OnManipulationStarted(ManipulationEventData evt)
        {
            puzzleGame.StartBuilding(this);
        }

        public void OnManipulationEnded(ManipulationEventData evt)
        {
            // puzzleGame.StopBuilding(this);
        }

        public void Snap(MixedRealityPose goalOffset)
        {
            PuzzleShard[] shards = GetComponentsInChildren<PuzzleShard>();
            foreach (var shard in shards)
            {
                // Define targets to snap shards into place relative to each other
                var target = goalOffset.Multiply(shard.Goal);
                shard.StartEffect(new SnapEffect(puzzleGame.SnapAnimation, shard.transform, target));
            }

            if (puzzleGame.SnapAudioClip)
            {
                StartEffect(new SoundEffect(snapAudioSource, puzzleGame.SnapAudioClip, puzzleGame.SnapAnimation.Duration()));
            }
        }
    }
}
