
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TreasureBoxManager : MonoBehaviour
{
    public static TreasureBoxManager instance;

    [SerializeField] RandomBox[] reward;
    List<SItemStack> rewardStack;

    public void GetBox() => GrantReward(false);

    public void GetSpecialBox() => GrantReward(true);

    private void GrantReward(bool special)
    {
        UtilityFunctions.Log(">> TreasureBoxManager : 보상 받음");
        SItemStack item = GetReward(special);
        if (item == null || InventoryManager.Instance == null) return;
        // Overflow follows the existing inventory rule: drop nearby, never discard the reward.
        InventoryManager.Instance.Add(item.Copy());
        if (TreasureBoxUI.instance != null)
        {
            rewardStack.Add(item.Copy());
            if (rewardStack.Count == 1) TreasureBoxUI.instance.ShowItem(rewardStack[0]);
        }
        AudioManager.instance?.PlaySfx(AudioManager.SFX.OpenBox);
        if (CreatureEffect.Instance != null && PlayerCore.Instance != null)
            CreatureEffect.Instance.PlayEffect(CreatureEffect.Instance.GetEffect(9),
                PlayerCore.Instance.transform.position + Vector3.up * 1.5f);
    }

    public SItemStack GetReward() => GetReward(false);

    private SItemStack GetReward(bool special)
    {
        if (reward == null || reward.Length == 0) return null;
        float total = 0f;
        foreach (RandomBox entry in reward)
            if (entry != null && entry.reward != null && entry.weight > 0f
                && (!special || entry.reward.id != 30001)) total += entry.weight;
        if (total <= 0f) return special ? GetReward(false) : null;
        float roll = Random.Range(0f, total);
        SItemStack fallback = null;
        foreach (RandomBox entry in reward)
        {
            if (entry == null || entry.reward == null || entry.weight <= 0f
                || (special && entry.reward.id == 30001)) continue;
            fallback = entry.reward;
            roll -= entry.weight;
            if (roll <= 0f) return entry.reward.Copy();
        }
        return fallback?.Copy();
    }

    // Both existing serialized buttons now only dismiss an already granted reward.
    public void Accept()
    {
        if (rewardStack.Count == 0) return;
        rewardStack.RemoveAt(0);
        if (TreasureBoxUI.instance == null) return;
        if (rewardStack.Count > 0) TreasureBoxUI.instance.ShowItem(rewardStack[0]);
        else TreasureBoxUI.instance.CloseWindow();
    }

    public void Deny() => Accept();

    private void Awake()
    {
        instance = this;
        rewardStack = new List<SItemStack>();
    }

    // Start is called before the first frame update


    // Update is called once per frame

}

[System.Serializable]
public class RandomBox
{
    public SItemStack reward;
    public float weight;
}