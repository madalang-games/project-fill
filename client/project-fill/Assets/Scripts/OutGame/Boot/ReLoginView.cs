using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.OutGame.Boot
{
    public class ReLoginView : MonoBehaviour
    {
        [SerializeField] private Button _reLoginButton;
        [SerializeField] private Button _continueAsGuestButton;

        private Action _onReLogin;
        private Action _onContinueAsGuest;

        public void Init(Action onReLogin, Action onContinueAsGuest)
        {
            _onReLogin         = onReLogin;
            _onContinueAsGuest = onContinueAsGuest;
            _reLoginButton.onClick.AddListener(() => _onReLogin?.Invoke());
            _continueAsGuestButton.onClick.AddListener(() => _onContinueAsGuest?.Invoke());
        }
    }
}
