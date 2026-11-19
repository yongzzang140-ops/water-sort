using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WaterSort.Core;
using WaterSort.Gimmicks;
using WaterSort.Level;
using WaterSort.Solver;
using WaterSort.View;

namespace WaterSort.Core
{
    /// <summary>
    /// 게임 코어 사이클, 보드 배치, 터치 입력 처리, 4종 특수 기믹 및 수익화 즉시구제를 총괄하는 메인 오케스트레이터
    /// </summary>
    public class WaterSortGameManager : MonoBehaviour
    {
        public static WaterSortGameManager Instance { get; private set; }

        [Header("Prefabs & Handlers")]
        [SerializeField] private BottleView _bottlePrefab;
        [SerializeField] private Transform _boardContainer;
        [SerializeField] private SmoothDampPourAnimationHandler _pourAnimationHandler;

        [Header("Layout Settings")]
        [SerializeField] private float _horizontalSpacing = 1.6f;
        [SerializeField] private float _verticalSpacing = 3.2f;
        [SerializeField] private int _maxBottlesPerRow = 5;

        private readonly List<BottleView> _activeBottleViews = new List<BottleView>();
        private readonly Stack<MoveCommand> _moveHistory = new Stack<MoveCommand>();

        private BottleView _selectedBottleView = null;
        private bool _isBusy = false;
        private int _currentLevelIndex = 1;

        public event Action OnStageCompleted;
        public event Action OnStageFailed;
        public event Action<int> OnLevelLoaded; // levelIndex

        public int CurrentLevelIndex => _currentLevelIndex;
        public bool CanUndo => _moveHistory.Count > 0 && !_isBusy;
        public List<BottleView> ActiveBottleViews => _activeBottleViews;

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

        private void Start()
        {
            if (MoveLimitManager.Instance != null)
            {
                MoveLimitManager.Instance.OnStageFailed += HandleStageFailed;
            }
            if (DifficultyManager.Instance != null)
            {
                DifficultyManager.Instance.OnMovesExhausted += HandleStageFailed;
            }
            if (GimmickManager.Instance != null)
            {
                GimmickManager.Instance.OnCountdownFailed += HandleStageFailed;
            }

            // 첫 게임 로드
            StartNewLevel(_currentLevelIndex);
        }

        private void OnDestroy()
        {
            if (MoveLimitManager.Instance != null)
            {
                MoveLimitManager.Instance.OnStageFailed -= HandleStageFailed;
            }
            if (DifficultyManager.Instance != null)
            {
                DifficultyManager.Instance.OnMovesExhausted -= HandleStageFailed;
            }
            if (GimmickManager.Instance != null)
            {
                GimmickManager.Instance.OnCountdownFailed -= HandleStageFailed;
            }
        }

        /// <summary>
        /// 새로운 레벨 생성 및 배치 (기믹 및 이동 제한 설정 포함)
        /// </summary>
        public void StartNewLevel(int levelIndex)
        {
            _currentLevelIndex = levelIndex;
            _selectedBottleView = null;
            _moveHistory.Clear();
            _isBusy = false;

            // 기존 병 오브젝트 정리
            ClearBoard();

            // 난이도 결정
            DifficultyType diff = DifficultyManager.Instance != null ? DifficultyManager.Instance.CurrentDifficulty : DifficultyType.AdultCasual;
            LevelData levelData = LevelGenerator.Instance != null
                ? LevelGenerator.Instance.GenerateLevel(diff, levelIndex)
                : CreateFallbackLevelData(diff, levelIndex);

            // 병 뷰 스폰 및 2D 자동 격자 배치
            SpawnAndLayoutBottles(levelData);

            List<Bottle> logicalBottles = GetLogicalBottles();

            // 1. MoveLimitManager에 BFS 기반 최적수 및 이동 제한 초기화
            if (MoveLimitManager.Instance != null)
            {
                MoveLimitManager.Instance.InitializeLimit(diff, logicalBottles);
            }
            else if (DifficultyManager.Instance != null)
            {
                DifficultyManager.Instance.InitializeLevelLimit(levelData.OptimalMoves);
            }

            // 2. 어른 모드 전용 4종 특수 기믹 셋업
            if (GimmickManager.Instance != null)
            {
                GimmickManager.Instance.SetupGimmicksForStage(levelIndex, diff, logicalBottles);
            }

            OnLevelLoaded?.Invoke(_currentLevelIndex);
        }

