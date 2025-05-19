using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JH_PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 2f;
    private Rigidbody rb;
    private Vector3 moveInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
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
}
