using System;
using System.Collections.Generic;
using UnityEngine;
using WaterSort.Core;
using WaterSort.Level;

namespace WaterSort.Solver
{
    /// <summary>
    /// BFS 최적수(M_opt) 기반 스테이지별 허용 이동 횟수(M_limit) 산출 및 잔여 수 제어 매니저.
    /// 이동 횟수 소진(0회) 시 OnStageFailed 이벤트를 발행하여 즉시구제(Fail Offer) 모달을 호출합니다.
    /// </summary>
    public class MoveLimitManager : MonoBehaviour
    {
        public static MoveLimitManager Instance { get; private set; }

        [Header("State")]
        [SerializeField] private int _optimalMoves = 0;   // M_opt
        [SerializeField] private int _limitMoves = 0;     // M_limit
        [SerializeField] private int _usedMoves = 0;      // 유저가 사용한 횟수
        [SerializeField] private bool _isUnlimited = false;

        public event Action<int, int> OnMovesChanged; // (used, limit)
        public event Action OnStageFailed;            // 잔여 이동 수 0 도달 시 실패 이벤트

        public int OptimalMoves => _optimalMoves;
        public int LimitMoves => _limitMoves;
        public int UsedMoves => _usedMoves;
        public int RemainingMoves => _isUnlimited ? int.MaxValue : Mathf.Max(0, _limitMoves - _usedMoves);
        public bool IsUnlimited => _isUnlimited;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 새로운 스테이지 시작 시 난이도 및 BFS 최적수를 기반으로 M_limit를 확정합니다.
        /// </summary>
        public void InitializeLimit(DifficultyType difficulty, List<Bottle> bottles)
        {
            _usedMoves = 0;

            // 1. 어린이 모드는 이동 횟수 완전 무제한
            if (difficulty == DifficultyType.Kid)
            {
                _isUnlimited = true;
                _optimalMoves = 0;
                _limitMoves = 0;
                OnMovesChanged?.Invoke(_usedMoves, _limitMoves);
                return;
            }

            // 2. 어른 모드는 BFS 최적수(M_opt) 연산 수행
            _isUnlimited = false;
            _optimalMoves = BoardSolver.CalculateOptimalMoves(bottles);

            // 3. PRD 2.2 규칙: M_limit = M_opt + Delta_M
            int deltaM = difficulty switch
            {
                DifficultyType.AdultHell => 0,    // 헬: 오차 불허 (M_opt + 0)
                DifficultyType.AdultPro => 3,     // 프로: M_opt + 3회
                _ => 15                           // 일반(Casual): M_opt + 15회
            };

            _limitMoves = _optimalMoves + deltaM;
            OnMovesChanged?.Invoke(_usedMoves, _limitMoves);
        }

        /// <summary>
        /// 플레이어가 액체 붓기(Pour)를 실행할 때마다 호출
        /// </summary>
        public bool ConsumeMove()
        {
            _usedMoves++;
            OnMovesChanged?.Invoke(_usedMoves, _limitMoves);

            // 제한 모드이고 남은 이동 횟수가 0 이하가 된 경우
            if (!_isUnlimited && _usedMoves >= _limitMoves)
            {
                OnStageFailed?.Invoke();
                return false;
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
                OnMovesChanged?.Invoke(_usedMoves, _limitMoves);
            }
        }

        /// <summary>
        /// Fail Offer 구매 또는 리워드 광고 시청 완료 시 즉시 +5회 연장
        /// </summary>
        public void AddExtraMoves(int extraMoves = 5)
        {
            _limitMoves += extraMoves;
            OnMovesChanged?.Invoke(_usedMoves, _limitMoves);
        }
    }
}
