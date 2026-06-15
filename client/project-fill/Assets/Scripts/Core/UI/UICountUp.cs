using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Game.Core.UI
{
    [RequireComponent(typeof(TMP_Text))]
    public class UICountUp : MonoBehaviour
    {
        [SerializeField] private float  _duration     = 0.6f;
        [SerializeField] private string _formatString = "{0:N0}";

        private TMP_Text _text;

        private void Awake() => _text = GetComponent<TMP_Text>();

        public void Play(int from, int to, Action onComplete = null)
            => StartCoroutine(CountSequence(from, to, onComplete));

        public void Play(int to, Action onComplete = null)
            => Play(0, to, onComplete);

        private IEnumerator CountSequence(int from, int to, Action onComplete)
        {
            float elapsed = 0f;
            while (elapsed < _duration)
            {
                elapsed += Time.deltaTime;
                float t    = UIEasing.EaseOut(Mathf.Clamp01(elapsed / _duration));
                int   val  = Mathf.RoundToInt(Mathf.Lerp(from, to, t));
                _text.text = string.Format(_formatString, val);
                yield return null;
            }
            _text.text = string.Format(_formatString, to);
            onComplete?.Invoke();
        }
    }
}
