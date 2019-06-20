using Microsoft.MixedReality.Toolkit;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Parsley.NeighborDetection
{
    public struct NeighborPair : IEquatable<NeighborPair>
    {
        public readonly PuzzlePiece pieceA;
        public readonly PuzzlePiece pieceB;

        public NeighborPair(PuzzlePiece a, PuzzlePiece b)
        {
            if (a.GetHashCode() <= b.GetHashCode())
            {
                pieceA = a;
                pieceB = b;
            }
            else
            {
                pieceA = b;
                pieceB = a;
            }
        }

        public bool Equals(NeighborPair other)
        {
            return pieceA == other.pieceA && pieceB == other.pieceB;
        }
    }

    public class NeighborPairSet : HashSet<NeighborPair>
    {}

    internal class NeighborDetector : MonoBehaviour
    {
        public PuzzlePiece Piece;

        public NeighborPairSet neighborPairs = null;

        void OnCollisionEnter(Collision collision)
        {
            var otherDetector = collision.gameObject.FindAncestorComponent<NeighborDetector>();
            if (otherDetector != null)
            {
                neighborPairs.Add(new NeighborPair(Piece, otherDetector.Piece));
            }
        }
    }

    /// Utility class to build a map of neighbors based on collision
    public class NeighborMapBuilder : IDisposable
    {
        // Temp. variables for constructing neighbor map
        private NeighborPairSet neighborPairs = null;
        public NeighborPairSet NeighborPairs => neighborPairs;

        private List<NeighborDetector> neighborDetectors = null;

        public NeighborMapBuilder()
        {
            // Start collecting neighbor pairs
            neighborPairs = new NeighborPairSet();
            neighborDetectors = new List<NeighborDetector>();
        }

        public void Dispose()
        {
            // Clean up, don't need temp. neighbor set any longer
            for (int i = 0; i < neighborDetectors.Count; ++i)
            {
                GameObject.Destroy(neighborDetectors[i]);
            }
            neighborDetectors = null;
            neighborPairs = null;
        }

        /// Add a temporary neighbor detector to a game object to build the neighbor map
        public void Add(PuzzlePiece piece)
        {
            var neighborDetector = piece.gameObject.AddComponent<NeighborDetector>();
            neighborDetectors.Add(neighborDetector);
            neighborDetector.Piece = piece;
            neighborDetector.neighborPairs = neighborPairs;
        }
    }
}
