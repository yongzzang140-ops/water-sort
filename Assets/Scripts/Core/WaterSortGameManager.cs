using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WaterSort.Core;
using WaterSort.Level;
using WaterSort.View;

namespace WaterSort.Core
{
    /// <summary>
    /// 게임 코어 사이클, 보드 배치, 터치 입력 처리, 승리/패배 및 Undo를 총괄하는 메인 오케스트레이터
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
            if (DifficultyManager.Instance != null)
            {
                DifficultyManager.Instance.OnMovesExhausted += HandleMovesExhausted;
            }

            // 첫 게임 로드
            StartNewLevel(_currentLevelIndex);
        }

        private void OnDestroy()
        {
            if (DifficultyManager.Instance != null)
            {
                DifficultyManager.Instance.OnMovesExhausted -= HandleMovesExhausted;
            }
        }

        /// <summary>
        /// 새로운 레벨 생성 및 배치
        /// </summary>
        public void StartNewLevel(int levelIndex)
        {
            _currentLevelIndex = levelIndex;
            _selectedBottleView = null;
            _moveHistory.Clear();
            _isBusy = false;

            // 기존 병 오브젝트 정리
            ClearBoard();

            // 난이도에 맞는 레벨 데이터 생성
            DifficultyType diff = DifficultyManager.Instance != null ? DifficultyManager.Instance.CurrentDifficulty : DifficultyType.Casual;
            LevelData levelData = LevelGenerator.Instance != null
                ? LevelGenerator.Instance.GenerateLevel(diff, levelIndex)
                : CreateFallbackLevelData(diff, levelIndex);

            // 이동 제한 초기화 (M_opt 기준)
            if (DifficultyManager.Instance != null)
            {
                DifficultyManager.Instance.InitializeLevelLimit(levelData.OptimalMoves);
            }

            // 병 뷰 스폰 및 2D 자동 격자 배치
            SpawnAndLayoutBottles(levelData);

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

                Vector3 spawnPos = new Vector3(x, y, 0f);
                Transform parent = _boardContainer != null ? _boardContainer : transform;

                BottleView view = _bottlePrefab != null
                    ? Instantiate(_bottlePrefab, spawnPos, Quaternion.identity, parent)
                    : CreateFallbackBottleObject(spawnPos, parent);

                Bottle logicalBottle = new Bottle(i, levelData.BottleConfigurations[i]);
                view.Bind(logicalBottle);
                view.OnBottleClicked += HandleBottleClicked;

                _activeBottleViews.Add(view);
            }
        }

        /// <summary>
        /// 병 탭 입력 처리
        /// </summary>
        private void HandleBottleClicked(BottleView clickedView)
        {
            if (_isBusy) return;

            // 1. 선택된 병이 없으면 클릭한 병 선택
            if (_selectedBottleView == null)
            {
                if (!clickedView.LogicalBottle.IsEmpty)
                {
                    _selectedBottleView = clickedView;
                    _selectedBottleView.SetSelected(true);
                }
                return;
            }

            // 2. 이미 선택된 병을 다시 클릭하면 선택 해제
            if (_selectedBottleView == clickedView)
            {
                _selectedBottleView.SetSelected(false);
                _selectedBottleView = null;
                return;
            }

            // 3. 다른 병을 클릭한 경우 Strict Pouring 시도
            Bottle source = _selectedBottleView.LogicalBottle;
            Bottle target = clickedView.LogicalBottle;

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
                // 부어넣을 수 없지만 클릭한 병이 내용물이 있으면 선택 대상을 변경
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

            // 난이도 매니저에 이동 기록
            if (DifficultyManager.Instance != null)
            {
                DifficultyManager.Instance.RecordMove();
            }

            // Smooth Damping 애니메이션 재생
            bool animFinished = false;
            if (_pourAnimationHandler != null)
            {
                yield return _pourAnimationHandler.PlayPourAnimationRoutine(
                    sourceView,
                    targetView,
                    result.Color,
                    result.Amount,
                    () => animFinished = true
                );
            }
            else
            {
                sourceView.RefreshVisuals();
                targetView.RefreshVisuals();
                animFinished = true;
            }

            _isBusy = false;

            // 승리 판정
            CheckWinCondition();
        }

        /// <summary>
        /// 되돌리기(Undo) 기능
        /// </summary>
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

                    if (DifficultyManager.Instance != null)
                    {
                        DifficultyManager.Instance.RevertMove();
                    }
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 승리 판정: 모든 활성 병이 비어있거나 단일 색상 4층으로 완성되었는지 확인
        /// </summary>
        private void CheckWinCondition()
        {
            for (int i = 0; i < _activeBottleViews.Count; i++)
            {
                if (!_activeBottleViews[i].LogicalBottle.IsCompletedOrEmpty)
                {
                    return; // 미완성 병이 하나라도 있으면 아직 진행 중
                }
            }

            // 모든 병 정렬 완료!
            _isBusy = true;
            OnStageCompleted?.Invoke();
        }

        private void HandleMovesExhausted()
        {
            // 이동 횟수가 소진되었고 아직 승리하지 못한 경우
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
