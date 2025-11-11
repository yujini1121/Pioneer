using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarkFog : MonoBehaviour
{
    public float armedTime;
    bool hasTouched = false;

    private void OnTriggerStay(Collider other)
    {
        if (armedTime > Time.time || hasTouched)
        {
            return;
        }

        if (ThisIsPlayer.IsThisPlayer(other))
        {
            // Debug.Log(">> DarkFog.Touched");

            GuiltySystem.instance.DarkFogTouched();

            if (AudioManager.instance != null)
                AudioManager.instance.PlaySfx(AudioManager.SFX.AfterAttack_BlackFog);

            Destroy(gameObject, 1.0f);
            hasTouched = true;
        }
    }
}
