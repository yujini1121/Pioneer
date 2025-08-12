using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JH_FollowCamera : MonoBehaviour, IBegin
{
    public GameObject attackObject;             
    public float attackCooldown = 0.5f;
    public float attackActiveTime = 0.2f;

    private bool canAttack = true;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && canAttack)
        {
            StartCoroutine(PerformAttack());
        }
    }

    private IEnumerator PerformAttack()
    {
        canAttack = false;

        Vector3 mouseScreenPos = Input.mousePosition;
        Ray ray = mainCamera.ScreenPointToRay(mouseScreenPos);

        Plane groundPlane = new Plane(Vector3.up, transform.position);
        if (groundPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            Vector3 direction = (hitPoint - transform.position).normalized;

            Vector3 attackPosition = transform.position + direction * 1.5f;
            attackObject.transform.position = attackPosition;
            attackObject.transform.rotation = Quaternion.LookRotation(direction);
            attackObject.SetActive(true);

            yield return new WaitForSeconds(attackActiveTime);
            attackObject.SetActive(false);
            attackObject.transform.position = Vector3.zero;

            yield return new WaitForSeconds(attackCooldown - attackActiveTime);
        }

        canAttack = true;
    }
}
