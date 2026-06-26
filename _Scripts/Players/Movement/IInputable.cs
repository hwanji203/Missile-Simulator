using UnityEngine;

namespace Players.Movement
{
    public interface IInputable
    {
        public void SetYawPitchRotation(Vector2 input);
        public void SetSuperBoost(bool isPress);
    }
}