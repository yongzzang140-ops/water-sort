using System;
using WaterSort.Core;

namespace WaterSort.Gimmicks
{
    /// <summary>
    /// 3. 카운트다운 액체 기믹 (Countdown Liquid)
    /// 병 내부의 액체 층에 카운트다운 숫자가 부여됩니다.
    /// 보드 전체에서 이동이 발생할 때마다 1씩 감소하며, 0이 되면 즉시 스테이지 실패 이벤트가 트리거됩니다.
    /// </summary>
    public class CountdownLiquidGimmick : IGimmick
    {
        public string GimmickName => "CountdownLiquid";

        public int CurrentCount { get; private set; }
        public int InitialCount { get; private set; }

        public event Action<int> OnCountChanged; // (remainingCount)
        public event Action OnCountdownExpired;  // 0 도달 시 즉시 실패

        public CountdownLiquidGimmick(int initialCount = 10)
        {
            InitialCount = initialCount;
            CurrentCount = initialCount;
        }

        public bool CanPourFrom(Bottle bottle) => true;
        public bool CanPourTo(Bottle bottle) => true;

        /// <summary>
        /// 이동 1회 발생 시 카운트다운 차감
        /// </summary>
        public void Tick()
        {
            if (CurrentCount > 0)
            {
                CurrentCount--;
                OnCountChanged?.Invoke(CurrentCount);

                if (CurrentCount <= 0)
                {
                    OnCountdownExpired?.Invoke();
                }
            }
        }

        public void OnMoveCompleted(Bottle bottle)
        {
            // 이동 완료 시 보드 단위에서 Tick 호출
        }

        public void OnLayerRevealed(int layerIndex)
        {
        }
    }
}
