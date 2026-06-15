using UnityEngine;
using UnityEngine.UI;

namespace Game.OutGame.Boot
{
    public class BootView : MonoBehaviour
    {
        [SerializeField] private Image _logoImage;
        [SerializeField] private GameObject _spinner;

        public void SetSpinnerActive(bool active)
        {
            if (_spinner != null) _spinner.SetActive(active);
        }
    }
}
