namespace Engine
{
    using System;
    using UnityEngine;
    using UnityEngine.Events;

    public class InputHandler : MonoBehaviour
    {
        private RaycastHandler2D raycaster;
        [SerializeField] private Camera raycastCamera;
        Touch touch;
        TouchPhase touchPhase;
        public bool shouldTrackInput = false;
        public bool initialized = false;
        public UnityEvent<GameObject> OnRaycastHit;
        public LayerMask layerMask;
        // Optional overlay gate. The original raycast path is unchanged without a UI adapter.
        // public Func<bool> IsPointerInputBlocked;

        private void Awake()
        {
            raycaster = new RaycastHandler2D(raycastCamera, layerMask, Mathf.Infinity);
            // raycaster.OnRaycastHit += RaycastHit;
            initialized = true;
        }
        // private void OnApplicationQuit() { raycaster.OnRaycastHit -= RaycastHit; }

        public void UpdateRaycastLayer() => raycaster.UpdateLayerMask(layerMask);
        private void RaycastHit(object sender, RaycastEvent<RaycastHit2D> raycastEvent)
        {
            if (raycastEvent.raycastHit.transform != null)
            {
                OnRaycastHit?.Invoke(raycastEvent.raycastHit.transform.gameObject);
            }
        }

        public void Update()
        {
            // if (IsPointerInputBlocked?.Invoke() == true)
            // {
            //     return;
            // }
#if UNITY_EDITOR
            if (Input.GetMouseButtonUp(0))
            {
                // raycaster.Raycast(Input.mousePosition);
            }
#else
            if (!shouldTrackInput)
            {
                return;
            }
            if (Input.touchCount > 0)
            {
                ProcessInput();
            }
            else
            {
                return;
            }
#endif
        }
        private void ProcessInput()
        {
            touch = Input.GetTouch(0);
            touchPhase = touch.phase;
            switch (touchPhase)
            {
                case TouchPhase.Began:
                    break;
                case TouchPhase.Moved:
                    break;
                case TouchPhase.Stationary:
                    break;
                case TouchPhase.Ended:
                    // raycaster.Raycast(touch.position);
                    break;
                case TouchPhase.Canceled: break;
            }
        }
    }

}
