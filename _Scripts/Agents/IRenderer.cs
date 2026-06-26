namespace Agents
{
    public interface IRenderer
    {
        void PlayClip(int clipHash, float normalizedTime, float crossFadeDuration, int layerIndex = 0);

        // 애니메이터 전역 재생 속도 배수(1 = 기본). 루트모션 이동 속도에도 영향을 준다.
        void SetSpeed(float speed);

        // 해당 클립(상태) 해시가 현재 재생 중인지. CrossFade 전이 중이면 전이 대상도 재생 중으로 본다.
        bool IsPlayingClip(int clipHash, int layerIndex = 0);
    }
}
