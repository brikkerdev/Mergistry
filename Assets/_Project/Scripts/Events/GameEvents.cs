using UnityEngine;

namespace Mergistry.Events
{
    // --- Input ---
    public struct DragStartEvent  { public Vector3 WorldPosition; }
    public struct DragUpdateEvent { public Vector3 WorldPosition; }
    public struct DragEndEvent    { public Vector3 WorldPosition; }

    // --- Game Flow ---
    public struct DistillationPhaseStartedEvent { }
    public struct CombatPhaseStartedEvent       { }
}
