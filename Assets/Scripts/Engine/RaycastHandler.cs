
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Engine
{
    using System;
    using UnityEngine;
    public class RaycastHandler
    {
        Camera raycastCamera;
        LayerMask layerFilter;
        float distance; Ray ray; RaycastHit hit; RaycastEvent<RaycastHit> raycastEvent;
        public EventHandler<RaycastEvent<RaycastHit>> OnRaycastHit;
        public RaycastHandler(Camera raycastCamera, LayerMask layerFilter, float distance)
        {
            this.raycastCamera = raycastCamera;
            this.layerFilter = layerFilter;
            this.distance = distance;
            raycastEvent= new RaycastEvent<RaycastHit>();
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
    
    public class RaycastHandler2D
    {
        Camera raycastCamera;
        LayerMask layerFilter;
        private float distance; RaycastHit2D hit; RaycastEvent<RaycastHit2D> raycastEvent;
        public EventHandler<RaycastEvent<RaycastHit2D>> OnRaycastHit;
        public RaycastHandler2D(Camera raycastCamera, LayerMask layerFilter, float distance)
        {
            this.raycastCamera = raycastCamera;
            this.layerFilter = layerFilter;
            this.distance = distance;
            raycastEvent= new RaycastEvent<RaycastHit2D>();
        }
        public void Raycast(Vector2 screenPosition)
        {
            if (Extensions.IsPointerOverUIElement())
            {
                return;
            }
            Vector3 worldPoint = raycastCamera.ScreenToWorldPoint(screenPosition);
            hit = Physics2D.Raycast(worldPoint, Vector2.zero, distance, layerFilter);
            if (hit == null || hit.collider == null)
            {
                Debug.LogError("FAILED TO HIT");
            }
            else if (hit.collider != null) 
            {
                raycastEvent.RaycastHitResultUpdate(hit);
                OnRaycastHit.Invoke(this, raycastEvent);
            }

        }

        public void UpdateLayerMask(LayerMask layerFilter) { this.layerFilter = layerFilter; }
    }

    
    public sealed class RaycastEvent<T> : EventArgs
    {
        public T raycastHit { get; private set; }

        internal void RaycastHitResultUpdate(T raycastHit)
        {
            this.raycastHit = raycastHit;
        }
    }
}

public static class Extensions
{
    public static bool IsPointerOverUIElement()
    {
        // 1. Create a fake pointer event at the current mouse/touch position
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        
        // 2. Create a list to harvest all the UI elements hit by the graphics raycaster
        List<RaycastResult> results = new List<RaycastResult>();
        
        // 3. Manually cast the ray against the UI layer
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        
        // 4. If the list count is greater than 0, we hit a UI element!
        return results.Count > 0;
    }
    
    public static bool Contains(this LayerMask mask, int layer)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    public static bool Contains(this LayerMask mask, GameObject gob)
    {
        if (gob == null) return false;
        return (mask.value & (1 << gob.layer)) != 0;
    }
}