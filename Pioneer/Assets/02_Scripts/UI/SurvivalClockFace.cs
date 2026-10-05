using UnityEngine;
using UnityEngine.UI;

public class SurvivalClockFace : MaskableGraphic
{
    [SerializeField] private bool daytime = true;
    [SerializeField, Range(0f, 1f)] private float progress;
    public float Progress => progress;
    public void SetPhase(bool day, float value)
    {
        value = Mathf.Clamp01(value);
        if (daytime == day && Mathf.Approximately(progress, value)) return;
        daytime = day; progress = value; SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        Vector2 center = r.center;
        float radius = Mathf.Min(r.width, r.height) * 0.46f;
        Color tint = daytime ? new Color(0.88f, 0.75f, 0.48f) : new Color(0.57f, 0.73f, 0.81f);
        Ring(vh, center, radius, radius - 2.5f, 1f, new Color(tint.r, tint.g, tint.b, 0.22f));
        Ring(vh, center, radius, radius - 2.5f, progress, tint);
        float inner = radius * 0.32f;
        if (daytime)
        {
            Ring(vh, center, inner, 0f, 1f, tint);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Vector2 n = new Vector2(-d.y, d.x) * 1.2f;
                Quad(vh, center + d * inner * 1.35f - n, center + d * inner * 1.9f - n,
                    center + d * inner * 1.9f + n, center + d * inner * 1.35f + n, tint);
            }
        }
        else
        {
            for (int i = 0; i < 32; i++)
            {
                float a = -Mathf.PI / 2f + Mathf.PI * i / 32f;
                float b = -Mathf.PI / 2f + Mathf.PI * (i + 1) / 32f;
                float size = inner * 1.75f;
                Quad(vh, center + new Vector2(-Mathf.Cos(a), Mathf.Sin(a)) * size,
                    center + new Vector2(-Mathf.Cos(b), Mathf.Sin(b)) * size,
                    center + new Vector2(-0.28f * Mathf.Cos(b), Mathf.Sin(b)) * size,
                    center + new Vector2(-0.28f * Mathf.Cos(a), Mathf.Sin(a)) * size, tint);
            }
        }
    }

    private static void Ring(VertexHelper vh, Vector2 center, float outer, float inner, float fraction, Color tint)
    {
        int steps = Mathf.CeilToInt(96 * fraction);
        for (int i = 0; i < steps; i++)
        {
            float a = Mathf.PI / 2f - Mathf.PI * 2f * i / 96f;
            float b = Mathf.PI / 2f - Mathf.PI * 2f * Mathf.Min((i + 1) / 96f, fraction);
            Vector2 da = new Vector2(Mathf.Cos(a), Mathf.Sin(a)), db = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
            Quad(vh, center + da * outer, center + db * outer, center + db * inner, center + da * inner, tint);
        }
    }

    private static void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color tint)
    {
        int n = vh.currentVertCount;
        vh.AddVert(a, tint, Vector2.zero); vh.AddVert(b, tint, Vector2.zero);
        vh.AddVert(c, tint, Vector2.zero); vh.AddVert(d, tint, Vector2.zero);
        vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n, n + 2, n + 3);
    }
}