        public void RestartCurrentLevel()
        {
            StartNewLevel(_currentLevelIndex);
        }

        public void NextLevel()
        {
            StartNewLevel(_currentLevelIndex + 1);
        }

        private void ClearBoard()
        {
            for (int i = 0; i < _activeBottleViews.Count; i++)
            {
                if (_activeBottleViews[i] != null)
                {
                    _activeBottleViews[i].OnBottleClicked -= HandleBottleClicked;
                    Destroy(_activeBottleViews[i].gameObject);
                }
            }
            _activeBottleViews.Clear();
        }

        /// <summary>
        /// 병들을 화면 중앙에 균등하게 1~2단 격자로 자동 배치
        /// </summary>
        private void SpawnAndLayoutBottles(LevelData levelData)
        {
            int total = levelData.TotalBottles;
            for (int i = 0; i < total; i++)
            {
                Vector3 spawnPos = Vector3.zero;
                Transform parent = _boardContainer != null ? _boardContainer : transform;

                BottleView view = _bottlePrefab != null
                    ? Instantiate(_bottlePrefab, spawnPos, Quaternion.identity, parent)
                    : CreateFallbackBottleObject(spawnPos, parent);

                Bottle logicalBottle = new Bottle(i, levelData.BottleConfigurations[i]);
                view.Bind(logicalBottle);
                view.OnBottleClicked += HandleBottleClicked;

                _activeBottleViews.Add(view);
            }

            RelayoutActiveBottles();
        }

        /// <summary>
        /// $4.99 Fail Offer 구매 또는 구제 시 비상용 빈 병 1개를 즉시 필드에 추가 스폰합니다.
        /// </summary>
        public void SpawnEmergencyBottle()
        {
            int newId = _activeBottleViews.Count;
            Vector3 spawnPos = Vector3.zero;
            Transform parent = _boardContainer != null ? _boardContainer : transform;

            BottleView view = _bottlePrefab != null
                ? Instantiate(_bottlePrefab, spawnPos, Quaternion.identity, parent)
                : CreateFallbackBottleObject(spawnPos, parent);

            Bottle emergencyBottle = new Bottle(newId, new List<LiquidColor>()); // 완전한 빈 병
            view.Bind(emergencyBottle);
            view.OnBottleClicked += HandleBottleClicked;

            _activeBottleViews.Add(view);
            RelayoutActiveBottles();

            // 부드러운 스폰 연출
            view.transform.localScale = Vector3.zero;
            StartCoroutine(AnimateEmergencySpawn(view.transform));
        }

        private IEnumerator AnimateEmergencySpawn(Transform target)
        {
            float elapsed = 0f;
            while (elapsed < 0.25f)
            {
                elapsed += Time.deltaTime;
                float s = Mathf.Lerp(0f, 1f, elapsed / 0.25f);
                if (target != null) target.localScale = Vector3.one * s;
                yield return null;
            }
            if (target != null) target.localScale = Vector3.one;
        }

        private void RelayoutActiveBottles()
        {
            int total = _activeBottleViews.Count;
            if (total == 0) return;

            int rows = total > _maxBottlesPerRow ? 2 : 1;
            int perRow = Mathf.CeilToInt((float)total / rows);

            for (int i = 0; i < total; i++)
            {
                int row = i / perRow;
                int col = i % perRow;

                int countInThisRow = (row == rows - 1) ? (total - row * perRow) : perRow;
                float rowStartX = -(countInThisRow - 1) * 0.5f * _horizontalSpacing;
                float x = rowStartX + (col * _horizontalSpacing);
                float y = (rows == 1) ? 0f : (row == 0 ? _verticalSpacing * 0.5f : -_verticalSpacing * 0.5f);

                _activeBottleViews[i].transform.position = new Vector3(x, y, 0f);
            }
        }

