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
                    // Ensure neighbors don't contain themselves (non-reflexive map)
                    Debug.Assert(neighbor != item.Key,
                        $"Piece {item.Key.name} neighbor map contains itself");

                    // Ensure neighbor map is symmetric
                    Debug.Assert(neighbors.TryGetValue(neighbor, out NeighborSet neighborNeighbors),
                        $"Piece {item.Key.name} neighbor {neighbor.name} does not have neighbors itself");
                    Debug.Assert(neighborNeighbors.Contains(item.Key),
                        $"Piece {item.Key.name} neighbor {neighbor.name} does not contain piece in turn");
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
                    combined.UnionWith(neighborSet);
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
                    combined.UnionWith(neighborSet);
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
                    combined.UnionWith(neighborSet);
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
            neighbors.TryGetValue(oldPiece, out var oldNeighborSet);

            if (oldNeighborSet != null)
            {
                // New piece neighbor set includes old piece neighbor set, without itself
                oldNeighborSet.Remove(newPiece);
                if (newNeighborSet == null)
                {
                    neighbors.Add(newPiece, oldNeighborSet);
                }
                else
                {
                    newNeighborSet.UnionWith(oldNeighborSet);
                }

                // Remove the old neighbor set
                neighbors.Remove(oldPiece);

                // Replace any reference to the old piece by the new piece
                foreach (var item in neighbors)
                {
                    if (item.Value.Remove(oldPiece))
                    {
                        if (item.Key != newPiece)
                        {
                            item.Value.Add(newPiece);
                        }
                    }
                }
            }

            ValidateNeighborMap();
        }
    }
}
