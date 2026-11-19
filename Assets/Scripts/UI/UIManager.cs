using UnityEngine;
using UnityEngine.UI;
using WaterSort.Core;
using WaterSort.Level;

namespace WaterSort.UI
{
    /// <summary>
    /// 모던 파스텔톤 2D UI 뷰 연동 매니저.
    /// 상단 스테이지/난이도/이동 수 표시, 하단 Undo/Restart 바, 3단계 모드 선택 패널, 승리 및 실패 팝업 제어.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Top Bar")]
        [SerializeField] private Text _levelText;
        [SerializeField] private Text _difficultyBadgeText;
        [SerializeField] private Image _difficultyBadgeBg;
        [SerializeField] private Text _movesCounterText;

        [Header("Bottom Control Bar")]
        [SerializeField] private Button _undoButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _modeSelectButton;

        [Header("Mode Selection Modal")]
        [SerializeField] private GameObject _modeSelectPanel;
        [SerializeField] private Button _closeModePanelButton;
        [SerializeField] private Button _casualModeButton;
        [SerializeField] private Button _proModeButton;
        [SerializeField] private Button _hellModeButton;

        [Header("Game Clear Modal")]
        [SerializeField] private GameObject _gameClearModal;
        [SerializeField] private Text _clearMovesInfoText;
        [SerializeField] private Button _nextLevelButton;

        [Header("Game Over / Moves Exhausted Modal")]
        [SerializeField] private GameObject _gameOverModal;
        [SerializeField] private Text _gameOverMessageText;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _giveUpButton;

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
            // 버튼 리스너 바인딩
            if (_undoButton != null) _undoButton.onClick.AddListener(OnClickUndo);
            if (_restartButton != null) _restartButton.onClick.AddListener(OnClickRestart);
            if (_modeSelectButton != null) _modeSelectButton.onClick.AddListener(OpenModeSelectModal);

            if (_closeModePanelButton != null) _closeModePanelButton.onClick.AddListener(CloseModeSelectModal);
            if (_casualModeButton != null) _casualModeButton.onClick.AddListener(() => OnSelectDifficulty(DifficultyType.Casual));
            if (_proModeButton != null) _proModeButton.onClick.AddListener(() => OnSelectDifficulty(DifficultyType.Pro));
            if (_hellModeButton != null) _hellModeButton.onClick.AddListener(() => OnSelectDifficulty(DifficultyType.Hell));

            if (_nextLevelButton != null) _nextLevelButton.onClick.AddListener(OnClickNextLevel);
            if (_retryButton != null) _retryButton.onClick.AddListener(OnClickRestart);
            if (_giveUpButton != null) _giveUpButton.onClick.AddListener(OpenModeSelectModal);

            // 이벤트 구독
            if (WaterSortGameManager.Instance != null)
            {
                WaterSortGameManager.Instance.OnLevelLoaded += HandleLevelLoaded;
                WaterSortGameManager.Instance.OnStageCompleted += HandleStageCompleted;
                WaterSortGameManager.Instance.OnStageFailed += HandleStageFailed;
            }

            if (DifficultyManager.Instance != null)
            {
                DifficultyManager.Instance.OnDifficultyChanged += HandleDifficultyChanged;
                DifficultyManager.Instance.OnMovesUpdated += HandleMovesUpdated;
                UpdateDifficultyBadge(DifficultyManager.Instance.CurrentDifficulty);
            }

