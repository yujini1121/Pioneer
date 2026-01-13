using UnityEngine;

namespace Michsky.UI.Dark
{
    public class LaunchURL : MonoBehaviour
    {
        public string URL;

        public void LaunchUrl()
        {
            Application.OpenURL(URL);
        }
    }
}