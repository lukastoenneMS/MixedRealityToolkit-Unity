using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Pose = Microsoft.MixedReality.Toolkit.Utilities.MixedRealityPose;

namespace Parsley
{
    /// Manager class that keeps track of pieces eligible for joining
    public class BuildManager
    {

        // Maximum number of pieces held in the build list
        const int numBuildSlots = 2;
        // Properties of pieces while building
        private struct BuildSlot
        {
            public PuzzlePiece piece;
            public int colorIndex;
        }

        private readonly List<BuildSlot> slots = new List<BuildSlot>(numBuildSlots);

        public IEnumerable<PuzzlePiece> BuildPieces => slots.Select(s => s.piece);
        public bool IsEmpty => slots.Count == 0;

        private Color[] slotColors;
        private System.Random slotRng;

        private Suspender suspender;

        const int GhostEffectId = 83245;
        private Color ghostColor;

        public BuildManager(Color ghostColor)
        {
            this.ghostColor = ghostColor;

            slotRng = new System.Random(68342);
            suspender = new Suspender();
            slotColors = CreateColorPalette(ghostColor, numBuildSlots);
        }

        public void Add(PuzzlePiece piece)
        {
            Debug.Assert(piece != null);
            // Remove older entries of the same piece, it gets pushed back on top
            bool existedAlready = RemoveAll(p => p == piece) > 0;

            // Make sure the list does not outgrow the allowed limit
            if (!existedAlready)
            {
                while (slots.Count >= numBuildSlots)
                {
                    Remove(slots[0].piece);
                }
            }

            var slot = new BuildSlot();
            slot.piece = piece;
            slot.colorIndex =  GetRandomSlotColorIndex();
            slots.Add(slot);

            suspender.Suspend(piece.Body, false);
        }

        public void Remove(PuzzlePiece piece)
        {
            int effectId = GhostEffectId + piece.GetHashCode();
            piece.StopEffect<GhostEffect>(effectId);

            suspender.Drop(piece.Body);

            slots.RemoveAll(s => s.piece == piece);
        }

        public int RemoveAll(Predicate<PuzzlePiece> pred)
        {
            foreach (var s in slots)
            {
                if (pred(s.piece))
                {
                    suspender.Drop(s.piece.Body);
                }
            }
            return slots.RemoveAll(s => pred(s.piece));
        }

        public void Clear()
        {
            suspender.DropAll();
            slots.Clear();
        }

        private static Color[] CreateColorPalette(Color baseColor, int n, float hueShift = 1.0f/24.0f)
        {
            var palette = new Color[n];
            Color.RGBToHSV(baseColor, out float H, out float S, out float V);
            for (int i = 0; i < n; ++i)
            {
                float offset = 2.0f * (float)i / (float)(n-1) - 1.0f;
                palette[i] = Color.HSVToRGB(H + hueShift * offset, S, V);
                palette[i].a = baseColor.a;
            }
            return palette;
        }

        // Select a random color for a build slot
        private int GetRandomSlotColorIndex()
        {
            var availableColors = Enumerable.Range(0, slotColors.Length).ToList();
            foreach (var s in slots)
            {
                availableColors.RemoveAt(s.colorIndex);
            }
            if (availableColors.Count > 0)
            {
                int r = slotRng.Next() % availableColors.Count;
                return availableColors[r];
            }
            else
            {
                return 0;
            }
        }
    }
}
