namespace HeistNSeek.Core.Enemy.States
{
    /// <summary>
    /// Interface for enemy AI states. Mirrors ah-light pattern.
    /// Each state implements Enter, Execute (called every frame), and Exit.
    /// </summary>
    public interface IEnemyState
    {
        void Enter();
        void Execute();
        void Exit();
    }
}
