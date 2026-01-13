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
    void Update()
    {
        
    }

    public void Repair(StructureBase target)
    {
        if (isAction) return;

        Debug.Log($"수리 버튼 눌림");

        StartCoroutine(RepairCoroutine(target));
    }

    IEnumerator RepairCoroutine(StructureBase target)
    {
        Debug.Log($"수리 버튼 눌림");

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
        target.Heal(target.maxHp);

        isAction = false;
        InventoryManager.Instance.Remove(new SItemStack(40007, 1));
    }
}
