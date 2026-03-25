using System.Collections.Generic;
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

    // --- Combat: A2 ---
    public struct EnemyPushedEvent
    {
        public int       EntityId;
        public Vector2Int FromPos;
        public Vector2Int ToPos;
        public bool      WallHit;   // true → +1 bonus damage applied
    }

    public struct BombExplodedEvent
    {
        public int                 EntityId;
        public List<Vector2Int>    AffectedCells;
    }

    public struct ArmorRemovedEvent
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
