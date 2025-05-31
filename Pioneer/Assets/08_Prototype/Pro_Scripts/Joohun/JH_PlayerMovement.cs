using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JH_PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 2f;
    private Rigidbody rb;
    private Vector3 moveInput;

    private Vector3 lastPosition;
    public float moveThreshold = 0.01f;     // 혹시 모를 미세한 움직임 대비

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        lastPosition = transform.position;
    }

    void Update()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");
        moveInput = new Vector3(moveX, 0f, moveZ).normalized;
    }

    void FixedUpdate()
    {
        Vector3 velocity = new Vector3(moveInput.x * moveSpeed, rb.velocity.y, moveInput.z * moveSpeed);
        rb.velocity = velocity;
    }

    public bool HasMoved()
    {
        float dist = Vector3.Distance(transform.position, lastPosition);
        bool moved = dist > moveThreshold;
        if (moved) lastPosition = transform.position;
        return moved;
    }
}
