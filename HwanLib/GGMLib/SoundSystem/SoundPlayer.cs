using System;
using System.Collections;
using ObjectPool.Runtime;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace HwanLib.GGMLib.SoundSystem
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundPlayer : MonoBehaviour, IPoolable
    {
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup musicGroup;

        private AudioSource _audioSource;

        public GameObject GameObject => this == null ? null : gameObject;
        [field: SerializeField] public PoolItemSO Item { get; set; }

        public event Action<SoundPlayer> OnSoundFinished;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        public void PlaySound(SoundClipSO clipData)
        {
            _audioSource.outputAudioMixerGroup = clipData.audioType == AudioTypes.Sfx ? sfxGroup : musicGroup;
            _audioSource.volume = clipData.volume;
            _audioSource.pitch = clipData.pitch;
            _audioSource.spatialBlend = clipData.isSpatialBlend ? 1f : 0f;
            _audioSource.maxDistance = clipData.isSpatialBlend ? clipData.maxDistance : 500f;
            _audioSource.minDistance = clipData.isSpatialBlend ? clipData.minDistance : 1f;

            if (clipData.randomizePitch)
                _audioSource.pitch += Random.Range(-clipData.randomPitchModifier, clipData.randomPitchModifier);

            _audioSource.clip = clipData.audioClip;
            _audioSource.loop = clipData.isLoop;

            if (!clipData.isLoop)
                StartCoroutine(DisableSoundTimer(_audioSource.clip.length + 2f));

            _audioSource.Play();
        }

        private IEnumerator DisableSoundTimer(float duration)
        {
            yield return new WaitForSeconds(duration);
            OnSoundFinished?.Invoke(this);
        }

        public void ForceStopSound()
        {
            _audioSource.Stop();
        }

        public void ResetItem() { }
    }
}
