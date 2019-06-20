using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

        public PuzzlePiece MergePieces(PuzzlePiece pa, PuzzlePiece pb, AnimationCurve snapAnimation)
        {
            if (pa == pb)
            {
                // Nothing to do, can't merge with itself
                return pa;
            }

            PuzzleShard[] shardsA = pa.gameObject.GetComponentsInChildren<PuzzleShard>();
            PuzzleShard[] shardsB = pb.gameObject.GetComponentsInChildren<PuzzleShard>();
            PuzzleShard[] allShards = new PuzzleShard[shardsA.Length + shardsB.Length];
            Array.Copy(shardsA, 0, allShards, 0, shardsA.Length);
            Array.Copy(shardsB, 0, allShards, shardsA.Length, shardsB.Length);

            PuzzleUtils.FindShardCenter(allShards, out MixedRealityPose center, out MixedRealityPose goalCenter);

            Transform parent = pa.transform.parent;
            string name = namePrefix + pa.name.Substring(namePrefix.Length) + "+" + pb.name.Substring(namePrefix.Length);
            PuzzlePiece pn = CreatePiece(name, allShards.Select((shard) => shard.transform), parent, center, goalCenter);

            neighborMap.MoveNeighbors(pa, pn);
            neighborMap.MoveNeighbors(pb, pn);

            RemovePiece(pa);
            RemovePiece(pb);

            pn.Snap();

            return pn;
        }

        public MixedRealityPose GetGoalDistance(PuzzlePiece a, PuzzlePiece b)
        {
            MixedRealityPose poseA = new MixedRealityPose(a.transform.position, a.transform.rotation);
            MixedRealityPose poseB = new MixedRealityPose(b.transform.position, b.transform.rotation);
            MixedRealityPose poseDistance = poseB.Inverse().Multiply(poseA);
            MixedRealityPose invGoalDistance = a.Goal.Inverse().Multiply(b.Goal);
            return invGoalDistance.Multiply(poseDistance);
        }

        public MixedRealityPose GetGoalDistance(int a, int b)
        {
            return GetGoalDistance(pieces[a], pieces[b]);
        }
    }
}
