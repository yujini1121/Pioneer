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
		Debug.Log($">> BuildCraftTable.Use() / name={name} / activeSelf={gameObject.activeSelf}");
		base.Use();
		Debug.Log(">> BuildCraftTable.Use() / calling ShowDefaultCraftUI");
		InGameUI.instance.ShowDefaultCraftUI();
	}

	public override void UnUse()
	{
		Debug.Log($">> BuildCraftTable.UnUse() / name={name}");
		base.UnUse();
		InGameUI.instance.CloseDefaultCraftUI();
	}
}
