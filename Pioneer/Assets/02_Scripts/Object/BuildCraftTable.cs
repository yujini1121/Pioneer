using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildCraftTable : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "Player")
        {
            InGameUI.instance.ShowDefaultCraftUI();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.name == "Player")
        {
            InGameUI.instance.CloseDefaultCraftUI();
        }
    }
}
