using UnityEngine;

namespace Game.Services
{
    public class StageApiService : MonoBehaviour
    {
        private static StageApiService _instance;
        public static StageApiService Instance => _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
