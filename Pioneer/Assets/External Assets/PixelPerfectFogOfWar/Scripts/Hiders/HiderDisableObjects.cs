using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FOW
{
    public class HiderDisableObjects : HiderBehavior
    {
        [SerializeField] private GameObject[] ObjectsToHide;

        protected override void OnHide()
        {
            foreach (GameObject o in ObjectsToHide)
                o.SetActive(false);
        }

        protected override void OnReveal()
        {
            foreach (GameObject o in ObjectsToHide)
                o.SetActive(true);
        }

        public void ModifyHiddenObjects(GameObject[] newObjectsToHide)
        {
            OnReveal();
            ObjectsToHide = newObjectsToHide;
            if (!enabled)
                return;

            if (!IsEnabled)
                OnHide();
            else
                OnReveal();
        }
    }
}