using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FOW.Demos
{
    public class FowCharacterDemo : MonoBehaviour
    {
        public float WalkingSpeed = 5;
        public float RunningMultiplier = 1.65f;
        public float Acceleration = 25;
        private float yRot;
        private CharacterController cc;
        private bool CursorLocked;
        private void Awake()
        {
            cc = GetComponent<CharacterController>();
            CursorLocked = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CursorLocked = !CursorLocked;
                if (CursorLocked)
                {
                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;
                }
                else
                {
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                }
            }
            if (CursorLocked)
            {
                transform.Rotate(0, Input.GetAxis("Mouse X"), 0);
                yRot -= Input.GetAxis("Mouse Y");
            }
            yRot = Mathf.Clamp(yRot, -80, 80);
            setInput();
            move();
        }
        Vector2 inputDirection = Vector2.zero;
        Vector2 velocityXZ = Vector2.zero;
        Vector3 velocity = Vector3.zero;
        float speedTarget;
        public void setInput()
        {
            bool[] inputs = new bool[]
                {
                Input.GetKey(KeyCode.W),
                Input.GetKey(KeyCode.A),
                Input.GetKey(KeyCode.S),
                Input.GetKey(KeyCode.D),
                Input.GetKey(KeyCode.LeftShift)
                };
            speedTarget = 0;
            inputDirection = Vector2.zero;
            if (inputs[0])
            {
                inputDirection.y += 1;
                speedTarget = WalkingSpeed;
            }
            if (inputs[1])
            {
                inputDirection.x -= 1;
                speedTarget = WalkingSpeed;
            }
            if (inputs[2])
            {
                inputDirection.y -= 1;
                speedTarget = WalkingSpeed;
            }
            if (inputs[3])
            {
                inputDirection.x += 1;
                speedTarget = WalkingSpeed;
            }
            if (inputs[4])
            {
                speedTarget *= RunningMultiplier;
            }
        }
        void move()
        {
            if (cc.isGrounded)
            {
                velocity.y = 0;
            }
            Vector2 forward = new Vector2(transform.forward.x, transform.forward.z);
            Vector2 right = new Vector2(transform.right.x, transform.right.z);
            Vector2 inputDir = Vector3.Normalize(right * inputDirection.x + forward * inputDirection.y);
            velocityXZ = Vector2.MoveTowards(velocityXZ, inputDir.normalized * speedTarget, Time.deltaTime * Acceleration);
            //velocityXZ = Vector2.ClampMagnitude(velocityXZ, speedTarget);
            velocity.x = velocityXZ.x * Time.deltaTime;
            velocity.z = velocityXZ.y * Time.deltaTime;
            velocity.y += -9.81f * Time.deltaTime * Time.deltaTime;

            cc.enabled = true;
            cc.Move(velocity);
            cc.enabled = false;
        }
    }
}