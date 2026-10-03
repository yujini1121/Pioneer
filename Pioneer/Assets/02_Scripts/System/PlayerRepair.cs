using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerRepair : MonoBehaviour
{
    public static PlayerRepair instance;

    [SerializeField] private GameObject circuleBack;
    [SerializeField] Image ringImage;
    // [SerializeField] GameObject effect;
    [SerializeField] private float repairTime = 3f;
    bool isAction = false;
    //private GameObject 

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }



    // Start is called before the first frame update
    void Start()
    {
        // effect.SetActive(false);    
    }

    // Update is called once per frame


    public void Repair(StructureBase target)
    {
        if (isAction || target == null || target.IsDead || RepairSystem.instance == null
            || RepairSystem.instance.remainRepairCount <= 0) return;

        UtilityFunctions.Log($"수리 버튼 눌림");

        StartCoroutine(RepairCoroutine(target));
    }

    IEnumerator RepairCoroutine(StructureBase target)
    {
        isAction = true;

        // effect.SetActive(true);
        circuleBack.SetActive(true);
        ringImage.enabled = true;

        for (float t = 0f; t < repairTime; t += Time.deltaTime)
        {
            ringImage.fillAmount = t / repairTime;
            yield return null;
        }

        // effect.SetActive(false);
        circuleBack.SetActive(false);
        ringImage.enabled = false;
        isAction = false;

        if (target == null || target.IsDead || RepairSystem.instance == null
            || RepairSystem.instance.remainRepairCount <= 0) yield break;

        target.Heal(target.maxHp);
        RepairSystem.instance.remainRepairCount--;
        //InventoryManager.Instance.Remove(new SItemStack(40007, 1));
    }
}
