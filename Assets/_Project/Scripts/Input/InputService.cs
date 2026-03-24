using Mergistry.Core;
using Mergistry.Events;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mergistry.Input
{
    public class InputService : MonoBehaviour
    {
        private Camera _camera;
        private bool   _isDragging;

        private void Awake() => _camera = Camera.main;

        private void Update()
        {
            HandleMouse();
            HandleTouch();
        }

        private void HandleMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 screen = mouse.position.ReadValue();

            if (mouse.leftButton.wasPressedThisFrame)
            {
                _isDragging = true;
                EventBus.Publish(new DragStartEvent { WorldPosition = ToWorld(screen) });
            }
            else if (_isDragging && mouse.leftButton.isPressed)
            {
                EventBus.Publish(new DragUpdateEvent { WorldPosition = ToWorld(screen) });
            }
            else if (_isDragging && mouse.leftButton.wasReleasedThisFrame)
            {
                _isDragging = false;
                EventBus.Publish(new DragEndEvent { WorldPosition = ToWorld(screen) });
            }
        }

        private void HandleTouch()
        {
            var ts = Touchscreen.current;
            if (ts == null) return;

            var touch = ts.primaryTouch;
            Vector2 screen = touch.position.ReadValue();

            if (touch.press.wasPressedThisFrame)
            {
                _isDragging = true;
                EventBus.Publish(new DragStartEvent { WorldPosition = ToWorld(screen) });
            }
            else if (_isDragging && touch.press.isPressed)
            {
                EventBus.Publish(new DragUpdateEvent { WorldPosition = ToWorld(screen) });
            }
            else if (_isDragging && touch.press.wasReleasedThisFrame)
            {
                _isDragging = false;
                EventBus.Publish(new DragEndEvent { WorldPosition = ToWorld(screen) });
            }
        }

        private Vector3 ToWorld(Vector2 screenPos)
        {
            var pos = new Vector3(screenPos.x, screenPos.y,
                Mathf.Abs(_camera.transform.position.z));
            return _camera.ScreenToWorldPoint(pos);
        }
    }
}
