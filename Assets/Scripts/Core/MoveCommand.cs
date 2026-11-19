using System;

namespace WaterSort.Core
{
    /// <summary>
    /// 플레이어의 단일 이동(Pour) 기록을 보관하여 되돌리기(Undo)를 지원하는 커맨드 구조체
    /// </summary>
    [Serializable]
    public struct MoveCommand
    {
        public int SourceBottleId;
        public int TargetBottleId;
        public LiquidColor Color;
        public int Amount;

        public MoveCommand(int sourceId, int targetId, LiquidColor color, int amount)
        {
            SourceBottleId = sourceId;
            TargetBottleId = targetId;
            Color = color;
            Amount = amount;
        }

        /// <summary>
        /// 해당 이동을 역으로 되돌림 (Target -> Source)
        /// </summary>
        public bool Undo(Bottle sourceBottle, Bottle targetBottle)
        {
            if (sourceBottle == null || targetBottle == null) return false;
            if (sourceBottle.BottleId != SourceBottleId || targetBottle.BottleId != TargetBottleId) return false;

            // Target에서 Amount만큼 꺼내서 Source로 다시 넣음
            for (int i = 0; i < Amount; i++)
            {
                LiquidColor popped = targetBottle.PopLayerDirect();
                if (popped.IsValid)
                {
                    sourceBottle.PushLayerDirect(popped);
                }
            }
            return true;
        }
    }
}
