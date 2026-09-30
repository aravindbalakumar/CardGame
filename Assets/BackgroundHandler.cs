using UnityEngine;

public class BackgroundHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer sr;

    [Header("Options")]
    [SerializeField] private bool fillEntireScreen = true;
    [SerializeField] private bool followCamera = true;

    private int lastScreenWidth;
    private int lastScreenHeight;
    private float lastOrthographicSize;

    private Camera MainCamera => Camera.main;

    private void Awake()
    {
        if (sr == null)
            sr = GetComponent<SpriteRenderer>();

        CacheScreenState();
    }

    private void Start()
    {
        ApplyToScreen();
    }

    private void Update()
    {
        if (Screen.width != lastScreenWidth ||
            Screen.height != lastScreenHeight ||
            (MainCamera != null && !Mathf.Approximately(MainCamera.orthographicSize, lastOrthographicSize)))
        {
            ApplyToScreen();
        }
    }

    private void LateUpdate()
    {
        if (followCamera)
            CenterOnCamera();
    }

    public void ApplyToScreen()
    {
        Camera cam = MainCamera;

        if (sr == null || cam == null || sr.sprite == null)
            return;

        CacheScreenState();

        // Camera bounds in world units
        float worldScreenHeight = cam.orthographicSize * 2f;
        float worldScreenWidth = worldScreenHeight * ((float)Screen.width / Screen.height);

        Vector2 spriteSize = sr.sprite.bounds.size;

        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
            return;

        // Scale factors needed to match the sprite to the screen bounds
        float scaleX = worldScreenWidth / spriteSize.x;
        float scaleY = worldScreenHeight / spriteSize.y;

        // max = cover screen entirely, min = fit inside with no cropping
        float targetScale = fillEntireScreen
            ? Mathf.Max(scaleX, scaleY)
            : Mathf.Min(scaleX, scaleY);

        // Keep the world scale correct even when a scaled parent is present
        Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        float xScale = Mathf.Approximately(parentScale.x, 0f) ? targetScale : targetScale / parentScale.x;
        float yScale = Mathf.Approximately(parentScale.y, 0f) ? targetScale : targetScale / parentScale.y;

        transform.localScale = new Vector3(xScale, yScale, 1f);

        CenterOnCamera();
    }

    private void CenterOnCamera()
    {
        Camera cam = MainCamera;

        if (cam == null)
            return;

        Vector3 position = transform.position;
        position.x = cam.transform.position.x;
        position.y = cam.transform.position.y;
        transform.position = position;
    }

    private void CacheScreenState()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        lastOrthographicSize = MainCamera != null ? MainCamera.orthographicSize : 0f;
    }
}