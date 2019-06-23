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
        private readonly Dictionary<int, Effect> effects = new Dictionary<int, Effect>();
        private MaterialPropertyBlock materialProps = null;

        private static int HashCombine(int a, int b)
        {
            int hash = 17;
            hash = hash * 23 + a;
            hash = hash * 23 + b;
            return hash;
        }

        void Update()
        {
            if (SnapEffect.Evaluate(effects.Values.OfType<SnapEffect>(), out MixedRealityPose snapPose))
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

                if (RimColorEffect.Evaluate(effects.Values.OfType<RimColorEffect>(), out Color rimColor))
                {
                    materialProps.SetColor("_RimColor", rimColor);
                }
                else
                {
                    materialProps.SetColor("_RimColor", Color.black);
                }

                RimColorRenderer.SetPropertyBlock(materialProps);
            }

            if (GhostEffect.Evaluate(effects.Values.OfType<GhostEffect>()))
            {
                // nothing further to do
            }

            // Remove ended effects
            var endedEffects = effects.Where((item) => item.Value.HasEnded()).ToList();
            foreach (var item in endedEffects)
            {
                item.Value.Dispose();
                effects.Remove(item.Key);
            }
        }

        private int GetKey<T>(int id) where T : Effect
        {
            return HashCombine(id, typeof(T).GetHashCode());
        }

        public bool TryGetEffect<T>(int id, out T effect) where T : Effect
        {
            int key = GetKey<T>(id);
            if (effects.TryGetValue(key, out Effect result))
            {
                effect = result as T;
                return effect != null;
            }

            effect = null;
            return false;
        }

        public bool StartEffect<T>(int id, T effect) where T : Effect
        {
            int key = GetKey<T>(id);
            if (effects.ContainsKey(key))
            {
                effect.Dispose();
                return false;
            }

            effects.Add(key, effect);
            effect.Start();

            Update();

            return true;
        }

        public bool StopEffect<T>(int id) where T : Effect
        {
            int key = GetKey<T>(id);
            if (effects.TryGetValue(key, out Effect effect))
            {
                effect.Dispose();
                effects.Remove(key);
                return true;
            }
            return false;
        }

        public virtual Renderer RimColorRenderer { get; } = null;
    }

    public abstract class Effect : IDisposable
    {
        private float duration = 0.0f;
        public float Duration => duration;

        private bool isFinite = true;
        public bool IsFinite
        {
            get => isFinite;
            set
            {
                if (isFinite != value)
                {
                    isFinite = value;
                    if (isFinite)
                    {
                        startTime = Time.time;
                    }
                }
            }
        }

        private float startTime = 0.0f;
        public float StartTime => startTime;

        public float LocalTime => Mathf.Min(Time.time - startTime, duration);

        public Effect(float duration)
        {
            this.duration = duration;
        }

        public virtual void Dispose()
        {
        }

        public void Start()
        {
            this.startTime = Time.time;
        }

        public void Stop()
        {
            isFinite = false;
            duration = 0.0f;
        }

        public bool HasEnded()
        {
            return isFinite ? Time.time - startTime >= duration : false;
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
            return IsFinite ? animationCurve.Evaluate(LocalTime) : 1.0f;
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
        public Transform GhostParent
        {
            get => ghostObj.transform.parent;
        }

        public void SetGhostParent(Transform ghostParent)
        {
            ghostObj.transform.SetParent(ghostParent, false);
        }

        public Color Color;

        public MixedRealityPose LocalGhostPose
        {
            get => new MixedRealityPose(ghostObj.transform.localPosition, ghostObj.transform.localRotation);
            set
            {
                ghostObj.transform.localPosition = value.Position;
                ghostObj.transform.localRotation = value.Rotation;
            }
        }

        private GameObject ghostObj;
        private MeshRenderer[] ghostRenderers;

        private MaterialPropertyBlock materialProps;

        public GhostEffect(GameObject obj, Transform ghostParent, MixedRealityPose localPose, AnimationCurve animationCurve, Material ghostMaterial, Color color)
            : base(animationCurve)
        {
            this.Color = color;

            this.ghostObj = new GameObject($"Ghost_{obj.name}");
            this.ghostObj.transform.localPosition = localPose.Position;
            this.ghostObj.transform.localRotation = localPose.Rotation;

            var renderers = obj.GetComponentsInChildren<MeshRenderer>();
            ghostRenderers = new MeshRenderer[renderers.Length];
            for (int i = 0; i < renderers.Length; ++i)
            {
                var renderer = renderers[i];

                Transform ghostRenderObj;
                if (renderer.transform == obj.transform)
                {
                    ghostRenderObj = ghostObj.transform;
                }
                else
                {
                    var offsetObj = new GameObject("Mesh Offset");
                    offsetObj.transform.SetParent(ghostObj.transform, false);
                    offsetObj.transform.localPosition = obj.transform.InverseTransformPoint(renderer.transform.position);
                    offsetObj.transform.localRotation = Quaternion.Inverse(obj.transform.rotation) * renderer.transform.rotation;
                    ghostRenderObj = offsetObj.transform;
                }

                ghostRenderers[i] = ghostRenderObj.gameObject.AddComponent<MeshRenderer>();
                ghostRenderers[i].materials = Enumerable.Repeat(ghostMaterial, renderer.materials.Length).ToArray();

                var mesh = renderer.GetComponent<MeshFilter>();
                var ghostMesh = ghostRenderObj.gameObject.AddComponent<MeshFilter>();
                ghostMesh.name = mesh.name;
                // Creates a mutable copy of the mesh so we can change mateials
                ghostMesh.mesh = mesh.sharedMesh;
            }

            ghostObj.transform.SetParent(ghostParent, false);
        }

        public override void Dispose()
        {
            GameObject.Destroy(ghostObj);
        }

        public static bool Evaluate(IEnumerable<GhostEffect> effects)
        {
            foreach (var effect in effects)
            {
                // Lazy init
                if (effect.materialProps == null)
                {
                    effect.materialProps = new MaterialPropertyBlock();
                }

                float weight = effect.GetWeight();
                effect.materialProps.SetColor("_Color", effect.Color * weight);

                foreach (var ghostRenderer in effect.ghostRenderers)
                {
                    ghostRenderer.SetPropertyBlock(effect.materialProps);
                }
            }

            return true;
        }
    }
}
