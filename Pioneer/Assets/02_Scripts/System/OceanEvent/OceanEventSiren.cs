using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
public class OceanEventSiren : OceanEventBase
{
    private Coroutine charmRoutine;

    public override void EventRun()
    {
        OceanEventManager.instance.BeginCoroutine(CharmLoop());
    }

    public override void EventEnd()
    {
        OceanEventManager.instance.StopCoroutine(charmRoutine); 
    }

    private IEnumerator CharmLoop()
    {
        float totalDuration = GameManager.Instance.dayDuration + GameManager.Instance.nightDuration;
        float elapsed = 0f;

        while (elapsed<totalDuration)
        {
            yield return new WaitForSeconds(30f);
            elapsed += 30f;

            if(Random.value <= 0.5f)
            {
                TryCharmMariner();
            }
        }
        EventEnd();
    }


    void TryCharmMariner()
    {
        MarinerAI[] mariners = GameObject.FindObjectsOfType<MarinerAI>();
        if (mariners.Length == 0) return;

        MarinerAI target = mariners[Random.Range(0, mariners.Length)];

        if (target != null && target.GetComponent<Charm>() == null)
        {
            target.gameObject.AddComponent<Charm>();
        }
    }
}

public class Charm : MonoBehaviour
{
    private int clickCount;
    public float timer;
    public int duration;

}

