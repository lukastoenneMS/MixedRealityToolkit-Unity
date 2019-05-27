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
    public class PuzzleShard : EffectManager
    {
        // Reference pose from the beginning of simulation
        private MixedRealityPose goal = MixedRealityPose.ZeroIdentity;
        public MixedRealityPose Goal => goal;

        private Renderer shardRenderer = null;

        public override Renderer RimColorRenderer => shardRenderer;

        void Awake()
        {
            var puzzleGame = transform.FindAncestorComponent<PuzzleGame>();
            if (puzzleGame == null)
            {
                Debug.LogWarning("PuzzleShard has no PuzzleGame parent component");
                gameObject.SetActive(false);
                return;
            }

            goal = new MixedRealityPose(transform.position, transform.rotation);

            Transform[] children = GetComponentsInChildren<Transform>();
            bool hasValidGeometry = false;
            foreach (var child in children)
            {
                if (child.name.EndsWith("Collider"))
                {
                    hasValidGeometry |= SetupCollider(child.gameObject);
                }
                else if (child.name.EndsWith("Render"))
                {
                    SetupRenderer(child.gameObject, out shardRenderer);
                }
            }

            // Only enable shards with valid collider geometry
            // to avoid bodies falling through environment colliders.
            if (!hasValidGeometry)
            {
                gameObject.SetActive(false);
                return;
            }
        }

        private static bool SetupCollider(GameObject go)
        {
            var meshFilter = go.GetComponent<MeshFilter>();
            if (!meshFilter)
            {
                Debug.LogWarning("Shard collider has no MeshFilter component");
                return false;
            }
            if (meshFilter.mesh.triangles.Length == 0)
            {
                return false;
            }

            var collider = go.GetComponent<Collider>();
            if (collider == null)
            {
                var meshCollider = go.AddComponent<MeshCollider>();
                collider = meshCollider;
                // Has to be convex to enable mesh-mesh collision
                meshCollider.convex = true;
            }

            var nearInteract = go.GetComponent<NearInteractionGrabbable>();
            if (nearInteract == null)
            {
                nearInteract = go.AddComponent<NearInteractionGrabbable>();
            }

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Disable rendering of colliders
                renderer.enabled = false;
            }

            return true;
        }

        private static bool SetupRenderer(GameObject go, out Renderer renderer)
        {
            renderer = go.GetComponent<Renderer>();
            return true;
        }
    }
}
