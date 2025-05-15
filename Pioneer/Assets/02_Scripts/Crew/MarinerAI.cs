using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarinerAI : MonoBehaviour
{
    public int marinerId;
    public bool isRepairing = false;
    private DefenseObject targetRepairObject;
    private int repairAmount = 30;

    private bool isSecondPriorityStarted = false; 

    private void Update()
    {
        if (!isRepairing && GameManager.Instance.IsDaytime)
        {
            StartRepair();
        }
    }

    private void StartRepair() // 1순위 행동: HP가 50% 이하인 설치형 오브젝트 수리 (3초 동안 수리)
    {
        List<DefenseObject> needRepairList = GameManager.Instance.GetRepairTargetsNeedingRepair();

        if (needRepairList.Count > 0)
        {
            targetRepairObject = needRepairList[0];
            Debug.Log($"[Mariner {marinerId}] 수리할 오브젝트 선택: {targetRepairObject.name}, 현재 HP: {targetRepairObject.currentHP}/{targetRepairObject.maxHP}");

            if (GameManager.Instance.CanMarinerRepair(marinerId, targetRepairObject))
            {
                Debug.Log($"[Mariner {marinerId}] 수리 시작: {targetRepairObject.name}");
                isRepairing = true;
                StartCoroutine(RepairProcess());
            }
            else
            {
                Debug.Log($"[Mariner {marinerId}] 수리망치가 없습니다");
            }
        }
        else
        {
            if (!isSecondPriorityStarted)
            {
                Debug.Log($"[Mariner {marinerId}] 수리할 오브젝트가 없음으로 2순위 행동 시작");
                isSecondPriorityStarted = true;
                StartCoroutine(StartSecondPriorityAction());

            }

        }
    }

    private IEnumerator RepairProcess()
    {
        float repairDuration = 3f;
        float elapsedTime = 0f;

        while (elapsedTime < repairDuration)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Debug.Log($"[Mariner {marinerId}] 수리 중 입니다 : {targetRepairObject.name}/ 수리량: {repairAmount}");
        targetRepairObject.Repair(repairAmount);

        isRepairing = false;

        GameManager.Instance.UpdateRepairTargets();
        StartRepair();
    }

    private IEnumerator StartSecondPriorityAction() // 2순위 행동: 바다 쓰레기 파밍 (10초 동안의 행동 후, 획득)
    {
        Debug.Log($"[Mariner {marinerId}] 2순위 행동으로 쓰레기 파밍 시작");

        yield return new WaitForSeconds(10f);

        Debug.Log($"[Mariner {marinerId}] 파밍 완료 후 쓰레기 획득");

        var needRepairList = GameManager.Instance.GetRepairTargetsNeedingRepair();

        if (needRepairList.Count > 0)
        {
            Debug.Log($"[Mariner {marinerId}] 수리 대상 발견 시 수리를 진행");
            isSecondPriorityStarted = false;
            StartRepair();
        }
        else
        {
            Debug.Log($"[Mariner {marinerId}] 수리 대상 없을 시 다시 쓰레기 파밍");
            StartCoroutine(StartSecondPriorityAction());
        }
    }
}
