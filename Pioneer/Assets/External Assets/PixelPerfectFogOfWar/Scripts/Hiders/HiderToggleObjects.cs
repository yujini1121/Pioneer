using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FOW
{
    public class HiderToggleObjects : HiderBehavior
    {
        [Tooltip("Objects that will be visible when in Line Of Sight")]
        public GameObject[] RevealedObjects;
        [Tooltip("Objects that will be visible when out of Line Of Sight")]
        public GameObject[] HiddenObjects;
        
        protected override void OnHide()
        {
            foreach (GameObject o in RevealedObjects)
                o.SetActive(false);

            foreach (GameObject o in HiddenObjects)
                o.SetActive(true);
        }

        protected override void OnReveal()
        {
            foreach (GameObject o in RevealedObjects)
                o.SetActive(true);

            foreach (GameObject o in HiddenObjects)
                o.SetActive(false);
        }
    }
}
