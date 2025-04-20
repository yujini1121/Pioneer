using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class jh_PlayerMovement : MonoBehaviour
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
        rb.velocity = moveInput * moveSpeed;
    }
}
