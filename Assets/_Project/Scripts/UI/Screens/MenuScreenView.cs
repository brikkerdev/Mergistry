using System;
using UnityEngine;
using UnityEngine.UI;

namespace Mergistry.UI.Screens
{
    public class MenuScreenView : MonoBehaviour
    {
        [SerializeField] private Button startButton;

        public event Action OnStartClicked;

        private void Start()
        {
            if (startButton == null)
                startButton = GetComponentInChildren<Button>(true);

            startButton?.onClick.AddListener(() => OnStartClicked?.Invoke());
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
