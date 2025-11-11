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
    public List<AnimationClip> woodSword;
    public List<AnimationClip> conchSword;
    public List<AnimationClip> ironSword;

    [Header("제발잘되게해주세요제발")]
    public AnimationSlot animList;
}