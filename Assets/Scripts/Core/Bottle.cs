using System;
using System.Collections.Generic;

namespace WaterSort.Core
{
    /// <summary>
    /// 단일 병(Flask)의 논리적 상태 및 액체 스택 제어를 담당하는 코어 클래스.
    /// 최대 4층(Capacity = 4)의 액체 보관 및 Strict Pour 검증 로직 포함.
    /// </summary>
    [Serializable]
    public class Bottle
    {
        public const int CAPACITY = 4;

        public int BottleId { get; private set; }

        // 아래층(index 0)부터 위층(index Count - 1) 순으로 저장되는 액체 스택
        private readonly List<LiquidColor> _layers = new List<LiquidColor>(CAPACITY);

        // 연출 진행 중 입력 방지용 상태 플래그
        public bool IsLocked { get; set; }

        public event Action<Bottle> OnStateChanged;

        public Bottle(int id)
        {
            BottleId = id;
            IsLocked = false;
        }

        public Bottle(int id, IEnumerable<LiquidColor> initialLayers) : this(id)
        {
            if (initialLayers != null)
            {
                foreach (var color in initialLayers)
                {
                    if (_layers.Count < CAPACITY && color.IsValid)
                    {
                        _layers.Add(color);
                    }
                }
            }
        }

        #region Properties

        public int CurrentCount => _layers.Count;

        public int AvailableSpace => CAPACITY - _layers.Count;

        public bool IsEmpty => _layers.Count == 0;

        public bool IsFull => _layers.Count >= CAPACITY;

        /// <summary>
        /// 맨 위 액체 색상 (없을 경우 LiquidColor.None)
        /// </summary>
        public LiquidColor TopColor => IsEmpty ? LiquidColor.None : _layers[_layers.Count - 1];

        /// <summary>
        /// 맨 위에 연속으로 쌓여있는 동일 색상의 층 개수
        /// </summary>
        public int TopColorConsecutiveCount
        {
            get
            {
                if (IsEmpty) return 0;
                LiquidColor top = TopColor;
                int count = 0;
                for (int i = _layers.Count - 1; i >= 0; i--)
                {
                    if (_layers[i] == top) count++;
                    else break;
                }
                return count;
            }
        }

        /// <summary>
        /// 병이 완벽하게 정렬(정리)되었는지 여부:
        /// 1) 완전히 비어있거나,
        /// 2) 4칸이 모두 차 있고 전부 동일한 단일 색상인 경우.
        /// </summary>
        public bool IsCompletedOrEmpty
        {
            get
            {
                if (IsEmpty) return true;
                if (!IsFull) return false;
                LiquidColor first = _layers[0];
                for (int i = 1; i < CAPACITY; i++)
                {
                    if (_layers[i] != first) return false;
                }
                return true;
            }
        }

        /// <summary>
        /// 비어있지 않으면서 완성된 상태인지 확인
        /// </summary>
        public bool IsCompletedSingleColor => IsFull && TopColorConsecutiveCount == CAPACITY;

        public IReadOnlyList<LiquidColor> Layers => _layers;

        #endregion

        #region Strict Pour Logic

        /// <summary>
        /// Strict 부어넣기(Pour) 유효성 검증 함수:
        /// 1. 대상 병이 자기 자신이 아니어야 함.
        /// 2. 자신이 비어있지 않아야 함.
        /// 3. 대상 병이 가득 차 있지 않아야 함.
        /// 4. 대상 병이 빈 병이거나, 대상 병의 최상단 색상이 내 최상단 색상과 일치해야 함.
        /// 5. 두 병 모두 잠금(애니메이션 중) 상태가 아니어야 함.
        /// </summary>
        public bool CanPourInto(Bottle target)
        {
            if (target == null || target == this) return false;
            if (IsLocked || target.IsLocked) return false;
            if (IsEmpty) return false;
            if (target.IsFull) return false;

            // 이미 한 병에 단일 색상으로 완성되어 있는 경우 굳이 빈 병으로 붓는 것은 무의미한 낭비 (옵션: 허용하되 최적수 방지)
            // Strict 퍼즐 룰: 대상 병이 비어있거나 색상이 같아야 이동 가능
            return target.IsEmpty || target.TopColor == this.TopColor;
        }

        /// <summary>
        /// 이번 이동에서 실제로 옮겨질 액체 방울(층)의 개수 계산
        /// </summary>
        public int CalculatePourAmount(Bottle target)
        {
            if (!CanPourInto(target)) return 0;
            int consecutive = TopColorConsecutiveCount;
            int space = target.AvailableSpace;
            return Math.Min(consecutive, space);
        }

        /// <summary>
        /// 타깃 병으로 액체를 이동시킴.
        /// 이동된 액체 수와 색상, 되돌리기(Undo)에 필요한 정보를 반환.
        /// </summary>
        public PourResult PourInto(Bottle target)
        {
            if (!CanPourInto(target))
            {
                return PourResult.Failed;
            }

            int amount = CalculatePourAmount(target);
            if (amount <= 0)
            {
                return PourResult.Failed;
            }

            LiquidColor colorToMove = TopColor;

            // 실제 레이어 데이터 이동
            for (int i = 0; i < amount; i++)
            {
                _layers.RemoveAt(_layers.Count - 1);
                target._layers.Add(colorToMove);
            }

            OnStateChanged?.Invoke(this);
            target.OnStateChanged?.Invoke(target);

            return new PourResult(true, BottleId, target.BottleId, colorToMove, amount);
        }

        /// <summary>
        /// Undo를 위해 액체 스택을 강제로 되돌릴 때 사용
        /// </summary>
        public void PushLayerDirect(LiquidColor color)
        {
            if (_layers.Count < CAPACITY && color.IsValid)
            {
                _layers.Add(color);
                OnStateChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// Undo를 위해 액체 스택 최상단을 강제로 제거할 때 사용
        /// </summary>
        public LiquidColor PopLayerDirect()
        {
            if (_layers.Count > 0)
            {
                var removed = _layers[_layers.Count - 1];
                _layers.RemoveAt(_layers.Count - 1);
                OnStateChanged?.Invoke(this);
                return removed;
            }
            return LiquidColor.None;
        }

        /// <summary>
        /// 특정 층의 색상 조회 (0: 바닥 ~ 3: 최상단)
        /// </summary>
        public LiquidColor GetLayerAt(int index)
        {
            if (index >= 0 && index < _layers.Count)
            {
                return _layers[index];
            }
            return LiquidColor.None;
        }

        /// <summary>
        /// 데이터 깊은 복사 (BFS Solver 및 시뮬레이션용)
        /// </summary>
        public Bottle Clone()
        {
            var cloned = new Bottle(BottleId);
            for (int i = 0; i < _layers.Count; i++)
            {
                cloned._layers.Add(_layers[i]);
            }
            return cloned;
        }

        #endregion
    }

    /// <summary>
    /// 부어넣기 실행 결과 구조체
    /// </summary>
    public readonly struct PourResult
    {
        public readonly bool Success;
        public readonly int SourceBottleId;
        public readonly int TargetBottleId;
        public readonly LiquidColor Color;
        public readonly int Amount;

        public PourResult(bool success, int sourceId, int targetId, LiquidColor color, int amount)
        {
            Success = success;
            SourceBottleId = sourceId;
            TargetBottleId = targetId;
            Color = color;
            Amount = amount;
        }

        public static PourResult Failed => new PourResult(false, -1, -1, LiquidColor.None, 0);
    }
}
