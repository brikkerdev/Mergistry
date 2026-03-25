using Mergistry.Data;
using UnityEngine;

namespace Mergistry.Events
{
    // --- Input ---
    public struct DragStartEvent  { public Vector3 WorldPosition; }
    public struct DragUpdateEvent { public Vector3 WorldPosition; }
    public struct DragEndEvent    { public Vector3 WorldPosition; }

    // --- Distillation ---
    public struct MergePerformedEvent  { }
    public struct InfusePerformedEvent { }

    // --- Combat: player actions ---
    public struct PotionThrownEvent
    {
        public PotionType Type;
        public int        Level;
        public Vector2Int TargetCell;
    }

    // --- Combat: enemy ---
    public struct EnemyDamagedEvent
    {
        public int EntityId;
        public int Damage;
        public int HPRemaining;
    }

    public struct EnemyDiedEvent
    {
        public int EntityId;
    }

    // --- Combat: player damage ---
    public struct PlayerDamagedEvent
    {
        public int Damage;
        public int HPRemaining;
    }

    // --- Combat: outcome ---
    public struct CombatEndedEvent
    {
        public bool Victory;
    }

    // --- Game Flow ---
    public struct DistillationPhaseStartedEvent { }
    public struct CombatPhaseStartedEvent       { }
}
