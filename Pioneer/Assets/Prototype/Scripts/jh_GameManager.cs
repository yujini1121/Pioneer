using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class jh_GameManager : MonoBehaviour
{
    public static jh_GameManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public void RespawnPlayer(GameObject player, Vector3 respawnPosition, float delay)
    {
        StartCoroutine(RespawnCoroutine(player, respawnPosition, delay));
    }

    private IEnumerator RespawnCoroutine(GameObject player, Vector3 respawnPosition, float delay)
    {
        Debug.Log("respawn ...");
        yield return new WaitForSeconds(delay);

        player.transform.position = respawnPosition;
        player.SetActive(true);

        jh_PlayerHealth health = player.GetComponent<jh_PlayerHealth>();
        if (health != null)
        {
            health.Revive();
        }

        Rigidbody rb = player.GetComponent<Rigidbody>(); // 없으면 부활 시 넘어짐...
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }
}
