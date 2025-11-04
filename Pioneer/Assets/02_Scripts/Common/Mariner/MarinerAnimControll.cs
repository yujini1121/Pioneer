using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]

public class MarinerAnimControll : MonoBehaviour
{
    public NavMeshAgent agent;
    public Animator animator;
    public SpriteRenderer sprite;

    [Header("Tuning")]
    public float idleThreshold = 0.05f;
    public float damp = 0.08f;
    public bool invertX = false; 
    public bool invertZ = false; 

    void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        sprite = GetComponentInChildren<SpriteRenderer>();
    }

    void Awake()
    {
        agent.updateRotation = false;
        agent.updateUpAxis = false;   
    }

    void Update()
    {
        Vector3 v = agent ? agent.velocity : Vector3.zero; 
        float dirX = v.x;   // 좌/우
        float dirZ = v.z;   // 위/아래

        if (invertX) dirX = -dirX;
        if (invertZ) dirZ = -dirZ;

        float speed = new Vector2(dirX, dirZ).magnitude;
        Vector2 n = speed > 1e-4f ? new Vector2(dirX, dirZ).normalized : Vector2.zero;

        animator.SetFloat("Speed", speed, damp, Time.deltaTime);
        animator.SetFloat("DirX", n.x, damp, Time.deltaTime); // Side 판정
        animator.SetFloat("DirZ", n.y, damp, Time.deltaTime); // Front/Back 판정

        if (sprite && speed >= idleThreshold && Mathf.Abs(n.x) > Mathf.Abs(n.y))
            sprite.flipX = (n.x < 0f);
    }
}
