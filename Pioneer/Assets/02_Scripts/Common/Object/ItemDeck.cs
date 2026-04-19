using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class ItemDeck : StructureBase
{
    public override bool IsInteractionTarget => false;
    [Header("파괴 옵션")]
    [SerializeField] private bool canBreak = true;
    [Header("번개 경고 색상")]
    [SerializeField] private Color thunderWarningColor = Color.red;
    [SerializeField] private MeshRenderer[] warningRenderers;
    private bool isHitByThunder = false;
    private Coroutine warningCoroutine;
    private Color[] originColors;
    private bool isCached = false;

    [Header("상부 설치 오브젝트 정리")]
    [SerializeField] private Vector3 installedObjectCheckHalfExtents = new Vector3(0.9f, 2f, 0.9f);
    [SerializeField] private Vector3 installedObjectCheckCenterOffset = new Vector3(0f, 1.2f, 0f);
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

    private void DestroyInstalledObjectsOnTop()
    {
        Vector3 center = transform.position + installedObjectCheckCenterOffset;

        Collider[] hits = Physics.OverlapBox(
            center,
            installedObjectCheckHalfExtents,
            transform.rotation,
            ~0,
            QueryTriggerInteraction.Collide
        );

        HashSet<InstalledObject> targets = new HashSet<InstalledObject>();

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null) continue;

            InstalledObject installed = hits[i].GetComponentInParent<InstalledObject>();
            if (installed == null) continue;
            if (installed.gameObject == gameObject) continue;

            targets.Add(installed);
        }

        foreach (InstalledObject installed in targets)
        {
            if (installed == null) continue;
            Destroy(installed.gameObject);
        }
    }

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
        DestroyInstalledObjectsOnTop();

        if (ItemDeckDisconnect.instance != null)
            ItemDeckDisconnect.instance.RemoveDeck(gameObject);

        base.WhenDestroy();
    }
}
