using System.Collections.Generic;
using Game.InGame.View;
using UnityEngine;

namespace Game.InGame.Controller
{
    public class InGameSceneEntry : MonoBehaviour
    {
        [SerializeField] private InGameController           _controller;
        [SerializeField] private HUDView                    _hudView;   // may be null (prefab missing)
        [SerializeField] private InGameSceneBackgroundView  _sceneBg;
        [SerializeField] private int                        _debugStageId = 1;

        private void Start()
        {
            Screen.orientation      = ScreenOrientation.Portrait;
            Application.targetFrameRate = 60;

            if (_controller == null)
            {
                Debug.LogError("[InGame] InGameController reference missing on SceneEntry");
                return;
            }

            var board = GenerateBoard(_debugStageId);
            _controller.Init(board, _debugStageId);
        }

        private static Board GenerateBoard(int stageId)
        {
            // Difficulty from stage ID
            int numTypes = stageId <= 5  ? 3 :
                           stageId <= 15 ? 4 :
                           stageId <= 30 ? 5 :
                           stageId <= 50 ? 6 : 7;
            int numEmpty = stageId <= 20 ? 2 : 1;
            int numLanes = numTypes + numEmpty;

            // Build chip list (4 of each type)
            var chips = new List<SignalType>(numTypes * SlotLane.Capacity);
            for (int t = 0; t < numTypes; t++)
                for (int i = 0; i < SlotLane.Capacity; i++)
                    chips.Add((SignalType)t);

            // Seeded shuffle for reproducibility per stage
            var rng = new System.Random(stageId * 1337 + 42);
            for (int i = chips.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (chips[i], chips[j]) = (chips[j], chips[i]);
            }

            // Distribute chips into filled lanes (leave numEmpty lanes empty)
            var lanes = new List<SlotLane>(numLanes);
            for (int i = 0; i < numLanes; i++) lanes.Add(new SlotLane());

            int idx = 0;
            for (int l = 0; l < numTypes && idx < chips.Count; l++)
                while (!lanes[l].IsFull && idx < chips.Count)
                    lanes[l].Push(chips[idx++]);

            var board = new Board(lanes, numTypes);

            // Reroll if immediately stuck (rare but possible with seeded shuffle)
            int attempts = 0;
            while (board.IsStuck() && attempts++ < 20)
            {
                board.Shuffle();
            }

            return board;
        }
    }
}
