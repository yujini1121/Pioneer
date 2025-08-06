using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class PlayerAttack : CreatureBase, IBegin
{
    [HideInInspector]public Transform playerTransform;

    [Header("Player Attack Range Object Height")]
    [SerializeField]private float attackHeight = 1.0f;

    [Header("Player Attack Range Object Duration")]
    [SerializeField]private float attackDuration = 0.2f;

    public IEnumerator AttackRange()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            Vector3 dir = (hit.point - playerTransform.position).normalized;

            dir.y = 0f;

            Vector3 position = playerTransform.position + dir;
            position.y = attackHeight;

            transform.position = position;

            transform.rotation = Quaternion.LookRotation(dir);

            gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(attackDuration);

        gameObject.SetActive(false);
        transform.position = Vector3.zero;
    }

    private  void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Enemy"))
        {
            CommonBase enemy = other.GetComponent<CommonBase>();
            if (enemy != null)
            {
                int damage = 10; // 예시로 데미지 값을 설정
                enemy.TakeDamage(damage); // 적에게 데미지 전달

                //if (enemy.hp <= 0)
                //    enemy.Die();
            }            
        }
    }
}
