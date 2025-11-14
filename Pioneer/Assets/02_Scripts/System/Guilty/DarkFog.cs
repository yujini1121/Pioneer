using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class DarkFog : MonoBehaviour
{
    public float armedTime;
    bool hasTouched = false;
    public ParticleSystem particle;
    //public EmissionModule emit;

    private void Start()
    {
        if (particle == null)
            particle = GetComponent<ParticleSystem>();
    }

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
            var emission = particle.emission;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(0f);

            Destroy(gameObject, 1.0f);
            hasTouched = true;
        }
    }
}
