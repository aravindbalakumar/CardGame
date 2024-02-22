namespace Engine
{
    using System;
    using UnityEngine;
    using UnityEngine.Events;

    public class InputHandler : MonoBehaviour
    {
        private RaycastHandler raycaster;
        [SerializeField] private Camera raycastCamera;
        Touch touch;
        TouchPhase touchPhase;
        public bool shouldTrackInput = false;
        public bool initialized=false;
        public UnityEvent<GameObject> OnRaycastHit;
        public LayerMask layerMask;
      
        private void Awake()
        {
            raycaster = new RaycastHandler(raycastCamera, layerMask, Mathf.Infinity);
            raycaster.OnRaycastHit += RaycastHit;
            initialized = true;
        }
        private void OnApplicationQuit() { raycaster.OnRaycastHit -= RaycastHit; }


        private void RaycastHit(object sender, RaycastEvent raycastEvent)
        {
            if (raycastEvent.raycastHit.transform != null)
            {
                OnRaycastHit?.Invoke(raycastEvent.raycastHit.transform.gameObject);
            }
        }

        public void Update()
        {
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
                    raycaster.Raycast(touch.position);
                    break;
                case TouchPhase.Canceled: break;
            }
        }
    }

}