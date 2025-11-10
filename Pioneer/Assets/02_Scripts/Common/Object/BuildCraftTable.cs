using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildCraftTable : StructureBase
{
	//private void OnTriggerEnter(Collider other)
	//{
	//	if (ThisIsPlayer.IsThisPlayer(other))
	//	{
	//		InGameUI.instance.ShowDefaultCraftUI();
	//	}
	//}

	//private void OnTriggerExit(Collider other)
	//{
	//	if (ThisIsPlayer.IsThisPlayer(other))
	//	{
	//		InGameUI.instance.CloseDefaultCraftUI();
	//	}
	//}

	public override void Use()
	{
		base.Use();
		InGameUI.instance.ShowDefaultCraftUI();
	}

	public override void UnUse()
	{
		base.UnUse();
		InGameUI.instance.CloseDefaultCraftUI();
	}
}
