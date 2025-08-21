using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JH_PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 2f;
    private Rigidbody rb;
    public Vector3 moveInput { get; private set; }

    private Vector3 lastPosition;
    public float moveThreshold = 0.01f;     // 혹시 모를 미세한 움직임 대비

    private Vector3 centerVec;
    private float centerVecY = 1f;
    [SerializeField] private float interactableRadius;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private Collider[] colliders;
    private RigidbodyConstraints rbConstraints;

    private void OnEnable()
    {
        if (TryGetComponent(out Rigidbody rigidbody))
        {
            rb = rigidbody;
        }
        else
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.constraints = rbConstraints;
        }
        lastPosition = transform.position;
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        Vector3 velocity = new Vector3(moveInput.x * moveSpeed, rb.velocity.y, moveInput.z * moveSpeed);
        rb.velocity = velocity;
    }

    void Update()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");
        moveInput = new Vector3(moveX, 0f, moveZ).normalized;


        DetectedEnemy();
        Attack();
    }

    void ToggleInteraction()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            //isUsing = !isUsing;
            //if (isUsing) OnUse?.Invoke();
            //else OnUnuse?.Invoke();
        }
    }

    // TODO: 발리스타 스크립트로 가져가기
    void DetectedEnemy()
    {
        centerVec = transform.position;
        centerVec.y += centerVecY;
        colliders = Physics.OverlapSphere(centerVec, interactableRadius, interactableLayer, QueryTriggerInteraction.Ignore);

        if (colliders.Length > 0)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                colliders[0].TryGetComponent<Ballista>(out var ballista);
                rbConstraints = rb.constraints;
                Destroy(rb);        // 플레이어 rb

                foreach (Behaviour component in GetComponents<Behaviour>())
                {
                    if (component is MeshFilter || component is MeshRenderer || component is Transform || component == null)
                        continue;

                    component.enabled = false;
                }
                GetComponent<CapsuleCollider>().enabled = false;
                ballista?.Use(gameObject);
            }
        }
    }

    private void Attack()
    {
        if (!(Input.GetMouseButtonDown(0))) return;

        Vector3 origin = transform.position + Vector3.up * 1f;
        Vector3 direction = transform.forward;
        float rayDistance = 5f;

        // 디버그 Ray 표시
        Debug.DrawRay(origin, direction * rayDistance, Color.red, 1f);

        if (Physics.Raycast(origin, direction, out RaycastHit hit, rayDistance))
        {
            Debug.LogError($"[공격] 적 감지됨: {hit.collider.name}");

            if (hit.collider.TryGetComponent(out MinionAI minion))
            {
                Debug.LogError("[공격] MinionAI에게 데미지 10 부여");
                minion.TakeDamage(10, this.gameObject);
            }
            else if (hit.collider.TryGetComponent(out ZombieMarinerAI zombie))
            {
                Debug.LogError("[공격] ZombieMarinerAI에게 데미지 10 부여");
                zombie.TakeDamage(10, this.gameObject);
            }
            else
            {
                Debug.LogError("[공격] 타겟은 MinionAI나 ZombieMarinerAI가 아님");
            }
        }
        else
        {
            Debug.LogError("[공격] 적 없음 - Ray에 아무것도 맞지 않음");
        }
    }

    public bool HasMoved()
    {
        float dist = Vector3.Distance(transform.position, lastPosition);
        bool moved = dist > moveThreshold;
        if (moved) lastPosition = transform.position;
        return moved;
    }

    private void OnDrawGizmos()
    {
        foreach (var collider in colliders)
        {
            if (collider == null) continue;

            Gizmos.color = Color.green;
            Vector3 center = collider.transform.position;
            center.y = centerVecY;
            Gizmos.DrawSphere(center, 0.5f);
        }
    }
}
