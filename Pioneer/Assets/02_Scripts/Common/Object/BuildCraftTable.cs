using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildCraftTable : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (ThisIsPlayer.IsThisPlayer(other))
        {
            InGameUI.instance.ShowDefaultCraftUI();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (ThisIsPlayer.IsThisPlayer(other))
        {
            InGameUI.instance.CloseDefaultCraftUI();
        }
    }
}
