using UnityEngine;

public class BackgroundHandler : MonoBehaviour
{
    [SerializeField]SpriteRenderer sr;
    public void Start(){
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    if(sr==null) return;
            if (sr.sprite == null) return;
    
            // Get camera bounds in world units
            float worldScreenHeight = Camera.main.orthographicSize * 2.0f;
            float worldScreenWidth = worldScreenHeight / Screen.height * Screen.width;
    
            // Get sprite size
            Vector2 spriteSize = sr.sprite.bounds.size;
    
            // Calculate scale factors
            float scaleX = worldScreenWidth / spriteSize.x;
            float scaleY = worldScreenHeight / spriteSize.y;
    
            // Apply scale (use max to fill screen entirely, or min to fit inside)
            float maxScale = Mathf.Max(scaleX, scaleY);
            transform.localScale = new Vector3(maxScale, maxScale, 1f);
    
            // Center position with camera
            transform.position = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y, transform.position.z);
        }
    }

