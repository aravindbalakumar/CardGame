
namespace Engine
{
    using System;
    using UnityEngine;
    public class RaycastHandler
    {
        Camera raycastCamera;
        LayerMask layerFilter;
        float distance; Ray ray; RaycastHit hit; RaycastEvent raycastEvent;
        public EventHandler<RaycastEvent> OnRaycastHit;
        public RaycastHandler(Camera raycastCamera, LayerMask layerFilter, float distance)
        {
            this.raycastCamera = raycastCamera;
            this.layerFilter = layerFilter;
            this.distance = distance;
            raycastEvent= new RaycastEvent();
        }
        public void Raycast(Vector2 screenPosition)
        {
            ray = raycastCamera.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out hit, distance, layerFilter))
            {
                raycastEvent.RaycastHitResultUpdate(hit);
                OnRaycastHit.Invoke(this, raycastEvent);
            }

        }

        public void UpdateLayerMask(LayerMask layerFilter) { this.layerFilter = layerFilter; }
    }


    public sealed class RaycastEvent : EventArgs
    {
        public RaycastHit raycastHit { get; private set; }

        internal void RaycastHitResultUpdate(RaycastHit raycastHit)
        {
            this.raycastHit = raycastHit;
        }
    }
}