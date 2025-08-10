using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarkMariner : MonoBehaviour
{
    string[] curseList = new string[]
    {
        "이기적인 놈",
        "어째서 못 본 채 할 수가 있는거지?",
        "역겨워",
        "더 이상 돌아갈 수 없어",
        "왜 그랬지?",
        "이미 늦었어",
        "너도 그렇게 될거야",
        "너도 얼마 안 남았어",
        "곧 너에게 갈게"
    };

    public void Curse() => GuiltyCanvas.instance.CurseView(SelectCurse());
    public string SelectCurse() => curseList[Random.Range(0, curseList.Length)];

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($">> DarkMariner.OnCollisionEnter(Collision collision) : 호출됨, 충돌체 {collision.collider.name}");

        if (ThisIsPlayer.IsThisPlayer(collision))
        {
            Debug.Log($">> DarkMariner.OnCollisionEnter(Collision collision) => Player");

            Curse();
        }
    }
}
