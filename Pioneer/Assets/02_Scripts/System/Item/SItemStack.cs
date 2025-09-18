using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SItemStack
{
    public int id;
    public int amount;
    public float duability; // 내구도

    public SItemStack(int id, int amount)
    {
        this.id = id;
        this.amount = amount;
    }
    public static bool IsEmpty(SItemStack target)
    {
        if (target == null) return true;
        if (target.id == 0) return true;
        return false;
    }
    public SItemStack Copy(SItemStack source)
    {
        return new SItemStack(source.id, source.amount);
    }

    // 아이템이 있는 곳에서는 종류와 갯수를 가리키는 것과 어떤 아이템이 쓰이는지 강하게 연결되어있을 것으로 보입니다.
    public virtual void Use()
    {

    }
}

