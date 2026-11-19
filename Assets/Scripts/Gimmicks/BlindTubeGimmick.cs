using System;
using System.Collections.Generic;
using WaterSort.Core;

namespace WaterSort.Gimmicks
{
    /// <summary>
    /// 1. 블라인드 튜브 기믹 (Blind Tube)
    /// 상단 1~2층만 공개되고 하단 층은 물음표(?)로 은폐되어 있으며,
    /// 상단 액체를 다른 병으로 치워야 하단 색상이 시각적으로 공개(해금)됩니다.
    /// </summary>
    public class BlindTubeGimmick : IGimmick
    {
        public string GimmickName => "BlindTube";

        // 숨겨진 층 인덱스 집합 (0: 바닥층, 1, 2, ...)
        private readonly HashSet<int> _hiddenLayers = new HashSet<int>();

        public event Action<int> OnBlindLayerRevealed; // layerIndex

        public BlindTubeGimmick(int hiddenCount)
        {
            for (int i = 0; i < hiddenCount; i++)
            {
                _hiddenLayers.Add(i);
            }
        }

        public bool IsLayerHidden(int layerIndex)
        {
            return _hiddenLayers.Contains(layerIndex);
        }

        public bool CanPourFrom(Bottle bottle)
        {
            // 상단 레이어는 비어있지 않다면 항상 따를 수 있음
            return !bottle.IsEmpty;
        }

        public bool CanPourTo(Bottle bottle)
        {
            // 상단에 액체가 차 있고 최상단이 은폐되어 있다면 strict 비교가 어려우므로
            // 최상단은 항상 공개되어 있어야 따를 수 있음
            if (bottle.IsEmpty) return true;
            int topIdx = bottle.CurrentCount - 1;
            return !_hiddenLayers.Contains(topIdx);
        }

        public void OnMoveCompleted(Bottle bottle)
        {
            // 액체가 이동하여 새로 드러난 최상단 층이 있다면 해금
            int currentTop = bottle.CurrentCount - 1;
            if (currentTop >= 0 && _hiddenLayers.Contains(currentTop))
            {
                _hiddenLayers.Remove(currentTop);
                OnBlindLayerRevealed?.Invoke(currentTop);
            }
        }

        public void OnLayerRevealed(int layerIndex)
        {
            if (_hiddenLayers.Contains(layerIndex))
            {
                _hiddenLayers.Remove(layerIndex);
                OnBlindLayerRevealed?.Invoke(layerIndex);
            }
        }
    }
}
