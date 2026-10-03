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

        return PlayerCore.Instance != null && PlayerCore.Instance.PlayerAttack != null
            && PlayerCore.Instance.PlayerAttack.HasEnemyInDirection(dir, data.weaponRange);
    }

    public static IEnumerator AttackCoroutine(CommonBase userGameObject, SItemStack itemWithState, SItemWeaponTypeSO data)
    {
        PlayerCore player = PlayerCore.Instance;
        if (player == null || userGameObject == null || itemWithState == null || data == null || Camera.main == null)
            yield break;
        UtilityFunctions.Log($">> WeaponUseUtils.AttackCoroutine : 함수 호출됨 내구도 닳기 : {data.duabilityRedutionPerHit}");
        UtilityFunctions.Assert(itemWithState != null);
        UtilityFunctions.Assert(data != null);

        float originalSpeed = PlayerCore.Instance.speed;
        PlayerAttack playerAttack = PlayerCore.Instance.PlayerAttack;
        if (playerAttack == null)
            yield break;

        try
        {
            PlayerCore.Instance.speed = 0f;
            UtilityFunctions.Log($"플레이어 이동 멈춤 : {PlayerCore.Instance.speed}");

            Ray m_rayFromMouse = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane attackPlane = new Plane(Vector3.up, userGameObject.transform.position);

            if (attackPlane.Raycast(m_rayFromMouse, out float distance))
            {
                Vector3 dir = m_rayFromMouse.GetPoint(distance) - userGameObject.transform.position;
                dir.y = 0f;
                dir.Normalize();

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

                Vector3 position = userGameObject.transform.position + dir * (data.weaponRange * 0.5f);
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
                float growth = PlayerStatsLevel.Instance != null ? PlayerStatsLevel.Instance.CombatDamageMultiplier : 1f;
                playerAttack.damage = Mathf.RoundToInt((data.weaponDamage + player.CalculatedHandAttack.weaponDamage) * growth);
                playerAttack.SetAttackRange(data.weaponRange);
                playerAttack.DisableAttackCollider();
                playerAttack.PlayAttack(dir);

                yield return new WaitForSeconds(windupTime);

                playerAttack.EnableAttackCollider();
                yield return new WaitForSeconds(activeTime);

                playerAttack.DisableAttackCollider();
                playerAttack.SetAttackRange(0.1f);
                yield return new WaitForSeconds(recoveryTime);
                if (InventoryUiMain.instance != null) InventoryUiMain.instance.IconRefresh();
            }

            if (InventoryUiMain.instance != null) InventoryUiMain.instance.IconRefresh();
        }
        finally
        {
            if (playerAttack != null)
            {
                playerAttack.DisableAttackCollider();
                playerAttack.SetAttackRange(0.1f);
            }

            if (player != null) player.speed = originalSpeed;
        }

        yield return new WaitForSeconds(data.weaponDelay);
    }
}
