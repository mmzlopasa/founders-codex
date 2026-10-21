using UnityEngine;

namespace Founders.World
{
    /// <summary>
    /// Touch-first map navigation with editor mouse controls for iteration.
    /// Attach to the scene camera.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class MobileMapCamera : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float panUnitsPerPixel = 0.75f;
        [SerializeField, Min(0.01f)] private float zoomUnitsPerPixel = 1.2f;
        [SerializeField] private float minimumHeight = 45f;
        [SerializeField] private float maximumHeight = 950f;
        [SerializeField] private float minimumPitch = 28f;
        [SerializeField] private float maximumPitch = 78f;

        private Vector2 previousPointer;
        private float previousPinchDistance;
        private bool dragging;

        private void Update()
        {
            if (Input.touchCount >= 2)
            {
                dragging = false;
                HandlePinch();
                return;
            }

            if (Input.touchCount == 1)
            {
                HandleSingleTouch(Input.GetTouch(0));
                return;
            }

            HandleMouse();
        }

        private void HandleSingleTouch(Touch touch)
        {
            if (touch.phase == TouchPhase.Began)
            {
                previousPointer = touch.position;
                dragging = true;
                return;
            }

            if (touch.phase == TouchPhase.Moved && dragging)
            {
                Pan(touch.position - previousPointer);
                previousPointer = touch.position;
            }

            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                dragging = false;
        }

        private void HandlePinch()
        {
            var first = Input.GetTouch(0);
            var second = Input.GetTouch(1);
            var distance = Vector2.Distance(first.position, second.position);

            if (first.phase == TouchPhase.Began || second.phase == TouchPhase.Began)
            {
                previousPinchDistance = distance;
                return;
            }

            if (previousPinchDistance > 0f)
                Zoom(distance - previousPinchDistance);

            previousPinchDistance = distance;
        }

        private void HandleMouse()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetMouseButtonDown(0))
            {
                previousPointer = Input.mousePosition;
                dragging = true;
            }
            else if (Input.GetMouseButton(0) && dragging)
            {
                var pointer = (Vector2)Input.mousePosition;
                Pan(pointer - previousPointer);
                previousPointer = pointer;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                dragging = false;
            }

            var wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > 0.001f)
                Zoom(wheel * 45f);
#endif
        }

        private void Pan(Vector2 screenDelta)
        {
            var planarRight = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
            var planarForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            var heightFactor = Mathf.Clamp(transform.position.y / 180f, 0.4f, 4f);
            var movement = (-planarRight * screenDelta.x - planarForward * screenDelta.y)
                           * panUnitsPerPixel * heightFactor;
            transform.position += movement;
        }

        private void Zoom(float screenDelta)
        {
            var position = transform.position + transform.forward * (screenDelta * zoomUnitsPerPixel);
            position.y = Mathf.Clamp(position.y, minimumHeight, maximumHeight);
            transform.position = position;

            var angles = transform.eulerAngles;
            var pitch = angles.x > 180f ? angles.x - 360f : angles.x;
            pitch = Mathf.Clamp(pitch, minimumPitch, maximumPitch);
            transform.rotation = Quaternion.Euler(pitch, angles.y, 0f);
        }
    }
}
