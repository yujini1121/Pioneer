using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class DarkFog : MonoBehaviour
{
    public float armedTime;
    bool hasTouched = false;
    public ParticleSystem particle;
    public IdObject<DarkFog> poolObjectSelf;
    Coroutine coroutine;

    //public EmissionModule emit;

    public void EndPossess()
    {
        EmissionModule one = particle.emission;
        one.rateOverTime = 0f;
        hasTouched = false;
        coroutine = null;
    }

    public void PreparePossess()
    {
        EmissionModule one = particle.emission;
        one.rateOverTime = 50f;
    }

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

            
            hasTouched = true;
            IEnumerator destroyCoroutine()
            {
                yield return new WaitForSeconds(1.0f);
                // ÆÄ±«
                GuiltySystem.instance.ReleasePoolObject(poolObjectSelf);
            }
            StartCoroutine(destroyCoroutine());
        }
    }
}
