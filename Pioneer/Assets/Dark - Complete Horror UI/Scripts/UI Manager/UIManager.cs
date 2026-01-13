using UnityEngine;
using UnityEngine.Video;
using TMPro;

namespace Michsky.UI.Dark
{
    [CreateAssetMenu(fileName = "New UI Manager", menuName = "Dark UI/New UI Manager")]
    public class UIManager : ScriptableObject
    {
        public bool enableDynamicUpdate = true;
        public bool enableExtendedColorPicker = true;
        public bool editorHints = true;
        public bool changeRootFolder = true;
        public string rootFolder = "Dark - Complete Horror UI/Prefabs/";

        // Background
        public Color backgroundColorTint = new Color(255, 255, 255, 255);
        public BackgroundType backgroundType;
        public Sprite backgroundImage;
        public VideoClip backgroundVideo;
        public bool backgroundPreserveAspect;
        [Range(0.1f, 5)] public float backgroundSpeed = 1;

        // Colors
        public Color primaryColor = new Color(255, 255, 255, 255);
        public Color primaryReversed = new Color(255, 255, 255, 255);
        public Color secondaryColor = new Color(255, 255, 255, 255);
        public Color negativeColor = new Color(255, 255, 255, 255);
        public Color backgroundColor = new Color(255, 255, 255, 255);
        public Color backgroundColorAlt = new Color(255, 255, 255, 255);

        // Fonts
        public TMP_FontAsset lightFont;
        public TMP_FontAsset mediumFont;
        public TMP_FontAsset boldFont;
        public TMP_FontAsset altFont;
        public TMP_FontAsset alt2Font;

        // Logo
        public Sprite brandLogo;
        public Sprite gameLogo;
        public Color logoColor = new Color(255, 255, 255, 255);

        public enum BackgroundType
        {
            BASIC,
            ADVANCED
        }
    }
}