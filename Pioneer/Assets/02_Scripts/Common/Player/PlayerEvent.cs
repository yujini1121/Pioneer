using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEvent : MonoBehaviour, IBegin
{
    public static PlayerEvent instance;

    // 공격 받았을 때 호출될 이벤트
    public event System.Action OnDamaged;

    // 몬스터에게 공격받는 함수 (예시)
    public void TakeDamage(int amount)
    {
        Debug.Log($"플레이어가 {amount} 데미지를 받았습니다.");

        // 데미지 처리 로직...

        // 구독자에게 알림
        OnDamaged();
    }


    private void Awake()
    {
        instance = this;
        OnDamaged += () => { };
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
