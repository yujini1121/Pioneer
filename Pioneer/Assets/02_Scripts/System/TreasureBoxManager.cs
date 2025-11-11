
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TreasureBoxManager : MonoBehaviour
{
    public static TreasureBoxManager instance;

    [SerializeField] RandomBox[] reward;
    List<SItemStack> rewardStack;

    public void GetBox()
    {
        Debug.Log(">> TreasureBoxManager : 보상 받음");

        SItemStack r = GetReward();
        rewardStack.Add(r);

        if (rewardStack.Count == 1)
        {
            TreasureBoxUI.instance.ShowItem(r);
        }

    }

    public SItemStack GetReward()
    {
        float max = 0.0f;
        foreach (RandomBox one in reward) max += one.weight;
        float value = Random.Range(0, max);

        for (int index = 0; index < reward.Length - 1; ++index)
        {
            if (value <= reward[index].weight)
            {
                return reward[index].reward;
            }
            value -= reward[index].weight;
        }
        return reward[reward.Length - 1].reward;
    }

    public void Accept()
    {
        InventoryManager.Instance.Add(rewardStack[0]);

        rewardStack.RemoveAt(0);
        if (rewardStack.Count > 0)
        {
            TreasureBoxUI.instance.ShowItem(rewardStack[0]);
        }
        else
        {
            TreasureBoxUI.instance.CloseWindow();
        }
    }

    public void Deny()
    {
        rewardStack.RemoveAt(0);
        if (rewardStack.Count > 0)
        {
            TreasureBoxUI.instance.ShowItem(rewardStack[0]);
        }
        else
        {
            TreasureBoxUI.instance.CloseWindow();
        }
    }

    private void Awake()
    {
        instance = this;
        rewardStack = new List<SItemStack>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

[System.Serializable]
public class RandomBox
{
    public SItemStack reward;
    public float weight;
}