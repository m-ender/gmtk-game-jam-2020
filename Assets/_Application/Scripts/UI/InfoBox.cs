﻿using GMTK2020.Data;
using GMTK2020.TutorialSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GMTK2020.UI
{
    public class InfoBox : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI infoText;
        [SerializeField] Button dismissTutorialButton;

        private TutorialManager tutorialManager;

        private void Start()
        {
            tutorialManager = TutorialManager.Instance;

            tutorialManager.TutorialReady += OnTutorialReady;
            tutorialManager.TutorialCompleted += OnTutorialCompleted;
        }

        private void OnDestroy()
        {
            tutorialManager.TutorialReady -= OnTutorialReady;
            tutorialManager.TutorialCompleted -= OnTutorialCompleted;
        }

        public void DismissTutorial()
        {
            tutorialManager.CompleteActiveTutorial();
        }

        private void OnTutorialReady(Tutorial tutorial)
        {
            infoText.text = tutorial.Message;
            if (tutorial.ShowDismissButton)
                dismissTutorialButton.ActivateObject();
        }

        private void OnTutorialCompleted(Tutorial tutorial)
        {
            infoText.text = "";
            dismissTutorialButton.DeactivateObject();
        }
    } 
}
