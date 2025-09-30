using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class WeaponUseUtils
{
    //public static IEnumerator AttackCoroutine(CommonBase userGameObject, SItemStack itemWithState, SItemWeaponTypeSO data)
    //{
    //    // �ֵθ��� �ִϸ��̼�
    //    // �÷��̾� �ൿ ����ŬŸ��
    //    // �÷��̾� Ŭ�� ���� ����
    //    // ���� ���� ����
    //    // ������ ���
    //    // ������ �κ��丮 ������Ʈ
    //
    //    // ��� ����
    //    // ���� �ִϸ��̼� �ڵ� �ֱ�
    //    yield return new WaitForSeconds(data.weaponAnimation);
    //    // ��� ����
    //
    //    // �÷��̾� Ŭ�� ����
    //    Ray m_rayFromMouse = Camera.main.ScreenPointToRay(Input.mousePosition);
    //    RaycastHit m_hitOnMap;
    //    Vector3 direction;
    //    LayerMask mapMaskLayer = LayerMask.NameToLayer("MouseClickArea");
    //
    //    // ���� ���� ����
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
    //        // ... RayCastHit m_hitOnMap�� ������ Ȱ���� �ڵ�
    //        direction = (m_hitOnMap.point - userGameObject.transform.position).normalized;
    //    }




    //    // ������ ���
    //    itemWithState.duability -= data.duabilityRedutionPerHit;

    //    // �κ��丮 ������Ʈ 
    //    InventoryUiMain.instance.IconRefresh();

    //    // ���� ������
    //    yield return new WaitForSeconds(data.weaponDelay);
    //    //

    //}

    public static IEnumerator AttackCoroutine(CommonBase userGameObject, SItemStack itemWithState, SItemWeaponTypeSO data)
    {
        Debug.Log($">> WeaponUseUtils.AttackCoroutine : �Լ� ȣ��� ������ ��� : {data.duabilityRedutionPerHit}");
        Debug.Assert(itemWithState != null);
        Debug.Assert(data != null);

        // �ֵθ��� �ִϸ��̼�
        // �÷��̾� �ൿ ����ŬŸ��
        // �÷��̾� Ŭ�� ���� ����
        // ���� ���� ����
        // ������ ���
        // ������ �κ��丮 ������Ʈ

        // ��� ����
        // ���� �ִϸ��̼� �ڵ� �ֱ�
        yield return new WaitForSeconds(data.weaponAnimation);
        // ��� ����

        // �÷��̾� Ŭ�� ����
        Ray m_rayFromMouse = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit m_hitOnMap;
        Vector3 direction;
        LayerMask mapMaskLayer = LayerMask.NameToLayer("MouseClickArea");

        // ���� ���� ����
        if (Physics.Raycast(m_rayFromMouse, out m_hitOnMap, Mathf.Infinity))
        {
            Debug.Log($">> WeaponUseUtils.AttackCoroutine : ������ ��� : {data.duabilityRedutionPerHit}");

            Vector3 dir = (m_hitOnMap.point - userGameObject.transform.position).normalized;
            dir.y = 0f;
            userGameObject.transform.rotation = Quaternion.LookRotation(dir);

            Vector3 position = userGameObject.transform.position + dir * 0.5f;
            position.y = PlayerCore.Instance.AttackHeight;
            PlayerCore.Instance.PlayerAttack.transform.position = position;
            PlayerCore.Instance.PlayerAttack.transform.rotation = Quaternion.LookRotation(dir);
            PlayerCore.Instance.PlayerAttack.EnableAttackCollider();
            PlayerCore.Instance.PlayerAttack.damage = (int)(data.weaponDamage);
            
            // ������ ���
            itemWithState.duability -= data.duabilityRedutionPerHit;

            PlayerCore.Instance.PlayerAttack.DisableAttackCollider();
            // �κ��丮 ������Ʈ 
            InventoryUiMain.instance.IconRefresh();

            // ... RayCastHit m_hitOnMap�� ������ Ȱ���� �ڵ�
            direction = (m_hitOnMap.point - userGameObject.transform.position).normalized;
        }

        // �κ��丮 ������Ʈ 
        InventoryUiMain.instance.IconRefresh();

        // ���� ������
        yield return new WaitForSeconds(data.weaponDelay);
        //

        
    }
}
