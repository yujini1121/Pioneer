using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class StructureHitFeedback : MonoBehaviour
{
    private sealed class Surface
    {
        public Renderer renderer;
        public int slot;
        public int colorId;
        public Color color;
        public MaterialPropertyBlock original;
        public MaterialPropertyBlock working;
    }
    private readonly List<Surface> surfaces = new List<Surface>();
    private Tween flash;

    public static void Play(GameObject target, float strength = 0.55f)
    {
        if (!target.activeInHierarchy || (GameManager.Instance != null && GameManager.Instance.IsGameResultActive)) return;
        var effect = target.GetComponent<StructureHitFeedback>() ?? target.AddComponent<StructureHitFeedback>();
        effect.PlayFlash(strength);
    }

    private void PlayFlash(float strength)
    {
        flash?.Kill(); Restore(); surfaces.Clear();
        foreach (var renderer in GetComponentsInChildren<MeshRenderer>())
        {
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                var material = materials[i];
                if (material == null) continue;
                int id = Shader.PropertyToID(material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color");
                if (!material.HasProperty(id)) continue;
                var original = new MaterialPropertyBlock();
                var working = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(original, i);
                renderer.GetPropertyBlock(working, i);
                surfaces.Add(new Surface { renderer = renderer, slot = i, colorId = id,
                    color = original.HasProperty(id) ? original.GetColor(id) : material.GetColor(id),
                    original = original, working = working });
            }
        }
        Apply(strength);
        flash = DOVirtual.Float(strength, 0f, 0.2f, Apply).SetUpdate(false).OnKill(Restore);
    }

    private void Apply(float amount)
    {
        foreach (var s in surfaces)
        {
            if (s.renderer == null) continue;
            Color tint = Color.Lerp(s.color, new Color(1f, 0.50f, 0.28f), amount); tint.a = s.color.a;
            s.working.SetColor(s.colorId, tint);
            s.renderer.SetPropertyBlock(s.working, s.slot);
        }
    }
    private void Restore()
    {
        foreach (var s in surfaces)
            if (s.renderer != null) s.renderer.SetPropertyBlock(s.original, s.slot);
    }
    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameResultActive)
        { flash?.Kill(); flash = null; Restore(); }
    }
    private void OnDisable() { flash?.Kill(); flash = null; Restore(); }
}
