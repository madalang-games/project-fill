using System;
using System.Collections.Generic;
using ProjectFill.Contracts.Tutorial;
using UnityEngine;

namespace Game.Services
{
    public class TutorialApiService : MonoBehaviour
    {
        private static TutorialApiService _instance;
        public static TutorialApiService Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogError("[TutorialApiService] Instance is missing! Ensure it is placed in the Boot scene as a GameObject.");
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void FetchProgress(Action<TutorialProgressResponse> onSuccess = null, Action<string> onError = null)
        {
            NetworkService.Instance.Get("/api/tutorial/progress", (ok, result) =>
            {
                if (!ok) { onError?.Invoke(result); return; }
                var json = JsonUtility.FromJson<TutorialProgressResponseJson>(result);
                onSuccess?.Invoke(json.ToContract());
            });
        }

        public void CompleteTutorial(int tutorialId, Action<TutorialProgressUpdateResponse> onSuccess = null, Action<string> onError = null)
        {
            NetworkService.Instance.Post($"/api/tutorial/progress/{tutorialId}", "{}", (ok, result) =>
            {
                if (!ok) { onError?.Invoke(result); return; }
                var json = JsonUtility.FromJson<TutorialProgressUpdateResponseJson>(result);
                onSuccess?.Invoke(json.ToContract());
            });
        }

        [Serializable]
        private class TutorialProgressResponseJson
        {
            public List<int> completedTutorialIds;
            public TutorialProgressResponse ToContract() => new TutorialProgressResponse { CompletedTutorialIds = completedTutorialIds ?? new List<int>() };
        }

        [Serializable]
        private class TutorialProgressUpdateResponseJson
        {
            public bool success;
            public List<int> completedTutorialIds;
            public TutorialProgressUpdateResponse ToContract() => new TutorialProgressUpdateResponse 
            { 
                Success = success, 
                CompletedTutorialIds = completedTutorialIds ?? new List<int>() 
            };
        }
    }
}
