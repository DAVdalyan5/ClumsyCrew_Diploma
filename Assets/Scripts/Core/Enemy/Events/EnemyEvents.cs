using UnityEngine;

namespace HeistNSeek.Core.Enemy.Events
{
    /// <summary>
    /// Published when an enemy spots the player.
    /// </summary>
    public class PlayerSpottedEvent
    {
        public MonoBehaviour Enemy { get; }
        public Transform Player { get; }

        public PlayerSpottedEvent(MonoBehaviour enemy, Transform player)
        {
            Enemy = enemy;
            Player = player;
        }
    }

    /// <summary>
    /// Published when an enemy performs an attack. For damage system integration.
    /// </summary>
    public class EnemyAttackEvent
    {
        public MonoBehaviour Enemy { get; }
        public Transform Target { get; }

        public EnemyAttackEvent(MonoBehaviour enemy, Transform target)
        {
            Enemy = enemy;
            Target = target;
        }
    }

    /// <summary>
    /// Published when an enemy kills the player.
    /// </summary>
    public class PlayerKilledEvent
    {
        public MonoBehaviour Killer { get; }

        public PlayerKilledEvent(MonoBehaviour killer)
        {
            Killer = killer;
        }
    }
}
