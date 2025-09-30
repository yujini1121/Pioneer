using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SItemStack
{
    public int id;
    public int amount;
    public int duability; // ������
    public bool isCanStack;
    public bool isUseCoroutineEnd = true; // �ش� Ŭ������ �������� ����. SItemTypeSO �� �ܺ� Ŭ������ ����

    public virtual int GetID() => id;
    public virtual bool IsCanStack() => isCanStack;

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

    // �������� �ִ� �������� ������ ������ ����Ű�� �Ͱ� � �������� ���̴��� ���ϰ� ����Ǿ����� ������ ���Դϴ�.
    public virtual void Use()
    {
        // ���� �������� ���

        // 
    }
}

