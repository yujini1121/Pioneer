using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FOW
{
    [RequireComponent(typeof(FogOfWarHider))]
    public abstract class HiderBehavior : MonoBehaviour
    {
        protected bool IsEnabled;
        protected virtual void Awake()
        {
            OnHide();
            GetComponent<FogOfWarHider>().OnActiveChanged += OnStatusChanged;
        }

        void OnStatusChanged(bool isEnabled)
        {
            IsEnabled = isEnabled;
            if (isEnabled)
                OnReveal();
            else
                OnHide();
        }
        protected abstract void OnReveal();
        protected abstract void OnHide();
    }
}
