using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemDeck : StructureBase
{
    [Header("°©ÆÇ ¼³Á¤")]
    [SerializeField] private bool canBreak = true;

    [Header("³ú¿ì ¿¹°í ¼³Á¤")]
    [SerializeField] private Color thunderWarningColor = Color.red;
    [SerializeField] private MeshRenderer[] warningRenderers;

    private bool isHitByThunder = false;

    private Coroutine warningCoroutine;
    private Color[] originColors;
    private bool isCached = false;

    private void CacheDefaultState()
    {
        if (isCached) return;

        if (warningRenderers == null || warningRenderers.Length == 0)
            warningRenderers = GetComponentsInChildren<MeshRenderer>();

        originColors = new Color[warningRenderers.Length];
        for (int i = 0; i < warningRenderers.Length; i++)
        {
            if (warningRenderers[i] == null) continue;
            originColors[i] = warningRenderers[i].material.color;
        }

        isCached = true;
    }

    public void BeginThunderWarning(float duration)
    {
        CacheDefaultState();

        if (warningCoroutine != null)
            StopCoroutine(warningCoroutine);

        warningCoroutine = StartCoroutine(ThunderWarningRoutine(duration));
    }

    private IEnumerator ThunderWarningRoutine(float duration)
    {
        for (int i = 0; i < warningRenderers.Length; i++)
        {
            if (warningRenderers[i] == null) continue;
            warningRenderers[i].material.color = thunderWarningColor;
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);

            yield return null;
        }

        warningCoroutine = null;
    }

    public void EndThunderWarning()
    {
        CacheDefaultState();

        if (warningCoroutine != null)
        {
            StopCoroutine(warningCoroutine);
            warningCoroutine = null;
        }

        for (int i = 0; i < warningRenderers.Length; i++)
        {
            if (warningRenderers[i] == null) continue;
            warningRenderers[i].material.color = originColors[i];
        }
    }

    // °©ÆÇ : ³ú¿ì ¸ÂÀ¸¸é Áï½Ã ÆÄ±«
    public void DestroyByThunder()
    {
        if (IsDead) return;
        if (isHitByThunder) return;

        IsDead = true;
        isHitByThunder = true;

        if (canBreak)
        {
            hp = 0;
            WhenDestroy();
        }
    }

    public override void WhenDestroy()
    {
        EndThunderWarning();

        // °©ÆÇ ÆÄ±« Ã³¸®
        base.WhenDestroy();
    }
}