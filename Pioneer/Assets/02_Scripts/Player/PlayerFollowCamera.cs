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
        float currYangle = Mathf.LerpAngle(transform.eulerAngles.y, target.eulerAngles.y, smoothRotate * Time.deltaTime);
        Quaternion rotation = Quaternion.Euler(0, currYangle, 0);
        Vector3 position = target.position - (rotation * Vector3.forward * distance) + (Vector3.up * height);

        transform.position = position;

        transform.LookAt(target);
    }
}
