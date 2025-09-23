using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponUseUtils
{
    public static IEnumerator AttackCoroutine(CommonBase userGameObject, SItemStack itemWithState, SItemWeaponTypeSO data)
    {
        // 휘두르는 애니메이션
        // 플레이어 행동 사이클타임
        // 플레이어 클릭 방향 감지
        // 공격 범위 생성
        // 내구도 닳기
        // 아이템 인벤토리 업데이트

        // 모션 시작
        // 여기 애니메이션 코드 넣기
        yield return new WaitForSeconds(data.weaponAnimation);
        // 모션 종료

        // 플레이어 클릭 방향
        Ray m_rayFromMouse = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit m_hitOnMap;
        Vector3 direction;
        LayerMask mapMaskLayer = LayerMask.NameToLayer("MouseClickArea");

        // 공격 범위 생성
        if (Physics.Raycast(
            m_rayFromMouse.origin,
            m_rayFromMouse.direction,
            out m_hitOnMap,
            maxDistance: 200.0f,
            mapMaskLayer))
        {
            m_hitOnMap.point = new Vector3(
                m_hitOnMap.point.x,
                userGameObject.transform.position.y,
                m_hitOnMap.point.z);

            // ... RayCastHit m_hitOnMap의 정보를 활용한 코드
            direction = (m_hitOnMap.point - userGameObject.transform.position).normalized;
        }




        // 내구도 닳기
        itemWithState.duability -= data.duabilityRedutionPerHit;

        // 인벤토리 업데이트 
        InventoryUiMain.instance.IconRefresh();

        // 공격 딜레이
        yield return new WaitForSeconds(data.weaponDelay);
        //


    }
}
