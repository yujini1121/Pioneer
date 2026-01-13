using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Michsky.UI.Dark
{
    public class MainPanelManager : MonoBehaviour
    {
        // List
        public List<PanelItem> panels = new List<PanelItem>();

        // Settings
        public bool settingsHelper;
        public bool instantInOnEnable;
        public int currentPanelIndex = 0;
        private int currentButtonIndex = 0;
        private int newPanelIndex;
        [Range(0.75f, 4)] public float disablePanelAfter = 1;
        [Range(0, 1)] public float animationSmoothness = 0.25f;
        [Range(0.75f, 4)] public float animationSpeed = 1;

        // Hidden vars
        private GameObject currentPanel;
        private GameObject nextPanel;
        private GameObject currentButton;
        private GameObject nextButton;

        private Animator currentPanelAnimator;
        private Animator nextPanelAnimator;
        private Animator currentButtonAnimator;
        private Animator nextButtonAnimator;

        // Animator state vars
        string panelFadeIn = "Panel In";
        string panelFadeOut = "Panel Out";
        string panelInstantIn = "Instant In";
        string buttonFadeIn = "Hover to Pressed";
        string buttonFadeOut = "Pressed to Normal";
        string buttonFadeNormal = "Pressed to Normal";

        bool firstTime = true;
        [HideInInspector] public bool gamepadEnabled = false;

        [System.Serializable]
        public class PanelItem
        {
            public string panelName = "My Panel";
            public GameObject panelObject;
            public GameObject panelButton;
            public GameObject defaultSelected;
        }

        void OnEnable()
        {
            if (panels[currentPanelIndex].panelButton != null)
            {
                currentButton = panels[currentPanelIndex].panelButton;
                currentButtonAnimator = currentButton.GetComponent<Animator>();
                currentButtonAnimator.Play(buttonFadeIn);
            }

            currentPanel = panels[currentPanelIndex].panelObject;
            currentPanel.SetActive(true);
            currentPanelAnimator = currentPanel.GetComponent<Animator>();

            if (instantInOnEnable == true && currentPanelAnimator.gameObject.activeInHierarchy == true)
                currentPanelAnimator.Play(panelInstantIn);
            else if (instantInOnEnable == false && currentPanelAnimator.gameObject.activeInHierarchy == true)
                currentPanelAnimator.Play(panelFadeIn);

            firstTime = false;

            for (int i = 0; i < panels.Count; i++)
            {
                if (panels[i].panelObject == null) { continue; }
                if (i != currentPanelIndex) { panels[i].panelObject.SetActive(false); }
            }
        }

        public void EnableFirstPanel()
        {
            try
            {
                panels[currentPanelIndex].panelObject.GetComponent<Animator>().Play("Instant In");
                panels[currentPanelIndex].panelButton.GetComponent<Animator>().Play("Instant In");
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(panels[currentPanelIndex].panelObject.GetComponent<RectTransform>());
            }

            catch { }
        }

        public void OpenFirstTab()
        {
            if (currentPanelIndex != 0)
                OpenPanel(panels[0].panelName);

            else if (currentPanelIndex == 0 && settingsHelper == true && firstTime == false)
            {
                OpenPanel(panels[1].panelName);
                OpenPanel(panels[0].panelName);

                if (panels[0].defaultSelected != null && gamepadEnabled == true)
                    EventSystem.current.SetSelectedGameObject(panels[0].defaultSelected);
            }
        }

        public void OpenPanel(string newPanel)
        {
            for (int i = 0; i < panels.Count; i++)
            {
                if (panels[i].panelName == newPanel)
                    newPanelIndex = i;
            }

            if (newPanelIndex != currentPanelIndex)
            {
                StopCoroutine("DisablePreviousPanel");

                currentPanel = panels[currentPanelIndex].panelObject;
                currentPanelIndex = newPanelIndex;
                nextPanel = panels[currentPanelIndex].panelObject;
                nextPanel.SetActive(true);

                currentPanelAnimator = currentPanel.GetComponent<Animator>();
                nextPanelAnimator = nextPanel.GetComponent<Animator>();

                currentPanelAnimator.SetFloat("Anim Speed", animationSpeed);
                currentPanelAnimator.CrossFade(panelFadeOut, animationSmoothness);
                nextPanelAnimator.SetFloat("Anim Speed", animationSpeed);
                nextPanelAnimator.CrossFade(panelFadeIn, animationSmoothness);

                StartCoroutine("DisablePreviousPanel");

                if (panels[currentButtonIndex].panelButton != null)
                    currentButton = panels[currentButtonIndex].panelButton;

                currentButtonIndex = newPanelIndex;

                if (panels[currentButtonIndex].panelButton != null)
                {
                    nextButton = panels[currentButtonIndex].panelButton;
                    currentButtonAnimator = currentButton.GetComponent<Animator>();
                    nextButtonAnimator = nextButton.GetComponent<Animator>();
                    currentButtonAnimator.Play(buttonFadeOut);
                    nextButtonAnimator.Play(buttonFadeIn);
                }

                if (panels[currentPanelIndex].defaultSelected != null && gamepadEnabled == true)
                    EventSystem.current.SetSelectedGameObject(panels[currentPanelIndex].defaultSelected);
            }
        }

        public void NextPage()
        {
            if (currentPanelIndex >= panels.Count - 1)
            {
                OpenPanel(panels[0].panelName);
                return;
            }

            StopCoroutine("DisablePreviousPanel");

            currentPanel = panels[currentPanelIndex].panelObject;

            if (panels[currentButtonIndex].panelButton != null)
                currentButton = panels[currentButtonIndex].panelButton;

            if (panels[currentButtonIndex + 1].panelButton != null)
                nextButton = panels[currentButtonIndex + 1].panelButton;

            currentPanel.gameObject.SetActive(true);
            currentPanelAnimator = currentPanel.GetComponent<Animator>();

            if (currentButton != null)
            {
                currentButtonAnimator = currentButton.GetComponent<Animator>();
                currentButtonAnimator.Play(buttonFadeNormal);
            }

            currentPanelAnimator.SetFloat("Anim Speed", animationSpeed);
            currentPanelAnimator.CrossFade(panelFadeOut, animationSmoothness);

            currentPanelIndex += 1;
            currentButtonIndex += 1;

            nextPanel = panels[currentPanelIndex].panelObject;
            nextPanel.gameObject.SetActive(true);

            nextPanelAnimator = nextPanel.GetComponent<Animator>();
            nextPanelAnimator.SetFloat("Anim Speed", animationSpeed);
            nextPanelAnimator.CrossFade(panelFadeIn, animationSmoothness);

            if (nextButton != null)
            {
                nextButtonAnimator = nextButton.GetComponent<Animator>();
                nextButtonAnimator.Play(buttonFadeIn);
            }

            if (panels[currentPanelIndex].defaultSelected != null && gamepadEnabled == true)
                EventSystem.current.SetSelectedGameObject(panels[currentPanelIndex].defaultSelected);

        }

        public void PrevPage()
        {
            if (currentPanelIndex <= 0)
            {
                OpenPanel(panels[panels.Count - 1].panelName);
                return;
            }

            StopCoroutine("DisablePreviousPanel");

            currentPanel = panels[currentPanelIndex].panelObject;

            if (panels[currentButtonIndex].panelButton != null)
                currentButton = panels[currentButtonIndex].panelButton;

            if (panels[currentButtonIndex - 1].panelButton != null)
                nextButton = panels[currentButtonIndex - 1].panelButton;

            currentPanel.gameObject.SetActive(true);
            currentPanelAnimator = currentPanel.GetComponent<Animator>();

            if (currentButton != null)
            {
                currentButtonAnimator = currentButton.GetComponent<Animator>();
                currentButtonAnimator.Play(buttonFadeNormal);
            }

            currentPanelAnimator.SetFloat("Anim Speed", animationSpeed);
            currentPanelAnimator.CrossFade(panelFadeOut, animationSmoothness);

            currentPanelIndex -= 1;
            currentButtonIndex -= 1;

            nextPanel = panels[currentPanelIndex].panelObject;
            nextPanel.gameObject.SetActive(true);

            nextPanelAnimator = nextPanel.GetComponent<Animator>();
            nextPanelAnimator.SetFloat("Anim Speed", animationSpeed);
            nextPanelAnimator.CrossFade(panelFadeIn, animationSmoothness);

            if (nextButton != null)
            {
                nextButtonAnimator = nextButton.GetComponent<Animator>();
                nextButtonAnimator.Play(buttonFadeIn);
            }

            if (panels[currentPanelIndex].defaultSelected != null && gamepadEnabled == true)
                EventSystem.current.SetSelectedGameObject(panels[currentPanelIndex].defaultSelected);
        }

        public void AddNewItem()
        {
            PanelItem newPanel = new PanelItem();
            panels.Add(newPanel);
        }

        IEnumerator DisablePreviousPanel()
        {
            yield return new WaitForSecondsRealtime(disablePanelAfter);
            currentPanel.SetActive(false);
        }
    }
}