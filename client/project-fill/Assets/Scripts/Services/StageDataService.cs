using System.Collections.Generic;
using Game.Utils;
using ProjectFill.Data.Generated;
using UnityEngine;

namespace Game.Services
{
    public class StageDataService : MonoBehaviour
    {
        public static StageDataService Instance { get; private set; }

        private Stage[] _stages;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _stages = CsvLoader.Load<Stage>(Stage.ResourcePath);
        }

        public Stage GetStage(int stageId)
        {
            if (_stages == null) return null;
            foreach (var s in _stages)
                if (s.stage_id == stageId) return s;
            return null;
        }

        public Stage[] GetAll() => _stages;

        public int MaxStageId()
        {
            int max = 0;
            if (_stages != null)
            {
                foreach (var s in _stages) if (s.stage_id > max) max = s.stage_id;
            }
            return max;
        }
    }
}
