using System.Collections.Generic;
using Game.Utils;
using ProjectFill.Data.Generated;
using UnityEngine;

namespace Game.Services
{
    public class StageDataService : MonoBehaviour
    {
        private static StageDataService _instance;

        // Lazy-instantiated if not placed in scene (e.g. entering InGame directly without Boot), so
        // stage data is always available rather than silently falling back to hardcoded samples.
        public static StageDataService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<StageDataService>();
                    if (_instance == null)
                        _instance = new GameObject(nameof(StageDataService)).AddComponent<StageDataService>();
                }
                return _instance;
            }
        }

        private Stage[] _stages;

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureLoaded();
        }

        private void EnsureLoaded()
        {
            if (_stages == null) _stages = CsvLoader.Load<Stage>(Stage.ResourcePath);
        }

        public Stage GetStage(int stageId)
        {
            EnsureLoaded();
            if (_stages == null) return null;
            foreach (var s in _stages)
                if (s.stage_id == stageId) return s;
            return null;
        }

        public Stage[] GetAll() { EnsureLoaded(); return _stages; }

        public int MaxStageId()
        {
            EnsureLoaded();
            int max = 0;
            if (_stages != null)
            {
                foreach (var s in _stages) if (s.stage_id > max) max = s.stage_id;
            }
            return max;
        }
    }
}
