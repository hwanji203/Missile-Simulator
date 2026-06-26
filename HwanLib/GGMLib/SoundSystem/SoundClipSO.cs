using UnityEngine;

namespace HwanLib.GGMLib.SoundSystem
{
    [CreateAssetMenu(fileName = "new Sound Clip", menuName = "GGMLib/Sound Clip", order = 0)]
    public class SoundClipSO : ScriptableObject
    {
        public AudioTypes audioType;
        public AudioClip audioClip;
        public bool isLoop = false;
        public bool randomizePitch = false;

        public bool isSpatialBlend = false;
        public float minDistance = 1f;
        public float maxDistance = 500f;

        [Range(0.1f, 1f)] public float randomPitchModifier = 0.1f;
        [Range(0.1f, 2f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
    }

    public enum AudioTypes
    {
        Sfx, Music
    }
}
