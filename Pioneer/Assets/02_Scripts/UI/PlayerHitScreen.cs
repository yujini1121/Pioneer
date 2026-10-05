using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHitScreen : MaskableGraphic
{
    private Sequence feedback;
    public void Play(int damage, int maxHp)
    {
        if (!isActiveAndEnabled || damage <= 0 || (GameManager.Instance != null && GameManager.Instance.IsGameResultActive)) return;
        feedback?.Kill();
        float intensity = Mathf.Lerp(0.13f, 0.26f, Mathf.Clamp01(damage / (float)Mathf.Max(1, maxHp) * 4f));
        feedback = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDisable)
            .Append(this.DOFade(intensity, 0.06f)).Append(this.DOFade(0f, 0.28f));
    }
    protected override void OnDisable()
    {
        feedback?.Kill(); feedback = null;
        color = new Color(0.65f, 0.12f, 0.08f, 0f);
        base.OnDisable();
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        Vector2[] outer = { new Vector2(r.xMin, r.yMin), new Vector2(r.xMin, r.yMax),
            new Vector2(r.xMax, r.yMax), new Vector2(r.xMax, r.yMin) };
        Color clear = new Color(color.r, color.g, color.b, 0f);
        for (int i = 0; i < 4; i++)
        {
            int next = (i + 1) % 4, n = vh.currentVertCount;
            vh.AddVert(outer[i], color, Vector2.zero); vh.AddVert(outer[next], color, Vector2.zero);
            vh.AddVert(Vector2.Lerp(outer[next], r.center, 0.22f), clear, Vector2.zero);
            vh.AddVert(Vector2.Lerp(outer[i], r.center, 0.22f), clear, Vector2.zero);
            vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n, n + 2, n + 3);
        }
    }
}
