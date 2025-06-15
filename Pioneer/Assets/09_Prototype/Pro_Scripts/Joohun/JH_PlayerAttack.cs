using System.Collections;
using UnityEngine;

public class JH_PlayerAttack : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float attackDistance = 1.5f;
    public float attackDelay = 0.5f;

    private Transform targetEnemy;
    private bool isAttacking = false;
    private bool isMoving = false;

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isAttacking)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                if (hit.transform.CompareTag("Enemy"))
                {
                    targetEnemy = hit.transform;
                    isMoving = true;
                }
            }
        }

        if (isMoving && targetEnemy != null)
        {
            MoveToTarget();
        }
    }

    void MoveToTarget()
    {
        Vector3 targetPos = targetEnemy.position;
        targetPos.y = transform.position.y;

        Vector3 direction = targetPos - transform.position;

        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(direction), 0.2f);

        transform.position += direction.normalized * moveSpeed * Time.deltaTime;

        if (Vector3.Distance(transform.position, targetPos) <= attackDistance)
        {
            isMoving = false;
            StartCoroutine(Attack());
        }
    }

    IEnumerator Attack()
    {
        isAttacking = true;
        Debug.Log("공격 시작");

        if (targetEnemy != null)
        {
            Debug.Log("적 처치");
            Destroy(targetEnemy.gameObject);
        }

        targetEnemy = null;
        
        yield return new WaitForSeconds(attackDelay);

        isAttacking = false;
    }
}
