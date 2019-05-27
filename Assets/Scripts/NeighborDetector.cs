using Microsoft.MixedReality.Toolkit;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Parsley.NeighborDetection
{
    public struct NeighborIndexPair : IEquatable<NeighborIndexPair>
    {
        public readonly int indexMinor;
        public readonly int indexMajor;

        public NeighborIndexPair(int a, int b)
        {
            if (a <= b)
            {
                indexMinor = a;
                indexMajor = b;
            }
            else
            {
                indexMinor = b;
                indexMajor = a;
            }
        }

        public bool Equals(NeighborIndexPair other)
        {
            return indexMinor == other.indexMinor && indexMajor == other.indexMajor;
        }
    }

    public class NeighborIndexSet : HashSet<NeighborIndexPair>
    {
    }

    internal class NeighborDetector : MonoBehaviour
    {
        public int Index = -1;

        public NeighborIndexSet neighborSet = null;

        void OnCollisionEnter(Collision collision)
        {
            var otherDetector = collision.gameObject.FindAncestorComponent<NeighborDetector>();
            if (otherDetector != null)
            {
                // Debug.Log($"{name}|{Index} collided with {collision.gameObject.name}|{otherDetector.Index}");
                neighborSet.Add(new NeighborIndexPair(Index, otherDetector.Index));
            }
        }
    }

    /// Utility class to build a map of neighbors based on collision
    public class NeighborMapBuilder : IDisposable
    {
        // Temp. variables for constructing neighbor map
        private NeighborIndexSet neighborSet = null;
        public NeighborIndexSet NeighborSet => neighborSet;

        private List<NeighborDetector> neighborDetectors = null;

        public NeighborMapBuilder()
        {
            // Start collecting neighbor pairs
            neighborSet = new NeighborIndexSet();
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
            neighborSet = null;
        }

        /// Add a temporary neighbor detector to a game object to build the neighbor map
        public void Add(GameObject go, int index)
        {
            var neighborDetector = go.AddComponent<NeighborDetector>();
            neighborDetectors.Add(neighborDetector);
            neighborDetector.Index = index;
            neighborDetector.neighborSet = neighborSet;
        }
    }
}
