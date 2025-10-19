using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FOW
{
    public class HiderDisableRenderers : HiderBehavior
    {
        [SerializeField] private Renderer[] ObjectsToHide;

        protected override void OnHide()
        {
            foreach (Renderer renderer in ObjectsToHide)
                renderer.enabled = false;
        }

        protected override void OnReveal()
        {
            foreach (Renderer renderer in ObjectsToHide)
                renderer.enabled = true;
        }

        public void ModifyHiddenRenderers(Renderer[] newObjectsToHide)
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