using Unity.Behavior;

namespace Enemies.BT
{
    [BlackboardEnum]
    public enum EnemyState
    {
        IDLE, MOVE, COMBAT, HIT, FLEE, DEATH
    }
}
