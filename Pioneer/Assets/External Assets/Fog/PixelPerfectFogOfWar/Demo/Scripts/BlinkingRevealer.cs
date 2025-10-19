using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FOW.Demos
{
    public class BlinkingRevealer : MonoBehaviour
    {
        public float BlinkCycleTime = 5;

        public bool RandomOffset = true;
        private void Awake()
        {
            if (RandomOffset)
                BlinkCycleTime += Random.Range(0, BlinkCycleTime * .5f);
        }
        private void Update()
        {
            if (Time.time % BlinkCycleTime < BlinkCycleTime / 2)
            {
                if (!transform.GetChild(0).gameObject.activeInHierarchy)
                    transform.GetChild(0).gameObject.SetActive(true);
            }
            else
            {
                if (transform.GetChild(0).gameObject.activeInHierarchy)
                    transform.GetChild(0).gameObject.SetActive(false);
            }
        }
    }
}