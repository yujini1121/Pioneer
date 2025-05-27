using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class PlayerFollowCamera : MonoBehaviour
{
    [SerializeField] Transform target;

    private float distance = 10.0f;
    private float height = 5.0f;
    private float smoothRotate = 5.0f;

    void LateUpdate()
    {
        Vector3 fixedOffset = new Vector3(0, height, -distance);
        transform.position = target.position + fixedOffset;

        transform.LookAt(target);
    }
}
