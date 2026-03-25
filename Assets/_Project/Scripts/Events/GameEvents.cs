using System.Collections.Generic;
using Mergistry.Data;
using Mergistry.Models.Combat;
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

    // --- Combat: A3 zones ---
    public struct ZoneCreatedEvent
    {
        public ZoneType             Type;
        public List<Vector2Int>     Positions;
        public int                  TurnsRemaining;
    }

    public struct ZoneExpiredEvent
    {
        public Vector2Int Position;
        public ZoneType   Type;
    }

    // --- Combat: A3 enemies ---
    public struct EnemyTeleportedEvent
    {
        public int        EntityId;
        public Vector2Int FromPos;
        public Vector2Int ToPos;
    }

    public struct EnemyRevivedEvent
    {
        public int        EntityId;
        public Vector2Int Position;
    }

    // --- Game Flow ---
    public struct DistillationPhaseStartedEvent { }
    public struct CombatPhaseStartedEvent       { }

    // --- A5: Events & Relics ---
    public struct RelicAcquiredEvent
    {
        public RelicType Type;
    }

    public struct EventChoiceMadeEvent
    {
        public EventNodeType EventType;
        public string        OutcomeType;
    }
}