        private List<Bottle> GetLogicalBottles()
        {
            var list = new List<Bottle>(_activeBottleViews.Count);
            for (int i = 0; i < _activeBottleViews.Count; i++)
            {
                list.Add(_activeBottleViews[i].LogicalBottle);
            }
            return list;
        }

        /// <summary>
        /// 병 탭 입력 처리 및 4종 기믹 룰(자물쇠/얼음 등) 반영
        /// </summary>
        private void HandleBottleClicked(BottleView clickedView)
        {
            if (_isBusy) return;

            // 1. 자물쇠 튜브 검사
            if (GimmickManager.Instance != null)
            {
                var lockGimmick = GimmickManager.Instance.GetGimmick<LockedTubeGimmick>(clickedView.LogicalBottle.BottleId);
                if (lockGimmick != null && lockGimmick.IsLocked)
                {
                    Debug.Log("[Game] 이 병은 자물쇠로 잠겨있습니다. 열쇠 색상을 완성하세요!");
                    return;
                }
            }

            // 2. 선택된 병이 없으면 클릭한 병 선택
            if (_selectedBottleView == null)
            {
                if (!clickedView.LogicalBottle.IsEmpty)
                {
                    _selectedBottleView = clickedView;
                    _selectedBottleView.SetSelected(true);
                }
                return;
            }

            // 3. 같은 병 재클릭 -> 선택 취소
            if (_selectedBottleView == clickedView)
            {
                _selectedBottleView.SetSelected(false);
                _selectedBottleView = null;
                return;
            }

            // 4. 다른 병 클릭 -> Strict Pouring 또는 얼음 깨기 시도
            Bottle source = _selectedBottleView.LogicalBottle;
            Bottle target = clickedView.LogicalBottle;

            // 4-1. 얼음 기믹 검사 (Frozen Layer)
            if (GimmickManager.Instance != null)
            {
                var frozenGimmick = GimmickManager.Instance.GetGimmick<FrozenLayerGimmick>(source.BottleId);
                if (frozenGimmick != null && frozenGimmick.TryHitIce())
                {
                    // 얼음 깨짐으로 1턴 소진
                    _selectedBottleView.SetSelected(false);
                    _selectedBottleView = null;
                    if (MoveLimitManager.Instance != null) MoveLimitManager.Instance.ConsumeMove();
                    Debug.Log("[Gimmick] 얼음 액체가 1회 타격되어 금이 갔습니다! (1턴 소진)");
                    return;
                }
            }

            if (source.CanPourInto(target))
            {
                BottleView sourceView = _selectedBottleView;
                BottleView targetView = clickedView;

                _selectedBottleView.SetSelected(false);
                _selectedBottleView = null;

                StartCoroutine(ExecutePourRoutine(sourceView, targetView));
            }
            else
            {
                if (!target.IsEmpty)
                {
                    _selectedBottleView.SetSelected(false);
                    _selectedBottleView = clickedView;
                    _selectedBottleView.SetSelected(true);
                }
                else
                {
                    _selectedBottleView.SetSelected(false);
                    _selectedBottleView = null;
                }
            }
        }

