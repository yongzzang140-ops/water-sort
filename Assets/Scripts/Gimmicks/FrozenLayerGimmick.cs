using System;
using WaterSort.Core;

namespace WaterSort.Gimmicks
{
    /// <summary>
    /// 4. 얼음/점성 액체 기믹 (Frozen Layer)
    /// 얼어있는 액체 층은 첫 번째 붓기 시도 시 얼음이 깨지며 1회 이동 수를 소진하고,
    /// 두 번째 시도에 비로소 실제 액체가 이동하여 유저의 제한 이동 수를 압박합니다.
    /// </summary>
    public class FrozenLayerGimmick : IGimmick
    {
        public string GimmickName => "FrozenLayer";

        // 얼음 내구도 (기본 2: 1타에 깨짐, 2타에 이동)
        public int FreezeHitsRemaining { get; private set; }

        public bool IsFrozen => FreezeHitsRemaining > 1;

        public event Action<int> OnHitIce;    // 남아있는 내구도 전달
        public event Action OnThawed;         // 완전히 녹았을 때 발생

        public FrozenLayerGimmick(int hitsRequired = 2)
        {
            FreezeHitsRemaining = hitsRequired;
        }

        public bool CanPourFrom(Bottle bottle)
        {
            // 얼어있더라도 탭해서 얼음을 깨는 상호작용은 허용해야 함
            return true;
        }

        public bool CanPourTo(Bottle bottle)
        {
            return true;
        }

        /// <summary>
        /// 부어넣기 시도 시 얼음 타격 처리.
        /// 반환값 true: 얼음이 깨지는 턴으로 실제 액체 이동은 보류.
        /// 반환값 false: 이미 해동되어 정상적인 액체 이동 진행.
        /// </summary>
        public bool TryHitIce()
        {
            if (IsFrozen)
            {
                FreezeHitsRemaining--;
                OnHitIce?.Invoke(FreezeHitsRemaining);

                if (!IsFrozen)
                {
                    OnThawed?.Invoke();
                }
                return true; // 얼음 깨짐 액션 소진
            }
            return false; // 이미 녹아 정상 이동
        }

        public void OnMoveCompleted(Bottle bottle)
        {
        }

        public void OnLayerRevealed(int layerIndex)
        {
        }
    }
}
