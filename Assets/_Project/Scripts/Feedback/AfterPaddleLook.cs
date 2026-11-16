using UnityEngine;

// After-only surface treatment. Geometry and gameplay remain the starter's size.
public sealed class AfterPaddleLook : MonoBehaviour
{
    private Texture2D surface;
    private Sprite paddleSprite;

    private void Start()
    {
        var paddle = FindFirstObjectByType<Paddle>();
        if (paddle == null || paddle.gameObject.scene != gameObject.scene) return;
        var visual = paddle.GetComponentInChildren<SpriteRenderer>();
        if (visual == null || visual.sprite == null) return;
        Bounds bounds = visual.sprite.bounds;
        const int size = 128;
        surface = new Texture2D(size, size, TextureFormat.RGBA32, false);
        surface.name = "After paddle beveled surface";
        surface.wrapMode = TextureWrapMode.Clamp;
        surface.filterMode = FilterMode.Bilinear;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float height = y / (float)(size - 1);
            Color color = Color.Lerp(new Color(0.40f, 0.60f, 0.72f),
                new Color(0.92f, 0.98f, 1f), height);
            // Cyan endcaps echo the ball trail; narrow seams give them depth.
            if (x < 13 || x >= size - 13)
                color = Color.Lerp(new Color(0.08f, 0.42f, 0.60f),
                    new Color(0.35f, 0.90f, 1f), height);
            if (x == 13 || x == size - 14) color *= 0.65f;
            if (y < 14) color *= 0.55f;
            if (y >= size - 12) color = Color.Lerp(color, Color.white, 0.72f);
            if (x < 2 || x >= size - 2) color *= 0.65f;
            color.a = 1f;
            pixels[y * size + x] = color;
        }
        surface.SetPixels(pixels);
        surface.Apply(false, true);
        // Size the sprite through its pixel density and pivot, without overriding
        // vertices outside its texture rectangle. Preserve the source local bounds.
        float pixelsPerUnit = size / Mathf.Max(bounds.size.x, bounds.size.y);
        var spriteRect = new Rect(0, 0,
            bounds.size.x * pixelsPerUnit, bounds.size.y * pixelsPerUnit);
        var pivot = new Vector2(-bounds.min.x / bounds.size.x,
            -bounds.min.y / bounds.size.y);
        paddleSprite = Sprite.Create(surface, spriteRect, pivot,
            pixelsPerUnit, 0, SpriteMeshType.FullRect);
        paddleSprite.name = "After paddle surface";
        visual.sprite = paddleSprite;
    }

    private void OnDestroy()
    {
        if (paddleSprite != null) Destroy(paddleSprite);
        if (surface != null) Destroy(surface);
    }
}
