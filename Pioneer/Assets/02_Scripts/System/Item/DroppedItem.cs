using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class DroppedItem : MonoBehaviour
{
    public bool isCanPickUp = false;
    public SItemStack itemValue;
    public Transform transformCanvas;
    public ItemSlotUI slotUI;
    [SerializeField] float pickUpRadius = 1.5f;
    [SerializeField] float collectDistance = 0.2f;
    [SerializeField] float attractSpeed = 10f;
    float pickUpTime = 0f;
    bool isAttracting = false;

    public void SetItem(SItemStack item, float pickUpTime)
    {
        Debug.Log($">> DroppedItem.SetItem(SItemStack item) : 호출됨 / isItemNull : {item == null}");

        itemValue = item;
        slotUI.Show(item);
        this.pickUpTime = pickUpTime;
        IEnumerator EnablePickUpAfterTime()
        {
            yield return new WaitForSeconds(pickUpTime);
            isCanPickUp = true;
        }
        StartCoroutine(EnablePickUpAfterTime());
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (ThisIsPlayer.IsThisPlayer(collision) && isCanPickUp)
        {
            PickUp();
        }
    }

    private void Update()
    {
        if (isCanPickUp == false || ThisIsPlayer.Player == null)
        {
            return;
        }

        Transform playerTransform = ThisIsPlayer.Player.transform;

        if (isAttracting)
        {
            MoveToPlayer(playerTransform);
            return;
        }

        if (GetPlanarSqrDistance(playerTransform.position) <= pickUpRadius * pickUpRadius)
        {
            isAttracting = true;
        }
    }

    private void MoveToPlayer(Transform playerTransform)
    {
        Vector3 targetPosition = playerTransform.position;
        float lerp = 1f - Mathf.Exp(-attractSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPosition, lerp);

        if (GetPlanarSqrDistance(targetPosition) <= collectDistance * collectDistance)
        {
            PickUp();
        }
    }

    private float GetPlanarSqrDistance(Vector3 targetPosition)
    {
        Vector3 offset = targetPosition - transform.position;
        offset.y = 0f;
        return offset.sqrMagnitude;
    }

    private void PickUp()
    {
        if (isCanPickUp == false)
        {
            return;
        }

        isCanPickUp = false;

        InventoryManager.Instance.Add(itemValue);
        InventoryManager.Instance.UpdateSlot();
        PlayerStatUI.Instance.UpdateBasicStatUI();
        InventoryUiMain.instance.IconRefresh();

        Destroy(gameObject);
    }
}
