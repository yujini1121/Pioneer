using UnityEngine;
using UnityEngine.EventSystems;

namespace Michsky.UI.Dark
{
    public class UIElementSound : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
    {
        // Resources
        public AudioSource audioSource;
        public AudioClip hoverSound;
        public AudioClip clickSound;

        // Settings
        public bool enableHoverSound = true;
        public bool enableClickSound = true;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (audioSource != null && enableHoverSound == true)
                audioSource.PlayOneShot(hoverSound);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (audioSource != null && enableClickSound == true)
                audioSource.PlayOneShot(clickSound);
        }
    }
}