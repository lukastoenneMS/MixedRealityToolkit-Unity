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

            // Remove ended effects
            effects.RemoveAll((effect) => effect.HasEnded());
        }

        public void StartEffect(Effect effect)
        {
            effects.Add(effect);
            effect.Start();
        }

        public virtual Renderer RimColorRenderer { get; } = null;
    }

    public abstract class Effect
    {
        private float duration = 0.0f;
        public float Duration => duration;

        private float startTime = 0.0f;
        public float StartTime => startTime;

        public float LocalTime => Time.time - startTime;

        public Effect(float duration)
        {
            this.duration = duration;
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

        public SnapEffect(AnimationCurve animationCurve, Transform transform, MixedRealityPose target)
            : base(animationCurve)
        {
            this.localTargetOrigin = new MixedRealityPose(transform.localPosition, transform.localRotation);

            if (transform.parent)
            {
                this.localTarget = new MixedRealityPose(
                    transform.parent.InverseTransformPoint(target.Position),
                    Quaternion.Inverse(transform.parent.rotation) * target.Rotation);
            }
            else
            {
                this.localTarget = target;
            }
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
}
