using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

using Pose = Microsoft.MixedReality.Toolkit.Utilities.MixedRealityPose;

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

        public bool IsLoaded => (puzzle != null);
        public bool IsMenuOpen => Menu.activeSelf;

        private BuildManager buildManager = null;

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

            buildManager = new BuildManager(GhostColor);

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
            Debug.Assert(buildManager.IsEmpty);

            TransitionTo(GameState.Build);
        }

        private IEnumerator UnloadPuzzleAsync()
        {
            if (puzzle)
            {
                buildManager.Clear();
                Destroy(puzzle.gameObject);
                puzzle = null;
            }
            yield return null;
        }

        void FixedUpdate()
        {
            if (IsLoaded)
            {
                var neighbors = puzzle.GetInternalNeighbors(buildManager.BuildPieces).ToArray();

                #if false
                bool hasCentroid = PuzzleUtils.ComputeCentroid(
                    neighbors.Select(p => p.transform.position),
                    out Vector3 centroid);
                bool hasGoalCentroid = PuzzleUtils.ComputeCentroid(
                    neighbors.Select(p => p.Goal.Position),
                    out Vector3 goalCentroid);
                bool hasAvgRotation = PuzzleUtils.ComputeAverageRotation(
                    neighbors.Select(p => p.transform.rotation),
                    out Quaternion avgRotation);
                bool hasAvgGoalRotation = PuzzleUtils.ComputeAverageRotation(
                    neighbors.Select(p => p.Goal.Rotation),
                    out Quaternion avgGoalRotation);
                if (hasCentroid && hasGoalCentroid && hasAvgRotation && hasAvgGoalRotation)
                {
                    Pose centerOffset = new Pose(centroid, avgRotation * Quaternion.Inverse(avgGoalRotation));
                    foreach (var piece in neighbors)
                    {
                        Pose centered = new Pose(-goalCentroid, Quaternion.identity).Multiply(piece.Goal);
                        Pose pose = centerOffset.Multiply(centered);
                        Pose localPose = piece.transform.parent.AsMixedRealityPose().Inverse().Multiply(pose);

                        int effectId = GhostEffectId + piece.GetHashCode();
                        if (!piece.TryGetEffect(effectId, out GhostEffect effect))
                        {
                            var slot = buildPieces.Find(s => s.piece == piece);
                            var color = slotColors[slot.colorIndex];
                            effect = new GhostEffect(piece.gameObject, piece.transform.parent, localPose, HighlightAnimation, GhostMaterial, color);
                            piece.StartEffect(effectId, effect);
                            effect.IsFinite = false;
                        }
                        else
                        {
                            effect.LocalGhostPose = localPose;
                        }
                    }
                }
                #endif

                PuzzlePiece joinedPiece = null;
                foreach (var neighborPiece in neighbors)
                {
                    if (joinedPiece != null)
                    {
                        var offset = puzzle.GetGoalDistance(joinedPiece, neighborPiece, out float linearDistance, out float angularDistance);
                        if (linearDistance <= SnappingDistance && angularDistance <= SnappingAngle)
                        {
                            MergePieces(joinedPiece, neighborPiece);
                        }
                    }
                    else
                    {
                        joinedPiece = neighborPiece;
                    }
                }
            }
        }

        private PuzzlePiece MergePieces(PuzzlePiece a, PuzzlePiece b)
        {
            buildManager.RemoveAll(p => p == a || p == b);
            return puzzle.MergePieces(new PuzzlePiece[] { a, b }, SnapAnimation);
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

                puzzle.MergePieces(new PuzzlePiece[] { puzzle.GetPiece(a), puzzle.GetPiece(b) }, SnapAnimation);

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

            foreach (var neighborPiece in puzzle.GetNeighbors(buildManager.BuildPieces))
            {
                PuzzleShard[] shards = neighborPiece.gameObject.GetComponentsInChildren<PuzzleShard>();
                foreach (var shard in shards)
                {
                    shard.StartEffect(shard.GetHashCode(), effect);
                }
            }
        }

        public void StartBuilding(PuzzlePiece piece)
        {
            if (!IsLoaded)
            {
                return;
            }

            buildManager.Add(piece);
        }

        public void StopBuilding(PuzzlePiece piece)
        {
            if (!IsLoaded)
            {
                return;
            }

            buildManager.Remove(piece);
        }
    }
}
