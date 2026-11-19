using System;
using WaterSort.Core;

namespace WaterSort.Gimmicks
{
    /// <summary>
    /// 병(Bottle)에 부착되는 특수 기믹 데코레이터 인터페이스
    /// </summary>
    public interface IGimmick
    {
        string GimmickName { get; }
        bool CanPourFrom(Bottle bottle);
        bool CanPourTo(Bottle bottle);
        void OnMoveCompleted(Bottle bottle);
        void OnLayerRevealed(int layerIndex);
    }
}
