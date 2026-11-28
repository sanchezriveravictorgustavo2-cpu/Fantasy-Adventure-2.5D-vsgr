using UnityEngine;

namespace Game.Systems.Combat
{
    public enum TeamFaction
    {
        Ally = 0,
        Enemy = 1
    }

    /// <summary>
    /// Common combat target contract shared by units (Miners, Explorers, Archers, Enemies)
    /// and structures (Player_Statue, Enemy_Statue).
    /// </summary>
    public interface ICombatTarget
    {
        TeamFaction Faction { get; }
        bool IsAlive { get; }
        bool IsStructure { get; }
        float CurrentHealth { get; }
        float MaxHealth { get; }
        Vector3 TargetPosition { get; }
        Transform TargetTransform { get; }
        bool TakeDamage(float amount, TeamFaction attackerFaction);
    }
}