            // 초기 팝업 닫기
            CloseAllModals();
        }

        private void OnDestroy()
        {
            if (WaterSortGameManager.Instance != null)
            {
                WaterSortGameManager.Instance.OnLevelLoaded -= HandleLevelLoaded;
                WaterSortGameManager.Instance.OnStageCompleted -= HandleStageCompleted;
                WaterSortGameManager.Instance.OnStageFailed -= HandleStageFailed;
            }

            if (DifficultyManager.Instance != null)
            {
                DifficultyManager.Instance.OnDifficultyChanged -= HandleDifficultyChanged;
                DifficultyManager.Instance.OnMovesUpdated -= HandleMovesUpdated;
            }
        }

        #region Event Handlers

        private void HandleLevelLoaded(int levelIndex)
        {
            CloseAllModals();
            if (_levelText != null)
            {
                _levelText.text = $"Level {levelIndex}";
            }
            UpdateUndoButtonState();
        }

        private void HandleDifficultyChanged(DifficultyType newDifficulty)
        {
            UpdateDifficultyBadge(newDifficulty);
        }

        private void HandleMovesUpdated(int usedMoves, int limitMoves)
        {
            if (_movesCounterText != null)
            {
                if (limitMoves <= 0)
                {
                    _movesCounterText.text = $"Moves: {usedMoves}";
                }
                else
                {
                    int remaining = Mathf.Max(0, limitMoves - usedMoves);
                    _movesCounterText.text = $"Moves: {usedMoves} / {limitMoves}  ({remaining} left)";
                }
            }
            UpdateUndoButtonState();
        }

        private void HandleStageCompleted()
        {
            if (_gameClearModal != null)
            {
                _gameClearModal.SetActive(true);

                if (_clearMovesInfoText != null && DifficultyManager.Instance != null)
                {
                    int used = DifficultyManager.Instance.UsedMoves;
                    int opt = DifficultyManager.Instance.OptimalMoves;
                    _clearMovesInfoText.text = $"Completed in {used} moves! (Optimal: {opt})";
                }
            }
        }

        private void HandleStageFailed()
        {
            if (_gameOverModal != null)
            {
                _gameOverModal.SetActive(true);

                if (_gameOverMessageText != null && DifficultyManager.Instance != null)
                {
                    _gameOverMessageText.text = $"{DifficultyManager.Instance.CurrentDifficulty} Mode limit reached!";
                }
            }
        }

        #endregion

        #region User Actions

        private void OnClickUndo()
        {
            if (WaterSortGameManager.Instance != null)
            {
                WaterSortGameManager.Instance.UndoLastMove();
                UpdateUndoButtonState();
            }
        }

        private void OnClickRestart()
        {
            CloseAllModals();
            if (WaterSortGameManager.Instance != null)
            {
                WaterSortGameManager.Instance.RestartCurrentLevel();
            }
        }

        private void OnClickNextLevel()
        {
            CloseAllModals();
            if (WaterSortGameManager.Instance != null)
            {
                WaterSortGameManager.Instance.NextLevel();
            }
        }

        public void OpenModeSelectModal()
        {
            CloseAllModals();
            if (_modeSelectPanel != null)
            {
                _modeSelectPanel.SetActive(true);
            }
        }

        public void CloseModeSelectModal()
        {
            if (_modeSelectPanel != null)
            {
                _modeSelectPanel.SetActive(false);
            }
        }

        private void OnSelectDifficulty(DifficultyType type)
        {
            if (DifficultyManager.Instance != null)
            {
                DifficultyManager.Instance.SetDifficulty(type);
            }
            CloseModeSelectModal();

            // 선택된 난이도로 레벨 1 새로 시작
            if (WaterSortGameManager.Instance != null)
            {
                WaterSortGameManager.Instance.StartNewLevel(1);
            }
        }

        private void CloseAllModals()
        {
            if (_modeSelectPanel != null) _modeSelectPanel.SetActive(false);
            if (_gameClearModal != null) _gameClearModal.SetActive(false);
            if (_gameOverModal != null) _gameOverModal.SetActive(false);
        }

        private void UpdateUndoButtonState()
        {
            if (_undoButton != null && WaterSortGameManager.Instance != null)
            {
                _undoButton.interactable = WaterSortGameManager.Instance.CanUndo;
            }
        }

        private void UpdateDifficultyBadge(DifficultyType diff)
        {
            if (_difficultyBadgeText != null)
            {
                _difficultyBadgeText.text = diff.ToString().ToUpper();
            }

            if (_difficultyBadgeBg != null)
            {
                _difficultyBadgeBg.color = diff switch
                {
                    DifficultyType.Casual => UITheme.CasualMint,
                    DifficultyType.Pro => UITheme.ProBlue,
                    DifficultyType.Hell => UITheme.HellCrimson,
                    _ => UITheme.CasualMint
                };
            }
        }

        #endregion
    }
}
