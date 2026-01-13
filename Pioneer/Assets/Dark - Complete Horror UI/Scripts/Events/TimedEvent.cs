using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Michsky.UI.Dark
{
    public class TimedEvent : MonoBehaviour
    {
        // Settings
        public float timer = 4;
        public bool enableAtStart;

        // Events
        public UnityEvent timerEvents;

        void Start()
        {
            if(enableAtStart == true)
                StartCoroutine("ProcessTimedEvent");
        }

        public void StartTimedEvent()
        {
            StartCoroutine("ProcessTimedEvent");
        }

        public void StopTimedEvent()
        {
            StopCoroutine("ProcessTimedEvent");
        }

        IEnumerator ProcessTimedEvent()
        {
            yield return new WaitForSeconds(timer);
            timerEvents.Invoke();
        }
    }
}
