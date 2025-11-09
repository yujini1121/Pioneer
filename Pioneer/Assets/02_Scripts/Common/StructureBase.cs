using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

/// <summary>
/// 구조체만 들어갈 수 있는 기능이 생길 수도 있으니까 일단 넣어둠, 근데 일단 지금은 비어있긴 함 ㅜㅜ
/// </summary>
public class StructureBase : CommonBase
{
    [field: SerializeField]
    public bool isUsing { get; private set; }


    void Start()
    {
        
    }

    public void Repair()
    {
        // 힐링
    }



    #region 상호작용 가능한 오브젝트만 사용할 것
    public virtual void Interactive()
    {

    }

    public virtual void Use()
    {
        // 사용했을 때 로직


        isUsing = true;
    }

    public virtual void UnUse()
    {
        // 사용 해제했을 때 로직

        isUsing = false;
    }
    #endregion
}