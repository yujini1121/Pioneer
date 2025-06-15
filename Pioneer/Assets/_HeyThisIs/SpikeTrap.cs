using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpikeTrap : MonoBehaviour
{
    [SerializeField] private SInstallableObjectDataSO objectData;

    [Header("Ʈ�� �ɼ�")]
    [SerializeField] private float timeToStart;
    [SerializeField] private float duration;
    [SerializeField] private float timeToReset;
    [SerializeField] private float attackPower;
    [SerializeField] private float attackInterval;
    [SerializeField] private int howManyTime;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Vector3 hidePos;
    [SerializeField] private Vector3 triggeredPos;
    [SerializeField] private GameObject niddles;
    [SerializeField] private AnimationCurve curve;

    [Header("�����")]
    [SerializeField] private int numberOfUses = 0;
    [SerializeField] private bool isTriggerd;

    private void OnTriggerStay(Collider other)
    {
        if (((1 << other.gameObject.layer) &enemyLayer) != 0 && isTriggerd == false)
        {
            StartCoroutine(Trigger());
        }
    }

    private IEnumerator Trigger()
    {
        isTriggerd = true;

        //�ߵ�
        float elapsed = 0f;
        while (elapsed < timeToStart)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / timeToStart);

            niddles.transform.localPosition = Vector3.Lerp(hidePos, triggeredPos, curve.Evaluate(t));
            yield return null;
        }
        niddles.transform.localPosition = triggeredPos;

        //����
        elapsed = 0f;
        while (elapsed < duration)
        {
            Debug.Log("����");
            elapsed += 1f;
            yield return new WaitForSeconds(1f);
        }

        //����
        elapsed = 0f;
        while (elapsed < timeToReset)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / timeToReset);

            niddles.transform.localPosition = Vector3.Lerp(triggeredPos, hidePos, curve.Evaluate(t));
            yield return null;
        }
        niddles.transform.localPosition = hidePos;

        isTriggerd = false;
        numberOfUses++;

        if (numberOfUses >= howManyTime)
        {
            Destroy(gameObject);
        }
    }
}