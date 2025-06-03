using System.Collections;
using UnityEngine;

public class SeaHarvestInteraction : MonoBehaviour
{
    [Header("채취 설정")]
    public float harvestTime = 5f;
    public LayerMask seaLayer;
    public JH_PlayerMovement playerMovement;

    private Coroutine harvestCoroutine;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 100f, seaLayer))
            {
                harvestCoroutine = StartCoroutine(HarvestRoutine());
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            CancelHarvest();
        }
    }

    IEnumerator HarvestRoutine()
    {
        float timer = 0f;

        while (timer < harvestTime)
        {
            if (playerMovement.HasMoved())
            {
                //Debug.Log("채취 실패: 움직임 감지");
                yield break;
            }

            if (!Input.GetMouseButton(0))
            {
                yield break; // 실패 아님, 클릭만 중단
            }

            timer += Time.deltaTime;
            yield return null;
        }

        //Debug.Log("채취 성공: 나무 +1");
        ResourceManager.Instance.AddResource(ResourceType.Wood, 1);
    }

    void CancelHarvest()
    {
        if (harvestCoroutine != null)
        {
            StopCoroutine(harvestCoroutine);
            harvestCoroutine = null;
            Debug.Log("클릭 취소됨");
        }
    }
}
