using UnityEngine;
using UnityEngine.UI;

namespace Michsky.UI.Dark
{
    public class CanvasManager : MonoBehaviour
    {
        public CanvasScaler canvasScaler;

        void Start()
        {
            if (canvasScaler == null)
                canvasScaler = gameObject.GetComponent<CanvasScaler>();
        }

        public void ScaleCanvas(int scale = 1080)
        {
            canvasScaler.referenceResolution = new Vector2(canvasScaler.referenceResolution.x, scale);
        }
    }
}