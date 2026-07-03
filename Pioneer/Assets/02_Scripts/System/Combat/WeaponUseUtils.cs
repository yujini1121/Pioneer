using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class WeaponUseUtils
{
    private const float AttackWindupRatio = 0.18f;
    private const float AttackActiveRatio = 0.2f;
    private const float MinAttackWindupTime = 0.05f;
    private const float MinAttackActiveTime = 0.05f;

    private static bool HasAttackTarget(CommonBase userGameObject, SItemWeaponTypeSO data, Vector3 dir)
    {
        if (userGameObject == null || data == null)
            return false;

        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
            return false;

        LayerMask enemyMask = PlayerCore.Instance != null ? PlayerCore.Instance.EnemyLayer : 0;
        Vector3 origin = userGameObject.transform.position;
        float range = Mathf.Max(data.weaponRange, 0.5f);
        Collider[] hits = Physics.OverlapSphere(origin, range, enemyMask, QueryTriggerInteraction.Ignore);

        foreach (Collider hit in hits)
        {
            if (hit == null)
                continue;

            Vector3 toTarget = hit.bounds.center - origin;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > range * range)
                continue;

            if (Vector3.Dot(dir.normalized, toTarget.normalized) < 0.2f)
                continue;

            CommonBase target = hit.GetComponentInParent<CommonBase>();
            if (target == null)
                target = hit.GetComponent<CommonBase>();

            if (target != null && !target.IsDead)
                return true;
        }

        return false;
    }

    public static IEnumerator AttackCoroutine(CommonBase userGameObject, SItemStack itemWithState, SItemWeaponTypeSO data)
    {
        Debug.Log($">> WeaponUseUtils.AttackCoroutine : 함수 호출됨 내구도 닳기 : {data.duabilityRedutionPerHit}");
        Debug.Assert(itemWithState != null);
        Debug.Assert(data != null);

        float originalSpeed = PlayerCore.Instance.speed;
        PlayerAttack playerAttack = PlayerCore.Instance.PlayerAttack;
        if (playerAttack == null)
            yield break;

        try
        {
            PlayerCore.Instance.speed = 0f;
            Debug.Log($"플레이어 이동 멈춤 : {PlayerCore.Instance.speed}");

            Ray m_rayFromMouse = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit m_hitOnMap;

            if (Physics.Raycast(m_rayFromMouse, out m_hitOnMap, Mathf.Infinity))
            {
                Vector3 dir = (m_hitOnMap.point - userGameObject.transform.position).normalized;
                dir.y = 0f;

                if (!HasAttackTarget(userGameObject, data, dir))
                    yield break;

                PlayerCore.Instance.StopHorizontalMovement();

                switch (data.id)
                {
                    case 20001:
                    case 20002:
                    case 20003:
                        if (AudioManager.instance != null)
                            AudioManager.instance.PlaySfx(AudioManager.SFX.SamshSound);
                        break;
                    default:
                        if (AudioManager.instance != null)
                            AudioManager.instance.PlaySfx(AudioManager.SFX.Punch3_Player);
                        break;
                }

                userGameObject.transform.rotation = Quaternion.LookRotation(dir);

                Vector3 position = userGameObject.transform.position + dir * 0.5f;
                position.y = PlayerCore.Instance.AttackHeight;

                float totalAnimationTime = Mathf.Max(0.01f, data.weaponAnimation);
                float windupTime = Mathf.Clamp(
                    totalAnimationTime * AttackWindupRatio,
                    Mathf.Min(MinAttackWindupTime, Mathf.Max(0f, totalAnimationTime - MinAttackActiveTime)),
                    Mathf.Max(0f, totalAnimationTime - MinAttackActiveTime));
                float activeBudget = Mathf.Max(0f, totalAnimationTime - windupTime);
                float activeTime = Mathf.Min(Mathf.Max(totalAnimationTime * AttackActiveRatio, Mathf.Min(MinAttackActiveTime, activeBudget)), activeBudget);
                float recoveryTime = Mathf.Max(0f, totalAnimationTime - windupTime - activeTime);

                playerAttack.transform.position = position;
                playerAttack.transform.rotation = Quaternion.LookRotation(dir);
                playerAttack.damage = (int)(data.weaponDamage + PlayerCore.Instance.CalculatedHandAttack.weaponDamage);
                playerAttack.SetAttackRange(data.weaponRange);
                playerAttack.DisableAttackCollider();
                playerAttack.PlayAttack(dir);

                yield return new WaitForSeconds(windupTime);

                playerAttack.EnableAttackCollider();
                yield return new WaitForSeconds(activeTime);

                playerAttack.DisableAttackCollider();
                playerAttack.SetAttackRange(0.1f);
                yield return new WaitForSeconds(recoveryTime);
                InventoryUiMain.instance.IconRefresh();
            }

            InventoryUiMain.instance.IconRefresh();
        }
        finally
        {
            if (playerAttack != null)
            {
                playerAttack.DisableAttackCollider();
                playerAttack.SetAttackRange(0.1f);
            }

            PlayerCore.Instance.speed = originalSpeed;
        }

        yield return new WaitForSeconds(data.weaponDelay);
    }
}
