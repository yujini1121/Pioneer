using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class guilt : MonoBehaviour, IBegin
{
    public GameObject shadow_crew;
    int guilt_level;
    float spown = 0;
    IEnumerator shadow_crew_lv1()
    {
        Instantiate(shadow_crew, transform.position, transform.rotation);
        yield return new WaitForSeconds(10);
        Destroy(shadow_crew );
    }
    IEnumerator shadow_crew_lv2()
    {
        Instantiate(shadow_crew, transform.position, transform.rotation);
        yield return new WaitForSeconds(10);
        Destroy(shadow_crew);
    }
    IEnumerator shadow_crew_lv3()
    {
        Instantiate(shadow_crew, transform.position, transform.rotation);
        yield return new WaitForSeconds(10);
        Destroy(shadow_crew);
    }
    IEnumerator shadow_crew_lv4()
    {
        Instantiate(shadow_crew, transform.position, transform.rotation);
        yield return new WaitForSeconds(10);
        Destroy(shadow_crew);
    }
    // Start is called before the first frame update
    void Start()
    {
        guilt_level = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (guilt_level == 1)
        {
            float r = Random.Range(100, 0);
            if (r <= 40 && r > 0&& Time.realtimeSinceStartup - spown > 30)
            {
               StartCoroutine(shadow_crew_lv1());
                spown = Time.realtimeSinceStartup;
            }

        }
        else if (guilt_level == 2)
        {
            float r = Random.Range(100, 0);
            if (r <= 50 && r > 0 && Time.realtimeSinceStartup - spown > 30)
            {
                StartCoroutine(shadow_crew_lv2());
                spown = Time.realtimeSinceStartup;
            }

        }
        else if (guilt_level == 3)
        {
            float r = Random.Range(100, 0);
            if (r <= 50 && r > 0 && Time.realtimeSinceStartup - spown > 30)
            {
                StartCoroutine(shadow_crew_lv3());
                spown = Time.realtimeSinceStartup;
            }

        }
        else if (guilt_level == 4)
        {
            float r = Random.Range(100, 0);
            if (r <= 70 && r > 0 && Time.realtimeSinceStartup - spown > 30)
            {
                StartCoroutine(shadow_crew_lv4());
                spown = Time.realtimeSinceStartup;
            }

        }
    }
}
