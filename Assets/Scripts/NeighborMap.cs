using Microsoft.MixedReality.Toolkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Parsley
{
    using NeighborSet = HashSet<PuzzlePiece>;
    using NeighborDict = Dictionary<PuzzlePiece, HashSet<PuzzlePiece>>;

    public interface IPieceMapping
    {
        int Count { get; }
        PuzzlePiece GetPiece(int index);
        int GetIndex(PuzzlePiece piece);
    }

    public class NeighborMap
    {
        #if UNITY_EDITOR
        const bool DEBUG_VALIDATE = true;
        #else
        const bool DEBUG_VALIDATE = false;
        #endif

        // Neighbor map for connecting pieces
        private readonly NeighborDict neighbors = new NeighborDict();

        public IEnumerator DetectNeighborsAsync(IEnumerable<PuzzlePiece> pieces)
        {
            using (var builder = new NeighborDetection.NeighborMapBuilder())
            {
                foreach (var piece in pieces)
                {
                    builder.Add(piece);
                }

                // Wait for one frame to register collisions
                yield return new WaitForFixedUpdate();

                BuildNeighborMap(builder.NeighborPairs);
            }
        }

        private void BuildNeighborMap(NeighborDetection.NeighborPairSet neighborPairs)
        {
            neighbors.Clear();

            foreach (var pair in neighborPairs)
            {
                InsertOneSided(pair.pieceA, pair.pieceB);
                InsertOneSided(pair.pieceB, pair.pieceA);
            }

            ValidateNeighborMap();
        }

        private void InsertOneSided(PuzzlePiece piece, PuzzlePiece neighbor)
        {
            if (!neighbors.TryGetValue(piece, out NeighborSet neighborSet))
            {
                neighborSet = new NeighborSet();
                neighbors.Add(piece, neighborSet);
            }
            neighborSet.Add(neighbor);
        }

        private void ValidateNeighborMap()
        {
            #if DEBUG_VALIDATE

            // Ensure neighbor map is symmetric
            foreach (var item in neighbors)
            {
                foreach (var neighbor in item.Value)
                {
                    Debug.Assert(neighbors.TryGetValue(neighbor, out NeighborSet neighborNeighbors));
                    Debug.Assert(neighborNeighbors.Contains(item.Key));
                }
            }

            #endif
        }

        public IEnumerable<PuzzlePiece> GetNeighbors(IEnumerable<PuzzlePiece> input)
        {
            var combined = new NeighborSet();
            foreach (var piece in input)
            {
                if (neighbors.TryGetValue(piece, out var neighborSet))
                {
                    combined.Union(neighborSet);
                }
            }
            foreach (var neighbor in combined)
            {
                yield return neighbor;
            }
        }

        public IEnumerable<PuzzlePiece> GetExternalNeighbors(IEnumerable<PuzzlePiece> input)
        {
            var combined = new NeighborSet();
            var inputSet = new NeighborSet();
            foreach (var piece in input)
            {
                inputSet.Add(piece);
                if (neighbors.TryGetValue(piece, out var neighborSet))
                {
                    combined.Union(neighborSet);
                }
            }

            combined.ExceptWith(inputSet);

            foreach (var neighbor in combined)
            {
                yield return neighbor;
            }
        }

        public IEnumerable<PuzzlePiece> GetInternalNeighbors(IEnumerable<PuzzlePiece> input)
        {
            var combined = new NeighborSet();
            var inputSet = new NeighborSet();
            foreach (var piece in input)
            {
                inputSet.Add(piece);
                if (neighbors.TryGetValue(piece, out var neighborSet))
                {
                    combined.Union(neighborSet);
                }
            }

            combined.IntersectWith(inputSet);

            foreach (var neighbor in combined)
            {
                yield return neighbor;
            }
        }

        /// <summary>
        /// Remove piece from the neighbor map.
        /// </summary>
        public void RemovePiece(PuzzlePiece piece)
        {
            neighbors.Remove(piece);

            foreach (var neighborSet in neighbors.Values)
            {
                neighborSet.Remove(piece);
            }
        }

        /// <summary>
        /// All neighbors of oldPiece become neighbors of newPiece.
        /// </summary>
        public void MoveNeighbors(PuzzlePiece oldPiece, PuzzlePiece newPiece)
        {
            neighbors.TryGetValue(newPiece, out var newNeighborSet);
            if (newNeighborSet != null)
            {
                neighbors.Remove(oldPiece);
            }

            if (neighbors.TryGetValue(oldPiece, out var oldNeighborSet))
            {
                oldNeighborSet.Remove(newPiece);
                if (newNeighborSet == null)
                {
                    neighbors.Add(newPiece, oldNeighborSet);
                }
                else
                {
                    newNeighborSet.UnionWith(oldNeighborSet);
                }

                foreach (var neighbor in oldNeighborSet)
                {
                    if (neighbors.TryGetValue(neighbor, out var neighborNeighbors))
                    {
                        neighborNeighbors.Remove(oldPiece);
                        neighborNeighbors.Add(newPiece);
                    }
                }

                neighbors.Remove(oldPiece);
            }
        }
    }
}
