namespace Agents.FSM
{
    public abstract class AgentState
    {
        protected readonly Agent Agent;
        protected readonly int StateClipHash; //해당 상태의 애니메이션 클립 해시
        protected readonly IRenderer Renderer;

        public AgentState(Agent agent, int stateClipHash)
        {
            Agent = agent;
            StateClipHash = stateClipHash;
            Renderer = agent.GetModule<IRenderer>();
        }

        public virtual void Enter(float transitionDuration, int layerIndex = 0)
        {
            Renderer.PlayClip(StateClipHash, 0f, transitionDuration, layerIndex);
        }

        public virtual void Update() {}
        public virtual void Exit() {}
    }
}
