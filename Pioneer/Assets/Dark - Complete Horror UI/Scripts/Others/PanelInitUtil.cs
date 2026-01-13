using UnityEngine;

namespace Michsky.UI.Util
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(CanvasGroup))]
    public class PanelInitUtil : MonoBehaviour
    {
        public RuntimeAnimatorController controller;

        void Awake()
        {
            Animator anm = gameObject.GetComponent<Animator>();
            anm.runtimeAnimatorController = controller;
            anm.updateMode = AnimatorUpdateMode.UnscaledTime;
        }
    }
}