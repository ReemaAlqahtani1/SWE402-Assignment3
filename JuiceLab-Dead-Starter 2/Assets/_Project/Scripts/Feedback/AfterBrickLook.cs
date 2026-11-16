using System.Collections;
using UnityEngine;

// After-only surface treatment. Same sprite bounds, transforms and colliders.
public sealed class AfterBrickLook : MonoBehaviour
{
    private Texture2D surface;
    private Sprite brickSprite;

    private IEnumerator Start()
    {
        // Let the unchanged LevelBuilder finish generating the board first.
        yield return null;
        const int size = 128;
        surface = new Texture2D(size, size, TextureFormat.RGBA32, false);
        surface.name = "Bevelled brick surface";
        surface.wrapMode = TextureWrapMode.Clamp;
        surface.filterMode = FilterMode.Bilinear;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float light = Mathf.Lerp(0.70f, 0.94f, (float)y / (size - 1));
            // Small deterministic grain; never consumes the game's random stream.
            light += Mathf.Sin(x * 17.13f + y * 31.71f) * 0.018f;
            if (x < 4 || x >= size - 4) light *= 0.65f;
            if (y < 12) light *= 0.52f;
            if (y >= size - 10) light = 1f;
            if (y == size - 11) light = 0.72f;
            int cornerX = Mathf.Min(x, size - 1 - x);
            int cornerY = Mathf.Min(y, size - 1 - y);
            float alpha = cornerX < 3 && cornerY < 7 ? 0f : 1f;
            pixels[y * size + x] = new Color(light, light, light, alpha);
        }
        surface.SetPixels(pixels);
        surface.Apply(false, true);
        brickSprite = Sprite.Create(surface, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        brickSprite.name = "After bevelled brick";
        foreach (var brick in FindObjectsByType<Brick>(FindObjectsSortMode.None))
        {
            if (brick.gameObject.scene != gameObject.scene) continue;
            var renderer = brick.GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.sprite = brickSprite;
        }
    }

    private void OnDestroy()
    {
        if (brickSprite != null) Destroy(brickSprite);
        if (surface != null) Destroy(surface);
    }
}
