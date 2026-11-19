using System;
using System.Collections;
using UnityEngine;
using WaterSort.Core;

namespace WaterSort.View
{
    /// <summary>
    /// 액체 부어넣기(Pour) 시 병의 틸팅(기울기), 이동, 액체 수면 변동 및 파티클 연출을 제어하는 인터페이스
    /// </summary>
    public interface IPourAnimationHandler
    {
        /// <summary>
        /// 소스 병에서 타깃 병으로 액체가 부어지는 전 과정 애니메이션 코루틴
        /// </summary>
        IEnumerator PlayPourAnimationRoutine(
            BottleView sourceView,
            BottleView targetView,
            LiquidColor color,
            int amount,
            Action onComplete);

        /// <summary>
        /// 병 하나가 단일 색상 4층으로 완전히 정렬되었을 때의 축하 시각 효과 (스파클, 플래시 등)
        /// </summary>
        void PlayCompletionEffect(BottleView bottleView);
    }
}
