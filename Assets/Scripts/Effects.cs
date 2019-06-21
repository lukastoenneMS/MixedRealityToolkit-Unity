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
    public class EffectManager : MonoBehaviour
    {
        private readonly List<Effect> effects = new List<Effect>();
        private MaterialPropertyBlock materialProps = null;

        void Update()
        {
            if (SnapEffect.Evaluate(effects.OfType<SnapEffect>(), out MixedRealityPose snapPose))
            {
                transform.localPosition = snapPose.Position;
                transform.localRotation = snapPose.Rotation;
            }

            if (RimColorRenderer)
            {
                // Lazy init
                if (materialProps == null)
                {
                    materialProps = new MaterialPropertyBlock();
                }

                if (RimColorEffect.Evaluate(effects.OfType<RimColorEffect>(), out Color rimColor))
                {
                    materialProps.SetColor("_RimColor", rimColor);
                }
                else
                {
                    materialProps.SetColor("_RimColor", Color.black);
                }

                RimColorRenderer.SetPropertyBlock(materialProps);
            }

            if (GhostEffect.Evaluate(effects.OfType<GhostEffect>(), this.transform))
            {
                // nothing further to do
            }

            // Remove ended effects
            var endedEffects = effects.Where((e) => e.HasEnded()).ToList();
            foreach (var effect in endedEffects)
            {
                effects.Remove(effect);
                effect.Dispose();
            }
        }

        public void StartEffect(Effect effect)
        {
            effects.Add(effect);
            effect.Start();

            Update();
        }

        public virtual Renderer RimColorRenderer { get; } = null;
    }

    public abstract class Effect : IDisposable
    {
        private float duration = 0.0f;
        public float Duration => duration;

        private float startTime = 0.0f;
        public float StartTime => startTime;

        public float LocalTime => Mathf.Min(Time.time - startTime, duration);

        public Effect(float duration)
        {
            this.duration = duration;
        }

        public void Dispose()
        {
        }

        public void Start()
        {
            this.startTime = Time.time;
        }

        public bool HasEnded()
        {
            return Time.time - startTime >= duration;
        }
    }

    public abstract class AnimatedEffect : Effect
    {
        private AnimationCurve animationCurve = null;

        public AnimatedEffect(AnimationCurve animationCurve)
            : base(animationCurve.Duration())
        {
            this.animationCurve = animationCurve;
        }

        public float GetWeight()
        {
            return animationCurve.Evaluate(LocalTime);
        }
    }

    public class SnapEffect : AnimatedEffect
    {
        private MixedRealityPose localTarget;
        private MixedRealityPose localTargetOrigin;

        public SnapEffect(AnimationCurve animationCurve, Transform transform, MixedRealityPose localTarget)
            : base(animationCurve)
        {
            this.localTargetOrigin = new MixedRealityPose(transform.localPosition, transform.localRotation);
            this.localTarget = localTarget;
        }

        public static bool Evaluate(IEnumerable<SnapEffect> effects, out MixedRealityPose result)
        {
            if (!effects.Any())
            {
                result = MixedRealityPose.ZeroIdentity;
                return false;
            }

            var effect = effects.Last();
            float snap = effect.GetWeight();
            Vector3 snapPosition = Vector3.Lerp(effect.localTargetOrigin.Position, effect.localTarget.Position, snap);
            Quaternion snapRotation = Quaternion.Slerp(effect.localTargetOrigin.Rotation, effect.localTarget.Rotation, snap);
            result = new MixedRealityPose(snapPosition, snapRotation);
            return true;
        }
    }

    public class RimColorEffect : AnimatedEffect
    {
        private Color color = Color.white;
        public Color Color => color;

        public RimColorEffect(AnimationCurve animationCurve, Color color)
            : base(animationCurve)
        {
            this.color = color;
        }

        public static bool Evaluate(IEnumerable<RimColorEffect> effects, out Color result)
        {
            result = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            float totWeight = 0.0f;
            foreach (var effect in effects)
            {
                float weight = effect.GetWeight();
                totWeight += weight;
                result += effect.Color * weight;
            }
            return totWeight > 0.0f;
        }
    }

    public class SoundEffect : Effect
    {
        const float minPitch = 0.2f;
        const float maxPitch = 5.0f;

        public SoundEffect(AudioSource source, AudioClip clip)
            : base(clip.length)
        {
            source.PlayOneShot(clip);
            source.pitch = 1.0f;
        }

        public SoundEffect(AudioSource source, AudioClip clip, float duration)
            : base(duration)
        {
            source.PlayOneShot(clip);
            source.pitch = clip.length / duration;
        }
    }

    public class GhostEffect : AnimatedEffect
    {
        private GameObject ghostObj;
        private MeshRenderer ghostRenderer;

        private Color color;

        private MaterialPropertyBlock materialProps;

        public GhostEffect(GameObject obj, MixedRealityPose localPose, AnimationCurve animationCurve, Material ghostMaterial, Color color)
            : base(animationCurve)
        {
            this.color = color;

            this.ghostObj = new GameObject($"Ghost_{obj.name}");
            this.ghostObj.transform.localPosition = localPose.Position;
            this.ghostObj.transform.localRotation = localPose.Rotation;

            var renderer = obj.GetComponentInChildren<MeshRenderer>();
            if (renderer)
            {
                Transform ghostParent;
                if (renderer.transform == obj.transform)
                {
                    ghostParent = ghostObj.transform;
                }
                else
                {
                    var offsetObj = new GameObject("Mesh Offset");
                    offsetObj.transform.SetParent(ghostObj.transform, false);
                    offsetObj.transform.localPosition = obj.transform.InverseTransformPoint(renderer.transform.position);
                    offsetObj.transform.localRotation = Quaternion.Inverse(obj.transform.rotation) * renderer.transform.rotation;
                    ghostParent = offsetObj.transform;
                }

                ghostRenderer = ghostParent.gameObject.AddComponent<MeshRenderer>();
                ghostRenderer.materials = Enumerable.Repeat(ghostMaterial, renderer.materials.Length).ToArray();

                var mesh = renderer.GetComponent<MeshFilter>();
                var ghostMesh = ghostParent.gameObject.AddComponent<MeshFilter>();
                ghostMesh.name = mesh.name;
                // Creates a mutable copy of the mesh so we can change mateials
                ghostMesh.mesh = mesh.sharedMesh;
            }
        }

        public new void Dispose()
        {
            GameObject.Destroy(ghostObj);
        }

        public static bool Evaluate(IEnumerable<GhostEffect> effects, Transform parent)
        {
            foreach (var effect in effects)
            {
                if (effect.ghostRenderer)
                {
                    // Lazy init
                    if (effect.materialProps == null)
                    {
                        effect.materialProps = new MaterialPropertyBlock();
                    }

                    float weight = effect.GetWeight();
                    effect.materialProps.SetColor("_Color", effect.color * weight);
                    effect.ghostRenderer.SetPropertyBlock(effect.materialProps);
                }

                if (effect.ghostObj.transform.parent != parent)
                {
                    effect.ghostObj.transform.SetParent(parent, false);
                }
            }

            return true;
        }
    }
}
