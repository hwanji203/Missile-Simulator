using UnityEngine;

namespace Effects
{
    public interface IPlayableVFX
    {
        int VfxHash { get; }
        float VfxDuration { get; }
        void PlayVFX(Vector3 position, Quaternion rotation);
        void PlayVFX(Quaternion rotation);
        void PlayVFX();
        void StopVFX();
    }
}
