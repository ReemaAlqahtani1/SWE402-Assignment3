using UnityEngine;
using UnityEngine.UI;

// Vector UI heart: does not depend on a font containing the heart glyph.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class HeartIconGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = GetPixelAdjustedRect();
        const int segments = 64;
        mesh.AddVert(rect.center, color, Vector2.zero);
        for (int i = 0; i < segments; i++)
        {
            float angle = i * 2f * Mathf.PI / segments;
            float x = 16f * Mathf.Pow(Mathf.Sin(angle), 3f);
            float y = 13f * Mathf.Cos(angle) - 5f * Mathf.Cos(2f * angle)
                - 2f * Mathf.Cos(3f * angle) - Mathf.Cos(4f * angle);
            Vector2 point = new Vector2(rect.center.x + x / 34f * rect.width,
                rect.center.y + (y + 2f) / 34f * rect.height);
            mesh.AddVert(point, color, Vector2.zero);
        }
        for (int i = 0; i < segments; i++)
            mesh.AddTriangle(0, i + 1, (i + 1) % segments + 1);
    }
}
