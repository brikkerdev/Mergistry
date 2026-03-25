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

    // --- Combat ---
    public struct PotionThrownEvent
    {
        public PotionType Type;
        public int        Level;
        public Vector2Int TargetCell;
    }

    // --- Game Flow ---
    public struct DistillationPhaseStartedEvent { }
    public struct CombatPhaseStartedEvent       { }
}
