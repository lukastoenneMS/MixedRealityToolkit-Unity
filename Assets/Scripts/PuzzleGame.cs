﻿using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Parsley
{
    [Serializable]
    public class PuzzleGame : MonoBehaviour
    {
        /// Container prefabs whose child objects will be puzzle pieces
        public GameObject[] PuzzlePrefabs = null;

        public GameObject Menu = null;
        public GameObject Stage = null;
        public GameObject StageGhost = null;

        [Header("Snapping")]
        public float SnappingDistance = 0.2f;
        public float SnappingAngle = 20.0f;
        public AnimationCurve SnapAnimation = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);
        public AudioClip SnapAudioClip = null;

        [Header("Physics")]
        public AudioClip CollisionAudioClip = null;

        [Header("Hints")]
        public AnimationCurve HighlightAnimation = PuzzleUtils.CreateHighlightCurve(2.5f);
        public Color NeighborColor = Color.green;
        public Material GhostMaterial = null;
        public Color GhostColor = new Color(0.0f, 0.0f, 1.0f, 0.3f);

        private Puzzle puzzle = null;
        public Puzzle Puzzle => puzzle;

        // Maximum number of pieces held in the build list
        const int numBuildSlots = 2;
        private readonly List<PuzzlePiece> buildPieces = new List<PuzzlePiece>(numBuildSlots);

        public bool IsLoaded => (puzzle != null);
        public bool IsMenuOpen => Menu.activeSelf;

        private Suspender suspender = null;
        public Suspender Suspender => suspender;

        public enum GameState
        {
            Init = 0,

            Intro,
            StagePlacement,
            PuzzleSelection,
            Build,
        }

        private GameState state = GameState.Init;
        public GameState State => state;

        public void OpenMenu()
        {
            DisableStagePlacement();
            ClosePuzzleSelectionMenu();

            Menu.SetActive(true);
        }

        public void CloseMenu()
        {
            Menu.SetActive(false);

            EnableStagePlacement();
            OpenPuzzleSelectionMenu();
        }

        public void ToggleMenu()
        {
            if (IsMenuOpen)
            {
                CloseMenu();
            }
            else
            {
                OpenMenu();
            }
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void StartStagePlacement()
        {
            CloseMenu();
            TransitionTo(GameState.StagePlacement);
        }

        public void StopStagePlacement()
        {
            if (state != GameState.StagePlacement)
            {
                Debug.LogWarning("StagePlaced called outside of StagePlacement state");
            }

            TransitionTo(IsLoaded ? GameState.Build : GameState.PuzzleSelection);
        }

        public void StartPuzzleSelection()
        {
            CloseMenu();
            TransitionTo(GameState.PuzzleSelection);
        }

        public void PuzzleSelected(int index)
        {
            if (index < 0 || index >= PuzzlePrefabs.Length)
            {
                Debug.LogError("Puzzle index out of range");
                return;
            }

            StartCoroutine(LoadPuzzleAsync(PuzzlePrefabs[index]));
        }

        private void TransitionTo(GameState newState)
        {
            if (newState == state)
            {
                return;
            }

            DisableStagePlacement();
            ClosePuzzleSelectionMenu();

            state = newState;

            EnableStagePlacement();
            OpenPuzzleSelectionMenu();
        }

        private void EnableStagePlacement()
        {
            if (state != GameState.StagePlacement || IsMenuOpen)
            {
                return;
            }

            Stage.SetActive(false);
            StageGhost.transform.position = Stage.transform.position;
            StageGhost.transform.rotation = Stage.transform.rotation;
            StageGhost.SetActive(true);
        }

        private void DisableStagePlacement()
        {
            StageGhost.SetActive(false);
            Stage.transform.position = StageGhost.transform.position;
            Stage.transform.rotation = StageGhost.transform.rotation;
            Stage.SetActive(true);
        }

        private void OpenPuzzleSelectionMenu()
        {
            if (state != GameState.PuzzleSelection || IsMenuOpen)
            {
                return;
            }

            // TODO
            PuzzleSelected(0);
        }

        private void ClosePuzzleSelectionMenu()
        {
        }

        void Awake()
        {
            if (!Menu)
            {
                Debug.LogError("PuzzleGame needs a valid menu object");
                enabled = false;
                return;
            }
            if (!Stage)
            {
                Debug.LogError("PuzzleGame needs a valid stage object");
                enabled = false;
                return;
            }
            if (!StageGhost)
            {
                Debug.LogError("PuzzleGame needs a valid stage ghost object");
                enabled = false;
                return;
            }

            suspender = new Suspender();

            // StartCoroutine(LoadPuzzleAsync());

            // TransitionTo(GameState.Intro);
            TransitionTo(GameState.StagePlacement);
            CloseMenu();
        }

        private IEnumerator LoadPuzzleAsync(GameObject prefab)
        {
            yield return UnloadPuzzleAsync();

            var newPuzzleOb = GameObject.Instantiate(prefab, Stage.transform);
            var newPuzzle = newPuzzleOb.GetComponent<Puzzle>();

            // Allow one frame to detect neighbors through collision
            yield return newPuzzle.DetectNeighborsAsync();

            newPuzzle.ScatterPieces();

            puzzle = newPuzzle;
            Debug.Assert(buildPieces.Count == 0);

            TransitionTo(GameState.Build);
        }

        private IEnumerator UnloadPuzzleAsync()
        {
            if (puzzle)
            {
                buildPieces.Clear();
                Destroy(puzzle.gameObject);
                puzzle = null;
            }
            yield return null;
        }

        void FixedUpdate()
        {
            if (IsLoaded)
            {
                var neighbors = puzzle.GetInternalNeighbors(buildPieces).ToArray();
                PuzzlePiece joinedPiece = null;
                foreach (var neighborPiece in neighbors)
                {
                    if (joinedPiece != null)
                    {
                        var offset = puzzle.GetGoalDistance(joinedPiece, neighborPiece);
                        float distance = offset.Position.magnitude;
                        float angle;
                        Vector3 axis;
                        offset.Rotation.ToAngleAxis(out angle, out axis);
                        if (distance <= SnappingDistance && angle <= SnappingAngle)
                        {
                            buildPieces.Remove(neighborPiece);
                            joinedPiece = puzzle.MergePieces(joinedPiece, neighborPiece, SnapAnimation);
                        }
                    }
                    else
                    {
                        joinedPiece = neighborPiece;
                    }
                }
            }
        }

        public void AutoFinish(float duration)
        {
            if (!IsLoaded)
            {
                return;
            }

            CloseMenu();

            if (puzzle.pieceCount > 0)
            {
                float mergeInterval = duration / puzzle.pieceCount;
                StartCoroutine(AutoFinishAsync(mergeInterval, 83472));
            }
        }

        private IEnumerator AutoFinishAsync(float mergeInterval, int seed)
        {
            System.Random rng = new System.Random(seed);
            while (puzzle.pieceCount > 1)
            {
                int a = rng.Next() % puzzle.pieceCount;
                int b = rng.Next() % puzzle.pieceCount;

                puzzle.MergePieces(puzzle.GetPiece(a), puzzle.GetPiece(b), SnapAnimation);

                yield return new WaitForSeconds(mergeInterval);
            }
        }

        public void HighlightNeighbors()
        {
            if (!IsLoaded)
            {
                return;
            }

            CloseMenu();

            var effect = new RimColorEffect(HighlightAnimation, NeighborColor);

            foreach (var neighborPiece in puzzle.GetNeighbors(buildPieces))
            {
                PuzzleShard[] shards = neighborPiece.gameObject.GetComponentsInChildren<PuzzleShard>();
                foreach (var shard in shards)
                {
                    shard.StartEffect(effect);
                }
            }
        }

        public void StartBuilding(PuzzlePiece piece)
        {
            if (!IsLoaded)
            {
                return;
            }

            // Remove older entries of the same piece, it gets pushed back on top
            bool existedAlready = buildPieces.Remove(piece);

            // Make sure the list does not outgrow the allowed limit
            if (!existedAlready)
            {
                while (buildPieces.Count >= numBuildSlots)
                {
                    StopBuilding(buildPieces[0]);
                }
            }

            buildPieces.Add(piece);
            suspender.Suspend(piece.Body, false);
        }

        public void StopBuilding(PuzzlePiece piece)
        {
            if (!IsLoaded)
            {
                return;
            }

            buildPieces.Remove(piece);
            suspender.Drop(piece.Body);
        }
    }
}
