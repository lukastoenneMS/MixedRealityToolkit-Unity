using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Pose = Microsoft.MixedReality.Toolkit.Utilities.MixedRealityPose;

namespace Parsley
{
    [Serializable]
    public class Puzzle : MonoBehaviour
    {
        public GameObject ShardsContainer = null;

        private readonly List<PuzzlePiece> pieces;

        public int pieceCount => pieces.Count;
        public PuzzlePiece GetPiece(int index) { return pieces[index]; }

        private readonly NeighborMap neighborMap = new NeighborMap();

        private static string namePrefix = "Piece ";

        public Puzzle()
        {
            pieces = new List<PuzzlePiece>();
        }

        void Awake()
        {
            if (!ShardsContainer)
            {
                Debug.LogError("Puzzle needs a valid shards container object");
                enabled = false;
                return;
            }

            // Prepare pieces
            Transform[] shards = new Transform[ShardsContainer.transform.childCount];
            for (int i = 0; i < ShardsContainer.transform.childCount; ++i)
            {
                shards[i] = ShardsContainer.transform.GetChild(i);
            }

            pieces.Capacity = shards.Length;
            foreach (var shard in shards)
            {
                shard.gameObject.AddComponent<PuzzleShard>();
                if (shard.gameObject.activeInHierarchy)
                {
                    var pose = new MixedRealityPose(shard.transform.position, shard.transform.rotation);
                    // Piece starts at its goal
                    var goal = pose;
                    CreatePiece($"{namePrefix}{pieces.Count}", shard, shard.parent, pose, goal);
                }
            }
        }

        private PuzzlePiece CreatePiece(string name, Transform shard, Transform parent, MixedRealityPose pose, MixedRealityPose goal)
        {
            return CreatePiece(name, Enumerable.Repeat(shard, 1), parent, pose, goal);
        }

        private PuzzlePiece CreatePiece(string name, IEnumerable<Transform> shards, Transform parent, MixedRealityPose pose, MixedRealityPose goal)
        {
            GameObject pieceOb = new GameObject(name);

            // Transform the piece
            pieceOb.transform.SetParent(parent, false);
            pieceOb.transform.position = pose.Position;
            pieceOb.transform.rotation = pose.Rotation;

            // Set piece as parent of the shards.
            foreach (var shard in shards)
            {
                shard.SetParent(pieceOb.transform, true);
            }

            // Note: parenting must happen before adding components,
            // since they expect a Puzzle ancestor compont on Awake()
            PuzzlePiece piece = pieceOb.AddComponent<PuzzlePiece>();
            pieces.Add(piece);

            piece.Goal = goal;

            return piece;
        }

        public void RemovePiece(PuzzlePiece piece)
        {
            pieces.Remove(piece);
            Destroy(piece.gameObject);
        }

        public IEnumerable<PuzzlePiece> GetNeighbors(IEnumerable<PuzzlePiece> input)
        {
            return neighborMap.GetNeighbors(input);
        }

        public IEnumerable<PuzzlePiece> GetExternalNeighbors(IEnumerable<PuzzlePiece> input)
        {
            return neighborMap.GetExternalNeighbors(input);
        }

        public IEnumerable<PuzzlePiece> GetInternalNeighbors(IEnumerable<PuzzlePiece> input)
        {
            return neighborMap.GetInternalNeighbors(input);
        }

        public IEnumerator DetectNeighborsAsync()
        {
            return neighborMap.DetectNeighborsAsync(pieces);
        }

        public void ScatterPieces()
        {
            var bounds = new Bounds();
            List<MeshFilter> meshes = new List<MeshFilter>();
            for (int i = 0; i < pieces.Count; ++i)
            {
                pieces[i].gameObject.GetComponentsInChildren<MeshFilter>(meshes);
                for (int j = 0; j < meshes.Count; ++j)
                {
                    var pieceBounds = meshes[j].mesh.bounds.Transform(pieces[i].transform.localToWorldMatrix);
                    if (i == 0 && j == 0)
                    {
                        bounds = pieceBounds;
                    }
                    else
                    {
                        bounds.Encapsulate(pieceBounds);
                    }
                }
                meshes.Clear();
            }
            float radius = 2.0f * Mathf.Max(Mathf.Max(bounds.extents.x, bounds.extents.y), bounds.extents.z);

            var rng = new System.Random();
            for (int i = 0; i < pieces.Count; ++i)
            {
                Vector2 p = UnityEngine.Random.insideUnitCircle * radius;
                Vector3 euler = new Vector3(
                    (float)rng.NextDouble() * 2.0f * Mathf.PI,
                    (float)rng.NextDouble() * 2.0f * Mathf.PI,
                    (float)rng.NextDouble() * 2.0f * Mathf.PI);
                pieces[i].transform.position = bounds.center + new Vector3(p.x, 0.0f, p.y);
                pieces[i].transform.rotation = Quaternion.Euler(euler);
            }
        }

        public PuzzlePiece MergePieces(IEnumerable<PuzzlePiece> pieces, AnimationCurve snapAnimation)
        {
            Debug.Assert(pieces.Any());

            IEnumerable<PuzzleShard> allShards = pieces.SelectMany(p => p.gameObject.GetComponentsInChildren<PuzzleShard>());

            Transform parent = pieces.First().transform.parent;
            string name = namePrefix + string.Join("+", pieces.Select(p => p.name.Substring(namePrefix.Length)));

            PuzzleUtils.ComputeAveragePose(allShards.Select(s => s.transform.AsMixedRealityPose()), out Pose pose);
            PuzzleUtils.ComputeAveragePose(allShards.Select(s => s.Goal), out Pose goal);
            PuzzlePiece pn = CreatePiece(name, allShards.Select((shard) => shard.transform), parent, pose, goal);

            foreach (var p in pieces)
            {
                neighborMap.MoveNeighbors(p, pn);
            }
            foreach (var p in pieces)
            {
                RemovePiece(p);
            }

            pn.Snap();

            return pn;
        }

        public MixedRealityPose GetGoalDistance(PuzzlePiece a, PuzzlePiece b, out float linearDistance, out float angularDistance)
        {
            MixedRealityPose poseA = a.transform.AsMixedRealityPose();
            MixedRealityPose poseB = b.transform.AsMixedRealityPose();

            Pose FromPieceAToPieceB = poseB.Inverse().Multiply(b.Goal.Multiply(a.Goal.Inverse().Multiply(poseA)));

            linearDistance = FromPieceAToPieceB.Position.magnitude;
            FromPieceAToPieceB.Rotation.ToAngleAxis(out float angle, out Vector3 axis);
            angularDistance = Mathf.Abs((angle + 180.0f) % 360.0f - 180.0f);

            return FromPieceAToPieceB;
        }
    }
}
