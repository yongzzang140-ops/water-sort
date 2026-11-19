using System;
using WaterSort.Core;

namespace WaterSort.Gimmicks
{
    /// <summary>
    /// 2. 자물쇠 튜브 기믹 (Locked Tube)
    /// 사슬로 잠겨 있어 액체 투입 및 추출이 모두 차단됩니다.
    /// 특정 열쇠 액체 색상이 정렬되거나 다른 병이 완성되었을 때 자물쇠가 풀립니다.
    /// </summary>
    public class LockedTubeGimmick : IGimmick
    {
        public string GimmickName => "LockedTube";

        public bool IsLocked { get; private set; }
        public LiquidColor RequiredKeyColor { get; private set; }

        public event Action OnUnlocked;

        public LockedTubeGimmick(LiquidColor requiredKeyColor)
        {
            IsLocked = true;
            RequiredKeyColor = requiredKeyColor;
        }

        public bool CanPourFrom(Bottle bottle)
        {
            // 잠겨있는 동안은 액체를 쏟을 수 없음
            return !IsLocked;
        }

        public bool CanPourTo(Bottle bottle)
        {
            // 잠겨있는 동안은 액체를 부을 수 없음
            return !IsLocked;
        }

        public void Unlock()
        {
            if (IsLocked)
            {
                IsLocked = false;
                OnUnlocked?.Invoke();
            }
        }

        public void OnMoveCompleted(Bottle bottle)
        {
            // 이동 완료 시 상태 점검 (필요 시 자동 해금 트리거 연결)
        }

        public void OnLayerRevealed(int layerIndex)
        {
        }
    }
}
