using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThisIsPlayer : MonoBehaviour
{
    static public GameObject Player = null;

    static public bool IsThisPlayer(GameObject sus) => Player == sus;
    static public bool IsThisPlayer(Collider sus) => Player == sus.gameObject;
    static public bool IsThisPlayer(Collision sus) => Player == sus.collider.gameObject;
    static public bool IsThisPlayer(string sus) => Player.name == sus;

    private void Awake()
    {
        Player = gameObject;
    }
}
