using System;
using UnityEngine;

namespace WaterSort.Level
{
    /// <summary>
    /// Casual, Pro, Hell 3단계 난이도 및 이동 횟수(M_limit = M_opt + Delta_M) 제어를 전담하는 매니저
    /// </summary>
    public class DifficultyManager : MonoBehaviour
    {
        public static DifficultyManager Instance { get; private set; }

        [Header("Current Difficulty Settings")]
        [SerializeField] private DifficultyType _currentDifficulty = DifficultyType.Casual;

        private DifficultyConfig _activeConfig;
        private int _optimalMoves = 0;   // M_opt
        private int _limitMoves = 0;     // M_limit
        private int _usedMoves = 0;      // 유저가 현재까지 사용한 이동 횟수

        public event Action<DifficultyType> OnDifficultyChanged;
        public event Action<int, int> OnMovesUpdated; // (usedMoves, limitMoves) - limit가 0이면 무제한
        public event Action OnMovesExhausted;         // 이동 횟수 완전 소진 시 발생

        public DifficultyType CurrentDifficulty => _currentDifficulty;
        public DifficultyConfig ActiveConfig => _activeConfig ?? (_activeConfig = DifficultyConfig.GetDefaultConfig(_currentDifficulty));
        public int OptimalMoves => _optimalMoves;
        public int LimitMoves => _limitMoves;
        public int UsedMoves => _usedMoves;
        public int RemainingMoves => _activeConfig.IsUnlimitedMoves ? int.MaxValue : Mathf.Max(0, _limitMoves - _usedMoves);

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                ApplyDifficulty(_currentDifficulty);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 난이도 모드 변경
        /// </summary>
        public void SetDifficulty(DifficultyType difficulty)
        {
            if (_currentDifficulty == difficulty && _activeConfig != null) return;

            _currentDifficulty = difficulty;
            _activeConfig = DifficultyConfig.GetDefaultConfig(difficulty);
            OnDifficultyChanged?.Invoke(_currentDifficulty);
        }

        private void ApplyDifficulty(DifficultyType difficulty)
        {
            _activeConfig = DifficultyConfig.GetDefaultConfig(difficulty);
        }

        /// <summary>
        /// 새로운 스테이지 시작 시 Solver가 산출한 M_opt를 기반으로 이동 제한 설정
        /// </summary>
        public void InitializeLevelLimit(int optimalMoves)
        {
            _optimalMoves = Mathf.Max(1, optimalMoves);
            _usedMoves = 0;

            if (_activeConfig.IsUnlimitedMoves)
            {
                _limitMoves = 0; // 0은 무제한을 의미
            }
            else
            {
                // M_limit = M_opt + Delta_M
                _limitMoves = _optimalMoves + _activeConfig.DeltaMoves;
            }

            OnMovesUpdated?.Invoke(_usedMoves, _limitMoves);
        }

        /// <summary>
        /// 플레이어가 액체를 한 번 부었을 때 호출
        /// </summary>
        public bool RecordMove()
        {
            _usedMoves++;
            OnMovesUpdated?.Invoke(_usedMoves, _limitMoves);

            if (!_activeConfig.IsUnlimitedMoves && _usedMoves >= _limitMoves)
            {
                OnMovesExhausted?.Invoke();
                return false; // 더 이상 이동 불가능
            }

            return true;
        }

        /// <summary>
        /// 되돌리기(Undo) 시 이동 수 복원
        /// </summary>
        public void RevertMove()
        {
            if (_usedMoves > 0)
            {
                _usedMoves--;
                OnMovesUpdated?.Invoke(_usedMoves, _limitMoves);
            }
        }

        /// <summary>
        /// 2차 개발의 Fail Offer 또는 광고 시청 시 이동 횟수 보너스 연장
        /// </summary>
        public void AddExtraMoves(int extraMoves)
        {
            _limitMoves += extraMoves;
            OnMovesUpdated?.Invoke(_usedMoves, _limitMoves);
        }
    }
}
