using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Effects
{
    public class PlayParticleVFX : MonoBehaviour, IPlayableVFX
    {
        [SerializeField] private AssetHashSO vfxAsset;
        [field: SerializeField] public float VfxDuration { get; private set; }
        [SerializeField] private ParticleSystem[] particles;

        public int VfxHash => vfxAsset.AssetHash;

        protected bool IsPlaying;

        private void Update()
        {
            if (Keyboard.current.gKey.wasPressedThisFrame && IsPlaying)
                StopVFX();
        }

        public void PlayVFX(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            PlayVFX();
        }

        public void PlayVFX(Quaternion rotation)
        {
            transform.rotation = rotation;
            PlayVFX();
        }

        public void PlayVFX()
        {
            foreach (ParticleSystem particle in particles)
                particle.Play();
            IsPlaying = true;
        }

        public void StopVFX()
        {
            foreach (ParticleSystem particle in particles)
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            IsPlaying = false;
        }
    }
}