        private IEnumerator ExecutePourRoutine(BottleView sourceView, BottleView targetView)
        {
            _isBusy = true;

            PourResult result = sourceView.LogicalBottle.PourInto(targetView.LogicalBottle);
            if (!result.Success)
            {
                _isBusy = false;
                yield break;
            }

            // 되돌리기(Undo)용 커맨드 기록
            _moveHistory.Push(new MoveCommand(result.SourceBottleId, result.TargetBottleId, result.Color, result.Amount));

            // 이동 횟수 차감
            if (MoveLimitManager.Instance != null)
            {
                MoveLimitManager.Instance.ConsumeMove();
            }
            else if (DifficultyManager.Instance != null)
            {
                DifficultyManager.Instance.RecordMove();
            }

            // 기믹 매니저에 이동 알림 (카운트다운 1씩 감소, 블라인드 층 해금 등)
            if (GimmickManager.Instance != null)
            {
                GimmickManager.Instance.NotifyMoveExecuted(GetLogicalBottles());
                var blindGimmick = GimmickManager.Instance.GetGimmick<BlindTubeGimmick>(sourceView.LogicalBottle.BottleId);
                blindGimmick?.OnMoveCompleted(sourceView.LogicalBottle);
            }

            // Smooth Damping 애니메이션 재생
            if (_pourAnimationHandler != null)
            {
                yield return _pourAnimationHandler.PlayPourAnimationRoutine(
                    sourceView,
                    targetView,
                    result.Color,
                    result.Amount,
                    null
                );
            }
            else
            {
                sourceView.RefreshVisuals();
                targetView.RefreshVisuals();
            }

            _isBusy = false;

            // 승리 판정
            CheckWinCondition();
        }

        public bool UndoLastMove()
        {
            if (!CanUndo) return false;

            MoveCommand command = _moveHistory.Pop();

            BottleView sourceView = _activeBottleViews.Find(v => v.LogicalBottle.BottleId == command.SourceBottleId);
            BottleView targetView = _activeBottleViews.Find(v => v.LogicalBottle.BottleId == command.TargetBottleId);

            if (sourceView != null && targetView != null)
            {
                bool success = command.Undo(sourceView.LogicalBottle, targetView.LogicalBottle);
                if (success)
                {
                    sourceView.RefreshVisuals();
                    targetView.RefreshVisuals();

                    if (MoveLimitManager.Instance != null)
                    {
                        MoveLimitManager.Instance.RevertMove();
                    }
                    else if (DifficultyManager.Instance != null)
                    {
                        DifficultyManager.Instance.RevertMove();
                    }
                    return true;
                }
            }

            return false;
        }

        private void CheckWinCondition()
        {
            for (int i = 0; i < _activeBottleViews.Count; i++)
            {
                if (!_activeBottleViews[i].LogicalBottle.IsCompletedOrEmpty)
                {
                    return;
                }
            }

            _isBusy = true;
            OnStageCompleted?.Invoke();
        }

        private void HandleStageFailed()
        {
            if (!_isBusy)
            {
                _isBusy = true;
                OnStageFailed?.Invoke();
            }
        }

        private LevelData CreateFallbackLevelData(DifficultyType diff, int levelIndex)
        {
            var fallback = new LevelData(levelIndex, diff, 5, 3, 2);
            fallback.BottleConfigurations.Add(new List<LiquidColor> { PastelPalette.Colors[0], PastelPalette.Colors[1], PastelPalette.Colors[0], PastelPalette.Colors[1] });
            fallback.BottleConfigurations.Add(new List<LiquidColor> { PastelPalette.Colors[1], PastelPalette.Colors[2], PastelPalette.Colors[2], PastelPalette.Colors[0] });
            fallback.BottleConfigurations.Add(new List<LiquidColor> { PastelPalette.Colors[2], PastelPalette.Colors[0], PastelPalette.Colors[1], PastelPalette.Colors[2] });
            fallback.BottleConfigurations.Add(new List<LiquidColor>());
            fallback.BottleConfigurations.Add(new List<LiquidColor>());
            fallback.OptimalMoves = 6;
            return fallback;
        }

        private BottleView CreateFallbackBottleObject(Vector3 pos, Transform parent)
        {
            GameObject go = new GameObject($"Bottle_{pos.x:F1}_{pos.y:F1}");
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.AddComponent<BoxCollider2D>().size = new Vector2(1.2f, 2.8f);
            return go.AddComponent<BottleView>();
        }
    }
}
