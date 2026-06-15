using System.Collections;
using UnityEngine;

namespace Game.Core.UI
{
    public class UIStarPop : MonoBehaviour
    {
        private const float PopDuration = 0.38f;
        private const float StarDelay   = 0.25f;
        private const float PunchPeak   = 1.35f;

        // All stars show empty state immediately; earned fills pop in left-to-right, independently.
        public IEnumerator PlayStarSequence(GameObject[] stars, int filledCount)
        {
            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] == null) continue;
                stars[i].SetActive(true);
                stars[i].transform.localScale = Vector3.one;
                var fill = stars[i].transform.Find("Fill");
                if (fill != null) fill.gameObject.SetActive(false);
            }

            for (int i = 0; i < filledCount && i < stars.Length; i++)
            {
                if (stars[i] != null)
                    StartCoroutine(PopFill(stars[i], i * StarDelay));
            }

            float total = filledCount > 0 ? (filledCount - 1) * StarDelay + PopDuration + 0.1f : 0f;
            yield return new WaitForSeconds(total);
        }

        private IEnumerator PopFill(GameObject star, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);

            var fill = star.transform.Find("Fill");
            if (fill == null) yield break;

            fill.gameObject.SetActive(true);
            var rt = fill.GetComponent<RectTransform>();
            if (rt == null) yield break;

            rt.localScale = Vector3.zero;
            StartCoroutine(PunchScale(star.transform));

            float elapsed = 0f;
            while (elapsed < PopDuration)
            {
                elapsed += Time.deltaTime;
                float t = UIEasing.EaseOutBack(Mathf.Clamp01(elapsed / PopDuration));
                rt.localScale = Vector3.one * t;
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        // Bell-curve scale punch on the whole star GO for satisfying impact feel.
        private IEnumerator PunchScale(Transform t)
        {
            float elapsed = 0f;
            while (elapsed < PopDuration)
            {
                elapsed += Time.deltaTime;
                float p = Mathf.Clamp01(elapsed / PopDuration);
                float s = 1f + (PunchPeak - 1f) * Mathf.Sin(p * Mathf.PI);
                t.localScale = Vector3.one * s;
                yield return null;
            }
            t.localScale = Vector3.one;
        }
    }
}
