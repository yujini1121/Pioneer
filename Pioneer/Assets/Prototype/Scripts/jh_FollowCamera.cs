using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class jh_FollowCamera : MonoBehaviour
{
    public Transform target;
    private Vector3 offset = new Vector3(0, 5, -5);

    void LateUpdate()
    {
        transform.position = target.position + offset;
        transform.LookAt(target);
    }
}
