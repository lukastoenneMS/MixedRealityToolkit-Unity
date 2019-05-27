using Microsoft.MixedReality.Toolkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Parsley
{
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
        private readonly List<int> neighborCount = new List<int>();
        private readonly List<int> neighborStart = new List<int>();
        private readonly List<int> neighbors = new List<int>();

        public IEnumerator DetectNeighborsAsync(IPieceMapping mapping)
        {
            using (var builder = new NeighborDetection.NeighborMapBuilder())
            {
                for (int i = 0; i < mapping.Count; ++i)
                {
                    builder.Add(mapping.GetPiece(i).gameObject, i);
                }

                // Wait for one frame to register collisions
                yield return new WaitForFixedUpdate();

                BuildNeighborMap(mapping, builder.NeighborSet);
            }
        }

        private void BuildNeighborMap(IPieceMapping mapping, NeighborDetection.NeighborIndexSet neighborSet)
        {
            int count = mapping.Count;
            neighborCount.Clear();
            neighborStart.Clear();
            neighbors.Clear();
            neighborCount.Capacity = count;
            neighborStart.Capacity = count;
            // NeighborSet only has one key per symmetric pair, we want both in the map
            int numNeighbors = neighborSet.Count * 2;
            neighbors.Capacity = numNeighbors;
            for (int i = 0; i < count; ++i)
            {
                neighborStart.Add(0);
                neighborCount.Add(0);
            }
            for (int i = 0; i < numNeighbors; ++i)
            {
                neighbors.Add(-1);
            }

            // Count neighbors
            foreach (NeighborDetection.NeighborIndexPair pair in neighborSet)
            {
                ++neighborCount[pair.indexMinor];
                ++neighborCount[pair.indexMajor];
            }
            UpdateNeighborStart();

            // Actual neighbor indices in the map
            {
                int[] nextNeighbor = new int[count];
                for (int i = 0; i < count; ++i)
                {
                    nextNeighbor[i] = neighborStart[i];
                }
                foreach (NeighborDetection.NeighborIndexPair pair in neighborSet)
                {
                    neighbors[nextNeighbor[pair.indexMinor]++] = pair.indexMajor;
                    neighbors[nextNeighbor[pair.indexMajor]++] = pair.indexMinor;
                }
            }

            // string s = "";
            // foreach (int n in neighbors) { s += $"{n}, "; }
            // Debug.Log($"Neighbor map: {s}");

            ValidateNeighborMap();
        }

        private void UpdateNeighborStart()
        {
            Debug.Assert(neighborStart.Count == neighborCount.Count);

            // Sum for offset in the neighbors map
            int start = 0;
            for (int i = 0; i < neighborCount.Count; ++i)
            {
                neighborStart[i] = start;
                start += neighborCount[i];
            }
        }

        private void ValidateNeighborMap()
        {
            #if DEBUG_VALIDATE

            Debug.Assert(neighborCount.Count == pieces.Count);
            Debug.Assert(neighborStart.Count == pieces.Count);
            if (pieces.Count == 0)
            {
                Debug.Assert(neighbors.Count == 0);
                return;
            }

            int total = 0;
            for (int i = 0; i < pieces.Count; ++i)
            {
                int start = neighborStart[i];
                int count = neighborCount[i];

                Debug.Assert(start == total);
                Debug.Assert(start + count <= neighbors.Count);
                Debug.Assert(total <= neighbors.Count);

                for (int j = 0; j < count; ++j)
                {
                    int other = neighbors[start + j];
                    Debug.Assert(other < pieces.Count);

                    // Ensure neighbor map is symmetric
                    int otherStart = neighborStart[other];
                    int otherCount = neighborCount[other];
                    bool isSymmetric = false;
                    for (int jj = 0; jj < otherCount; ++jj)
                    {
                        if (neighbors[otherStart + jj] == i)
                        {
                            isSymmetric = true;
                        }
                    }
                    Debug.Assert(isSymmetric);
                }

                total += count;
            }
            Debug.Assert(neighbors.Count == total);
            #endif
        }

        public void MapIndices(IPieceMapping mapping, Func<int, int> mapIndex, Func<int, int> mapNeighbor)
        {
            var neighborSet = new NeighborDetection.NeighborIndexSet();
            for (int i = 0; i < neighborStart.Count; ++i)
            {
                int newIndex = mapIndex(i);
                if (newIndex >= 0)
                {
                    int start = neighborStart[i];
                    int count = neighborCount[i];
                    for (int j = 0; j < count; ++j)
                    {
                        int newNeighbor = mapNeighbor(neighbors[start + j]);
                        if (newNeighbor >= 0)
                        {
                            neighborSet.Add(new NeighborDetection.NeighborIndexPair(newIndex, newNeighbor));
                        }
                    }
                }
            }

            BuildNeighborMap(mapping, neighborSet);
        }

        public IEnumerable<PuzzlePiece> GetNeighbors(IPieceMapping mapping, IEnumerable<PuzzlePiece> input)
        {
            var inPieces = new HashSet<int>();
            var result = new HashSet<int>();

            foreach (var piece in input)
            {
                inPieces.Add(mapping.GetIndex(piece));
            }
            foreach (int p in inPieces)
            {
                foreach (int n in Enumerable.Range(neighborStart[p], neighborCount[p]))
                {
                    int pn = neighbors[n];
                    result.Add(pn);
                }
            }
            foreach (int p in result)
            {
                yield return mapping.GetPiece(p);
            }
        }

        public IEnumerable<PuzzlePiece> GetExternalNeighbors(IPieceMapping mapping, IEnumerable<PuzzlePiece> input)
        {
            var inPieces = new HashSet<int>();
            var result = new HashSet<int>();

            foreach (var piece in input)
            {
                inPieces.Add(mapping.GetIndex(piece));
            }
            foreach (int p in inPieces)
            {
                foreach (int n in Enumerable.Range(neighborStart[p], neighborCount[p]))
                {
                    int pn = neighbors[n];
                    if (!inPieces.Contains(pn))
                    {
                        result.Add(pn);
                    }
                }
            }
            foreach (int p in result)
            {
                yield return mapping.GetPiece(p);
            }
        }

        public IEnumerable<PuzzlePiece> GetInternalNeighbors(IPieceMapping mapping, IEnumerable<PuzzlePiece> input)
        {
            var inPieces = new HashSet<int>();
            var result = new HashSet<int>();

            foreach (var piece in input)
            {
                inPieces.Add(mapping.GetIndex(piece));
            }
            foreach (int p in inPieces)
            {
                foreach (int n in Enumerable.Range(neighborStart[p], neighborCount[p]))
                {
                    int pn = neighbors[n];
                    if (inPieces.Contains(pn))
                    {
                        result.Add(pn);
                    }
                }
            }
            foreach (int p in result)
            {
                yield return mapping.GetPiece(p);
            }
        }
    }
}
