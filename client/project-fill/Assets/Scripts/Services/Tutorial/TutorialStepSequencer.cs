using System;
using System.Collections.Generic;
using ProjectFill.Data.Generated;

namespace Game.Services.Tutorial
{
    public class TutorialStepSequencer
    {
        public event Action<TutorialStep> OnStepChanged;
        public event Action OnComplete;

        private List<TutorialStep> _steps;
        private int _currentIndex = -1;

        public TutorialStep CurrentStep => (_steps != null && _currentIndex >= 0 && _currentIndex < _steps.Count) 
            ? _steps[_currentIndex] 
            : null;

        public bool IsActive => _steps != null && _currentIndex >= 0 && _currentIndex < _steps.Count;

        public void Start(List<TutorialStep> steps)
        {
            _steps = steps;
            _currentIndex = 0;
            if (_steps != null && _steps.Count > 0)
            {
                OnStepChanged?.Invoke(CurrentStep);
            }
            else
            {
                Complete();
            }
        }

        public void Next()
        {
            if (_steps == null) return;

            _currentIndex++;
            if (_currentIndex >= _steps.Count)
            {
                Complete();
            }
            else
            {
                OnStepChanged?.Invoke(CurrentStep);
            }
        }

        public void Complete()
        {
            _currentIndex = -1;
            _steps = null;
            OnComplete?.Invoke();
        }
    }
}
