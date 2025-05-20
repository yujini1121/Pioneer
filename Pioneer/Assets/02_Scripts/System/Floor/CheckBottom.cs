using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class CheckBottom : MonoBehaviour
{
    public int playerHp = 100;
    public int maxHp = 100;
    public Slider hpBar;
    public float breathTime = 30f;
    public float damageInterval = 5f;
    public int damageAmount = 10;
    public float returnSpeed = 2f;
    public KeyCode returnKey = KeyCode.B;
    public Vector3 shipPosition = Vector3.zero;

    public LayerMask seaLayerMask; // 바다 감지용 레이어

    private bool isInSea = false;
    private float seaTimer = 0f;
    private float damageTimer = 0f;
    private bool isReturning = false;

    private NavMeshAgent agent;

    void Start()
    {
        if (hpBar != null)
        {
            hpBar.maxValue = maxHp;
            hpBar.value = playerHp;
        }

        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        DetectSeaUnderneath();

        if (isInSea && !isReturning)
        {
            seaTimer += Time.deltaTime;

            if (seaTimer > breathTime)
            {
                damageTimer += Time.deltaTime;
                if (damageTimer >= damageInterval)
                {
                    ApplyDamage(damageAmount);
                    damageTimer = 0f;
                }
            }

            if (Input.GetKeyDown(returnKey))
            {
                StartReturningToShip();
            }
        }

        if (isReturning)
        {
            CheckArrivalAtShip();
        }
    }

    void StartReturningToShip()
    {
        isReturning = true;
        agent.isStopped = false;
        agent.SetDestination(shipPosition);
        Debug.Log("배로 복귀 중...");
    }

    void CheckArrivalAtShip()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
            {
                isReturning = false;
                isInSea = false;
                seaTimer = 0f;
                damageTimer = 0f;
                agent.ResetPath();      // 복귀 완료 : 대기 상태로 전환 (언제든지 다시 움직일 수 있게)
            }
        }
    }

    void DetectSeaUnderneath()
    {
        Ray ray = new Ray(transform.position + Vector3.up, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 5f, seaLayerMask, QueryTriggerInteraction.Collide))
        {
            if (!isInSea)
            {
                isInSea = true;
                seaTimer = 0f;
                damageTimer = 0f;
                Debug.Log("바다에 진입");
            }
        }
        else
        {
            if (isInSea && !isReturning)
            {
                isInSea = false;
                seaTimer = 0f;
                damageTimer = 0f;
                Debug.Log("바다에서 나옴");
            }
        }
    }

    private void ApplyDamage(int amount)
    {
        playerHp -= amount;
        playerHp = Mathf.Clamp(playerHp, 0, maxHp);

        if (hpBar != null)
        {
            hpBar.value = playerHp;
        }

        Debug.Log($"HP 감소: {amount}, 현재 HP: {playerHp}");
    }
}
