using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Michsky.UI.Dark
{
    public class BackgroundFilter : MonoBehaviour
    {
        [Range(0, 0.75f)] public float filterIntensity = 0.25f;
        public int selectedFilter;
        public List<Sprite> filterList = new List<Sprite>();
        public Image filterImage;
        public bool editMode = false;
    }
}