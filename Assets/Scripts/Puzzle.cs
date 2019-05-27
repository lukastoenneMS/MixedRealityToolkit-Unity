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
    internal class PuzzlePieceMapping : IPieceMapping
    {
        private readonly List<PuzzlePiece> pieces;
        private readonly Dictionary<PuzzlePiece, int> pieceIndices = new Dictionary<PuzzlePiece, int>();

        public PuzzlePieceMapping(List<PuzzlePiece> pieces)
        {
            this.pieces = pieces;
        }

        public int Count => pieces.Count;

        public PuzzlePiece GetPiece(int index)
        {
            return pieces[index];
        }

        public int GetIndex(PuzzlePiece piece)
        {
            if (!pieceIndices.TryGetValue(piece, out int index))
            {
                Debug.LogError($"Piece {piece.gameObject.name} is not part of the puzzle");
                return -1;
            }
            return index;
        }

        public void UpdateIndices()
        {
            pieceIndices.Clear();
            for (int i = 0; i < pieces.Count; ++i)
            {
                pieceIndices.Add(pieces[i], i);
            }
        }
   }

    [Serializable]
    public class Puzzle : MonoBehaviour
    {
        public GameObject ShardsContainer = null;

        private readonly List<PuzzlePiece> pieces;
        private readonly PuzzlePieceMapping mapping;

        public int pieceCount => pieces.Count;
        public PuzzlePiece GetPiece(int index) { return pieces[index]; }

        private readonly NeighborMap neighborMap = new NeighborMap();

        public Puzzle()
        {
            pieces = new List<PuzzlePiece>();
            mapping = new PuzzlePieceMapping(pieces);
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
                    CreatePiece(shard);
                }
            }

            mapping.UpdateIndices();
        }

        private PuzzlePiece CreatePiece(Transform shard)
        {
            GameObject pieceOb = new GameObject("PuzzlePiece");
            // Insert piece as parent of the shard
            // Note: parenting must happen before adding the component,
            // since it expects a Puzzle ancestor compont on Awake()
            pieceOb.transform.SetParent(shard.parent, false);
            shard.SetParent(pieceOb.transform, false);

            PuzzlePiece piece = pieceOb.AddComponent<PuzzlePiece>();
            pieces.Add(piece);
            int index = pieces.Count - 1;

            // Transform the piece, move shard to local origin
            piece.transform.position = shard.position;
            piece.transform.rotation = shard.rotation;
            shard.localPosition = Vector3.zero;
            shard.localRotation = Quaternion.identity;

            // Piece starts at its goal
            piece.Goal = new MixedRealityPose(piece.transform.position, piece.transform.rotation);

            return piece;
        }

        public IEnumerable<PuzzlePiece> GetNeighbors(IEnumerable<PuzzlePiece> input)
        {
            return neighborMap.GetNeighbors(mapping, input);
        }

        public IEnumerable<PuzzlePiece> GetExternalNeighbors(IEnumerable<PuzzlePiece> input)
        {
            return neighborMap.GetExternalNeighbors(mapping, input);
        }

        public IEnumerable<PuzzlePiece> GetInternalNeighbors(IEnumerable<PuzzlePiece> input)
        {
            return neighborMap.GetInternalNeighbors(mapping, input);
        }

        public IEnumerator DetectNeighborsAsync()
        {
            return neighborMap.DetectNeighborsAsync(mapping);
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

        public PuzzlePiece MergePieces(PuzzlePiece a, PuzzlePiece b, AnimationCurve snapAnimation)
        {
            int indexA = mapping.GetIndex(a);
            int indexB = mapping.GetIndex(b);
            int newIndex = MergePieces(indexA, indexB, snapAnimation);
            return mapping.GetPiece(newIndex);
        }

        private int MergePieces(int a, int b, AnimationCurve snapAnimation)
        {
            if (a == b)
            {
                // Nothing to do
                return a;
            }

            var pa = pieces[a];
            var pb = pieces[b];
            PuzzleShard[] shardsA = pa.gameObject.GetComponentsInChildren<PuzzleShard>();
            PuzzleShard[] shardsB = pb.gameObject.GetComponentsInChildren<PuzzleShard>();
            PuzzleShard[] allShards = new PuzzleShard[shardsA.Length + shardsB.Length];
            Array.Copy(shardsA, 0, allShards, 0, shardsA.Length);
            Array.Copy(shardsB, 0, allShards, shardsA.Length, shardsB.Length);

            PuzzleUtils.FindShardGoalPose(allShards, out MixedRealityPose goalOffset, out MixedRealityPose goalCenter);
            pa.Goal = goalCenter;
            // Move shards to A
            foreach (var shard in shardsB)
            {
                shard.transform.SetParent(pa.transform, true);
            }

            // Recenter the piece on the shared center of shards
            MixedRealityPose center = goalOffset.Multiply(goalCenter);
            MixedRealityPose invParentOffset = center.Inverse().Multiply(new MixedRealityPose(pa.transform.position, pa.transform.rotation));
            pa.transform.position = center.Position;
            pa.transform.rotation = center.Rotation;
            foreach (var shard in allShards)
            {
                // Parent has been moved, make sure world transform stays the same
                shard.transform.localPosition = invParentOffset.Multiply(shard.transform.localPosition);
                shard.transform.localRotation = invParentOffset.Multiply(shard.transform.localRotation);
            }

            pa.Snap(goalOffset);

            // Remove the piece
            pieces.RemoveAt(b);
            GameObject.Destroy(pb.gameObject);
            mapping.UpdateIndices();
            int newA = (a < b ? a : a-1);

            // Maps old index to new:
            // < b  : unchanged
            // > b  : shift left 1
            // == b : -1, removed
            Func<int, int> mapIndex = (n) => (n < b ? n : (n > b ? n-1 : -1));
            // Maps old neighbor index to new:
            // < b  : unchanged
            // > b  : shift left 1
            // == b : becomes a
            Func<int, int> mapNeighbor = (n) => (n < b ? n : (n > b ? n-1 : newA));
            // Modify neighbor map
            neighborMap.MapIndices(mapping, mapIndex, mapNeighbor);

            return newA;
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
