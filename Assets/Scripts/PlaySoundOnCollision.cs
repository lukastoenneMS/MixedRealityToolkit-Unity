using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using UnityEngine;

namespace Parsley
{
    [Serializable]
    public class PlaySoundOnCollision : MonoBehaviour
    {
        public AudioClip Clip = null;

        public float MinVelocity = 1.0f;
        public float MaxVelocity = 5.0f;

        public float MinPitch = 0.96f;
        public float MaxPitch = 1.04f;

        private AudioSource source = null;

        private System.Random rng = null;

        void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1.0f;

            rng = new System.Random(gameObject.GetInstanceID());
        }

        void OnCollisionEnter(Collision collision)
        {
            if (Clip)
            {
                float velocity = collision.relativeVelocity.magnitude;
                if (velocity > MinVelocity)
                {
                    source.volume = Mathf.InverseLerp(MinVelocity, MaxVelocity, Mathf.Min(velocity, MaxVelocity));
                    if (!source.isPlaying)
                    {
                        source.pitch = Mathf.Lerp(MinPitch, MaxPitch, (float)rng.NextDouble());
                        source.PlayOneShot(Clip);
                    }
                }
            }
        }
    }
}
