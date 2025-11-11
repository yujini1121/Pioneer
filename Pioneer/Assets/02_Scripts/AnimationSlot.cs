using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AnimationSlot
{
	[Header("공통")]
	public List<AnimationClip> idle;
	public List<AnimationClip> run;
	public List<AnimationClip> fising;
	public List<AnimationClip> attack;

	[Header("플레이어 전용")]
	public List<AnimationClip> woodenSword;
	public List<AnimationClip> ironSword;
	public List<AnimationClip> conchSword;

	[Header("애니메이션 클립")]
	public AnimationClip curIdleClip;
	public AnimationClip curRunClip;
	public AnimationClip curFishingClip;
	public AnimationClip curAttackClip;
	public AnimationClip curWoodenSwordClip;
	public AnimationClip curIronSwrordClip;
	public AnimationClip curConchSwordClip;
}