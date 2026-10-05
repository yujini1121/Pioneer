using UnityEngine;
using UnityEngine.UI;

public class SlotTypeBorder : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        const float width = 2f;
        Add(vh, new Rect(r.xMin, r.yMin, r.width, width));
        Add(vh, new Rect(r.xMin, r.yMax - width, r.width, width));
        Add(vh, new Rect(r.xMin, r.yMin + width, width, r.height - 2 * width));
        Add(vh, new Rect(r.xMax - width, r.yMin + width, width, r.height - 2 * width));
    }
    private void Add(VertexHelper vh, Rect r)
    {
        int n = vh.currentVertCount;
        vh.AddVert(new Vector2(r.xMin, r.yMin), color, Vector2.zero);
        vh.AddVert(new Vector2(r.xMin, r.yMax), color, Vector2.zero);
        vh.AddVert(new Vector2(r.xMax, r.yMax), color, Vector2.zero);
        vh.AddVert(new Vector2(r.xMax, r.yMin), color, Vector2.zero);
        vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n, n + 2, n + 3);
    }
}
