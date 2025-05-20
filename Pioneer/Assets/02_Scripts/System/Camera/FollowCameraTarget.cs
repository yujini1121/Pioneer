using UnityEngine;

public class FollowCameraTarget : MonoBehaviour
{
    public Transform player;
    public Vector3 offset = new Vector3(0, 10, 0);
    public float followSpeed = 10f;

    void LateUpdate()
    {
        if (player == null) return;

        Vector3 targetPos = player.position + offset;
        transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);
    }
}
