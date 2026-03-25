using System;
using Mergistry.Core;
using Mergistry.Events;
using UnityEngine;

namespace Mergistry.UI.Popups
{
    /// <summary>
    /// Fires OnClicked when a DragStart lands inside this GameObject's Collider bounds (XY only).
    /// Compatible with new Input System via EventBus.
    /// Requires a Collider (e.g. BoxCollider) on the same GameObject.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SlotClickHandler : MonoBehaviour
    {
        public Action OnClicked;

        private Collider _col;

        private void Awake() => _col = GetComponent<Collider>();

        private void OnEnable()  => EventBus.Subscribe<DragStartEvent>(OnDragStart);
        private void OnDisable() => EventBus.Unsubscribe<DragStartEvent>(OnDragStart);

        private void OnDragStart(DragStartEvent e)
        {
            if (_col == null) return;
            var b = _col.bounds;
            var p = e.WorldPosition;
            // Check XY overlap only (Z doesn't matter in this 2D-style game)
            if (p.x >= b.min.x && p.x <= b.max.x &&
                p.y >= b.min.y && p.y <= b.max.y)
            {
                OnClicked?.Invoke();
            }
        }
    }
}
