using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class WeaponUseUtils
{
    //public static IEnumerator AttackCoroutine(CommonBase userGameObject, SItemStack itemWithState, SItemWeaponTypeSO data)
    //{
    //    // 휘두르는 애니메이션
    //    // 플레이어 행동 사이클타임
    //    // 플레이어 클릭 방향 감지
    //    // 공격 범위 생성
    //    // 내구도 닳기
    //    // 아이템 인벤토리 업데이트
    //
    //    // 모션 시작
    //    // 여기 애니메이션 코드 넣기
    //    yield return new WaitForSeconds(data.weaponAnimation);
    //    // 모션 종료
    //
    //    // 플레이어 클릭 방향
    //    Ray m_rayFromMouse = Camera.main.ScreenPointToRay(Input.mousePosition);
    //    RaycastHit m_hitOnMap;
    //    Vector3 direction;
    //    LayerMask mapMaskLayer = LayerMask.NameToLayer("MouseClickArea");
    //
    //    // 공격 범위 생성
    //    if (Physics.Raycast(
    //        m_rayFromMouse.origin,
    //        m_rayFromMouse.direction,
    //        out m_hitOnMap,
    //        maxDistance: 200.0f,
    //        mapMaskLayer))
    //    {
    //        m_hitOnMap.point = new Vector3(
    //            m_hitOnMap.point.x,
    //            userGameObject.transform.position.y,
    //            m_hitOnMap.point.z);
    //
    //        // ... RayCastHit m_hitOnMap의 정보를 활용한 코드
    //        direction = (m_hitOnMap.point - userGameObject.transform.position).normalized;
    //    }




    //    // 내구도 닳기
    //    itemWithState.duability -= data.duabilityRedutionPerHit;

    //    // 인벤토리 업데이트 
    //    InventoryUiMain.instance.IconRefresh();

    //    // 공격 딜레이
    //    yield return new WaitForSeconds(data.weaponDelay);
    //    //

    //}

    public static IEnumerator AttackCoroutine(CommonBase userGameObject, SItemStack itemWithState, SItemWeaponTypeSO data)
    {
        Debug.Log($">> WeaponUseUtils.AttackCoroutine : 함수 호출됨 내구도 닳기 : {data.duabilityRedutionPerHit}");
        Debug.Assert(itemWithState != null);
        Debug.Assert(data != null);

        // 휘두르는 애니메이션
        // 플레이어 행동 사이클타임
        // 플레이어 클릭 방향 감지
        // 공격 범위 생성
        // 내구도 닳기
        // 아이템 인벤토리 업데이트

        // 플레이어 이동 제한
        float originalSpeed = PlayerCore.Instance.speed;

        try
        {
            PlayerCore.Instance.speed = 0f;

            Debug.Log($"플레이어 이동 멈춤 : {PlayerCore.Instance.speed}");
            // 모션 시작

            // 플레이어 클릭 방향
            Ray m_rayFromMouse = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit m_hitOnMap;
            Vector3 direction;
            LayerMask mapMaskLayer = LayerMask.NameToLayer("MouseClickArea");

            // 공격 범위 생성
            if (Physics.Raycast(m_rayFromMouse, out m_hitOnMap, Mathf.Infinity))
            {
                Debug.Log($">> WeaponUseUtils.AttackCoroutine : 내구도 닳기 : {data.duabilityRedutionPerHit}");
                //data.weaponRange;
                Vector3 dir = (m_hitOnMap.point - userGameObject.transform.position).normalized;
                dir.y = 0f;
                userGameObject.transform.rotation = Quaternion.LookRotation(dir);

                Vector3 position = userGameObject.transform.position + dir * 0.5f;
                position.y = PlayerCore.Instance.AttackHeight;
                PlayerCore.Instance.PlayerAttack.transform.position = position;
                PlayerCore.Instance.PlayerAttack.transform.rotation = Quaternion.LookRotation(dir);
                PlayerCore.Instance.PlayerAttack.EnableAttackCollider();
                PlayerCore.Instance.PlayerAttack.damage = (int)(data.weaponDamage + PlayerCore.Instance.CalculatedHandAttack.weaponDamage);
                // 공격 범위 세팅
                PlayerCore.Instance.PlayerAttack.SetAttackRange(data.weaponRange);

                // 여기 애니메이션 코드 넣기
                yield return new WaitForSeconds(data.weaponAnimation);
                // 모션 종료


                // 내구도 닳기
                itemWithState.duability = Mathf.Max(0, itemWithState.duability -
                    Mathf.Max(0, data.duabilityRedutionPerHit - PlayerCore.Instance.DuabilityReducePrevent));

                PlayerCore.Instance.PlayerAttack.DisableAttackCollider();
                PlayerCore.Instance.PlayerAttack.SetAttackRange(0.1f);
                // 인벤토리 업데이트 
                InventoryUiMain.instance.IconRefresh();

                // ... RayCastHit m_hitOnMap의 정보를 활용한 코드
                direction = (m_hitOnMap.point - userGameObject.transform.position).normalized;
            }

            // 인벤토리 업데이트 
            InventoryUiMain.instance.IconRefresh();
		    
        }
		finally
        {
            // 플레이어 속도 복구
            PlayerCore.Instance.speed = originalSpeed;
        }
        
        // 공격 딜레이
        yield return new WaitForSeconds(data.weaponDelay);
    }
}
