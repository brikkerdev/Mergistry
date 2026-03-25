using System;
using UnityEngine;
using UnityEngine.UI;

namespace Mergistry.UI.Screens
{
    public class MenuScreenView : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button continueButton;  // A8: shown when a run save exists

        public event Action OnStartClicked;
        public event Action OnContinueClicked;

        private void Start()
        {
            if (startButton == null)
                startButton = GetComponentInChildren<Button>(true);

            startButton?.onClick.AddListener(() => OnStartClicked?.Invoke());
            continueButton?.onClick.AddListener(() => OnContinueClicked?.Invoke());

            // Hide continue button by default; MenuState enables it when save exists
            if (continueButton != null)
                continueButton.gameObject.SetActive(false);
        }

        public void ShowContinueButton(bool show)
        {
            if (continueButton != null)
                continueButton.gameObject.SetActive(show);
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
