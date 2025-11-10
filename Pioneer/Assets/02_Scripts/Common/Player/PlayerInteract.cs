using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
	public static PlayerInteract instance;

	List<StructureBase> ready;

	public static void Add(StructureBase one)
	{
		instance.ready.Add(one);
	}

	private void Awake()
	{
		instance = this;
	}

	private void Update()
	{
		if (ready != null && ready.Count > 0)
		{
			if (Input.GetKeyDown(KeyCode.R))
			{
				// 가장 가까운 애 선택

				StructureBase closest = null;
				float distanceSqr = float.MaxValue;
				foreach (StructureBase one in ready)
				{
					float oneDistanceSqr = (transform.position - one.transform.position).sqrMagnitude;
					if (oneDistanceSqr < distanceSqr)
					{
						closest = one;
						distanceSqr = oneDistanceSqr;
					}
				}

				closest.Use();
			}
		}

		ready = new List<StructureBase>();
	}
}
