using System;
using System.Collections.Generic;
using UnityEngine;
using WaterSort.Core;
using WaterSort.Level;

namespace WaterSort.Gimmicks
{
    /// <summary>
    /// 어른 모드(Adult Mode: Casual, Pro, Hell) 전용 4종 특수 기믹 오케스트레이터.
    /// 스테이지 난이도에 따라 블라인드, 자물쇠, 카운트다운, 얼음 기믹을 동적으로 배치하고 제어합니다.
    /// (어린이 모드에서는 순수 힐링을 위해 기믹이 적용되지 않습니다.)
    /// </summary>
    public class GimmickManager : MonoBehaviour
    {
        public static GimmickManager Instance { get; private set; }

        private readonly Dictionary<int, List<IGimmick>> _bottleGimmicks = new Dictionary<int, List<IGimmick>>();
        private readonly List<CountdownLiquidGimmick> _activeCountdowns = new List<CountdownLiquidGimmick>();
        private readonly List<LockedTubeGimmick> _activeLocks = new List<LockedTubeGimmick>();

        public event Action OnCountdownFailed; // 카운트다운 0 도달 시 실패 이벤트

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
        /// 새로운 스테이지 시작 시 어른 모드 여부를 검사하고 기믹을 초기화합니다.
        /// </summary>
        public void SetupGimmicksForStage(int stageLevel, DifficultyType difficulty, List<Bottle> bottles)
        {
            ClearAllGimmicks();

            // ⚠️ 중요: 어린이 모드(Kid)에서는 기믹을 절대 추가하지 않음
            if (difficulty == DifficultyType.Kid)
            {
                return;
            }

            if (bottles == null || bottles.Count < 3) return;

            // 1. 블라인드 튜브 (Stage 3 이상, 어른 모드)
            if (stageLevel >= 3)
            {
                int blindTargetIdx = 0;
                var blindGimmick = new BlindTubeGimmick(hiddenCount: 2);
                AddGimmick(bottles[blindTargetIdx].BottleId, blindGimmick);
            }

            // 2. 얼음 레이어 기믹 (Stage 6 이상)
            if (stageLevel >= 6)
            {
                int frozenTargetIdx = Mathf.Min(1, bottles.Count - 2);
                var frozenGimmick = new FrozenLayerGimmick(hitsRequired: 2);
                AddGimmick(bottles[frozenTargetIdx].BottleId, frozenGimmick);
            }

            // 3. 자물쇠 튜브 기믹 (Stage 8 이상, Pro/Hell 중심)
            if (stageLevel >= 8 && bottles.Count >= 6)
            {
                int lockTargetIdx = bottles.Count - 2; // 뒤쪽 빈 병 바로 앞
                LiquidColor keyCol = bottles[0].TopColor; // 첫 번째 병의 색상을 열쇠로 설정
                var lockedGimmick = new LockedTubeGimmick(keyCol);
                AddGimmick(bottles[lockTargetIdx].BottleId, lockedGimmick);
                _activeLocks.Add(lockedGimmick);
            }

            // 4. 카운트다운 액체 기믹 (Stage 10 이상 또는 Hell 모드)
            if (stageLevel >= 10 || difficulty == DifficultyType.AdultHell)
            {
                int cdTargetIdx = Mathf.Min(2, bottles.Count - 3);
                int initialCount = (difficulty == DifficultyType.AdultHell) ? 12 : 16;
                var countdownGimmick = new CountdownLiquidGimmick(initialCount);
                countdownGimmick.OnCountdownExpired += HandleCountdownExpired;
                AddGimmick(bottles[cdTargetIdx].BottleId, countdownGimmick);
                _activeCountdowns.Add(countdownGimmick);
            }
        }

        public void AddGimmick(int bottleId, IGimmick gimmick)
        {
            if (!_bottleGimmicks.ContainsKey(bottleId))
            {
                _bottleGimmicks[bottleId] = new List<IGimmick>();
            }
            _bottleGimmicks[bottleId].Add(gimmick);
        }

        public List<IGimmick> GetGimmicks(int bottleId)
        {
            return _bottleGimmicks.TryGetValue(bottleId, out var list) ? list : null;
        }

        public T GetGimmick<T>(int bottleId) where T : class, IGimmick
        {
            if (_bottleGimmicks.TryGetValue(bottleId, out var list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] is T match) return match;
                }
            }
            return null;
        }

        /// <summary>
        /// 이동이 발생할 때마다 카운트다운 차감 및 자물쇠 해금 상태 검사
        /// </summary>
        public void NotifyMoveExecuted(List<Bottle> allBottles)
        {
            // 1. 카운트다운 기믹 일괄 차감
            for (int i = 0; i < _activeCountdowns.Count; i++)
            {
                _activeCountdowns[i].Tick();
            }

            // 2. 자물쇠 기믹 해금 조건 검사 (열쇠 색상이 완전히 정렬된 병이 생겼는지 확인)
            for (int i = 0; i < _activeLocks.Count; i++)
            {
                var lockGimmick = _activeLocks[i];
                if (lockGimmick.IsLocked)
                {
                    for (int b = 0; b < allBottles.Count; b++)
                    {
                        var bottle = allBottles[b];
                        if (bottle.IsCompletedSingleColor && bottle.TopColor == lockGimmick.RequiredKeyColor)
                        {
                            lockGimmick.Unlock();
                            break;
                        }
                    }
                }
            }
        }

        private void HandleCountdownExpired()
        {
            OnCountdownFailed?.Invoke();
        }

        public void ClearAllGimmicks()
        {
            for (int i = 0; i < _activeCountdowns.Count; i++)
            {
                _activeCountdowns[i].OnCountdownExpired -= HandleCountdownExpired;
            }
            _activeCountdowns.Clear();
            _activeLocks.Clear();
            _bottleGimmicks.Clear();
        }
    }
}
