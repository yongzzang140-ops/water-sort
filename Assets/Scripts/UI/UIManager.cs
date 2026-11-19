using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using WaterSort.Core;
using WaterSort.Level;

namespace WaterSort.UI
{
    /// <summary>
    /// 2025-2026 모바일 퍼즐 트렌드를 반영한 트렌디 UI/UX 매니저.
    /// - Corner Radius 28pt 플로팅 글래스 모달 카드 시스템
    /// - 한 손 엄지 최적화 Thumb-Zone 하단 컨트롤 독 (Undo, 병 추가, 셔플, 설정/재시작)
    /// - DOTween 기반 Elastic Ease Out 팝업/버튼 탄성 모션
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Top Modern Glass Bar")]
        [SerializeField] private Text _levelText;
        [SerializeField] private Text _difficultyBadgeText;
        [SerializeField] private Image _difficultyBadgeBg;
        [SerializeField] private Text _movesCounterText;

        [Header("Bottom Ergonomic Thumb-Zone Dock")]
        [Tooltip("되돌리기 (1수 무르기)")]
        [SerializeField] private Button _undoButton;
        [Tooltip("빈 병 추가 (긴급 탈출 튜브)")]
        [SerializeField] private Button _addBottleButton;
        [Tooltip("셔플 / 힌트 아이템")]
        [SerializeField] private Button _shuffleButton;
        [Tooltip("환경설정 / 모드 변경")]
        [SerializeField] private Button _settingsButton;
        [Tooltip("현재 스테이지 재시작")]
        [SerializeField] private Button _restartButton;

        [Header("Floating Glass Modal: Mode Selection")]
        [SerializeField] private GameObject _modeSelectModal;
        [SerializeField] private Transform _modeModalCardTransform;
        [SerializeField] private Button _closeModeModalButton;
        [SerializeField] private Button _casualModeButton;
        [SerializeField] private Button _proModeButton;
        [SerializeField] private Button _hellModeButton;

        [Header("Floating Glass Modal: Stage Clear")]
        [SerializeField] private GameObject _gameClearModal;
        [SerializeField] private Transform _clearModalCardTransform;
        [SerializeField] private Text _clearMovesInfoText;
        [SerializeField] private Button _nextLevelButton;

        [Header("Floating Glass Modal: Game Over")]
        [SerializeField] private GameObject _gameOverModal;
        [SerializeField] private Transform _gameOverModalCardTransform;
        [SerializeField] private Text _gameOverMessageText;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _emergencyAddBottlePromptButton;

        [Header("Elastic Motion Tuning")]
        [SerializeField] private float _modalElasticDuration = 0.42f;
        [SerializeField] private float _buttonBounceScale = 0.90f;

        private Coroutine _activeModalRoutine;

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
            // Thumb-Zone 하단 조작 버튼 리스너 바인딩 및 탄성 피드백 연결
            BindJuicyButton(_undoButton, OnClickUndo);
            BindJuicyButton(_addBottleButton, OnClickAddBottle);
            BindJuicyButton(_shuffleButton, OnClickShuffle);
            BindJuicyButton(_settingsButton, OpenModeSelectModal);
            BindJuicyButton(_restartButton, OnClickRestart);

            // 모달 내부 액션 버튼 바인딩
            BindJuicyButton(_closeModeModalButton, CloseModeSelectModal);
            BindJuicyButton(_casualModeButton, () => OnSelectDifficulty(DifficultyType.Casual));
            BindJuicyButton(_proModeButton, () => OnSelectDifficulty(DifficultyType.Pro));
            BindJuicyButton(_hellModeButton, () => OnSelectDifficulty(DifficultyType.Hell));

            BindJuicyButton(_nextLevelButton, OnClickNextLevel);
            BindJuicyButton(_retryButton, OnClickRestart);
            BindJuicyButton(_emergencyAddBottlePromptButton, OnClickAddBottle);

            // 게임 코어 이벤트 구독
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

            CloseAllModalsInstant();
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

        /// <summary>
        /// 버튼에 손맛을 살리는 Elastic Squash & Stretch 터치 애니메이션 바인딩
        /// </summary>
        private void BindJuicyButton(Button btn, UnityEngine.Events.UnityAction action)
        {
            if (btn == null) return;
            btn.onClick.AddListener(() =>
            {
                StartCoroutine(PlayButtonElasticRoutine(btn.transform));
                action?.Invoke();
            });
        }

        private IEnumerator PlayButtonElasticRoutine(Transform target)
        {
            if (target == null) yield break;

            Vector3 baseScale = Vector3.one;
            float elapsed = 0f;
            float duration = 0.24f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // 눌렸다가 튕겨 나오는 탄성 감쇠 진동 (Elastic Out)
                float scale = 1f - (Mathf.Sin(t * Mathf.PI) * (1f - _buttonBounceScale) * Mathf.Exp(-4f * t));
                target.localScale = baseScale * scale;
                yield return null;
            }

            target.localScale = baseScale;
        }

        #region Event Handlers

        private void HandleLevelLoaded(int levelIndex)
        {
            CloseAllModalsInstant();
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
                    _movesCounterText.text = $"Moves: {usedMoves} / {limitMoves} ({remaining} 남음)";
                }
            }
            UpdateUndoButtonState();
        }

        private void HandleStageCompleted()
        {
            if (_gameClearModal != null)
            {
                if (_clearMovesInfoText != null && DifficultyManager.Instance != null)
                {
                    int used = DifficultyManager.Instance.UsedMoves;
                    int opt = DifficultyManager.Instance.OptimalMoves;
                    _clearMovesInfoText.text = $"🎉 멋져요! {used}회 만에 클리어! (최적수: {opt})";
                }
                OpenFloatingModal(_gameClearModal, _clearModalCardTransform);
            }
        }

        private void HandleStageFailed()
        {
            if (_gameOverModal != null)
            {
                if (_gameOverMessageText != null && DifficultyManager.Instance != null)
                {
                    _gameOverMessageText.text = $"{DifficultyManager.Instance.CurrentDifficulty} 모드 이동 횟수를 모두 소진했습니다!";
                }
                OpenFloatingModal(_gameOverModal, _gameOverModalCardTransform);
            }
        }

        #endregion

        #region Thumb-Zone User Actions

        private void OnClickUndo()
        {
            if (WaterSortGameManager.Instance != null)
            {
                bool success = WaterSortGameManager.Instance.UndoLastMove();
                if (success)
                {
                    UpdateUndoButtonState();
                }
            }
        }

        private void OnClickAddBottle()
        {
            // 막힌 상황에서 긴급 튜브 추가 (보상형 구제 수단)
            if (WaterSortGameManager.Instance != null)
            {
                WaterSortGameManager.Instance.SpawnEmergencyBottle();
                CloseAllModalsInstant();
                UpdateUndoButtonState();
            }
        }

        private void OnClickShuffle()
        {
            Debug.Log("[Thumb-Zone] 셔플 / 힌트 아이템이 실행되었습니다.");
            // 셔플 이펙트 연출 또는 힌트 표시
        }

        private void OnClickRestart()
        {
            CloseAllModalsInstant();
            if (WaterSortGameManager.Instance != null)
            {
                WaterSortGameManager.Instance.RestartCurrentLevel();
            }
        }

        private void OnClickNextLevel()
        {
            CloseAllModalsInstant();
            if (WaterSortGameManager.Instance != null)
            {
                WaterSortGameManager.Instance.NextLevel();
            }
        }

        public void OpenModeSelectModal()
        {
            OpenFloatingModal(_modeSelectModal, _modeModalCardTransform);
        }

        public void CloseModeSelectModal()
        {
            CloseFloatingModal(_modeSelectModal, _modeModalCardTransform);
        }

        private void OnSelectDifficulty(DifficultyType type)
        {
            if (DifficultyManager.Instance != null)
            {
                DifficultyManager.Instance.SetDifficulty(type);
            }
            CloseModeSelectModal();

            if (WaterSortGameManager.Instance != null)
            {
                WaterSortGameManager.Instance.StartNewLevel(1);
            }
        }

        #endregion

        #region Floating Modal Elastic Animation System

        /// <summary>
        /// 플로팅 모달 카드를 DOTween Elastic Ease Out으로 통통 튀며 열기
        /// </summary>
        public void OpenFloatingModal(GameObject modalRoot, Transform cardTransform)
        {
            if (modalRoot == null) return;
            modalRoot.SetActive(true);

            if (cardTransform != null)
            {
                if (_activeModalRoutine != null) StopCoroutine(_activeModalRoutine);
                _activeModalRoutine = StartCoroutine(ModalElasticOpenRoutine(cardTransform));
            }
        }

        public void CloseFloatingModal(GameObject modalRoot, Transform cardTransform)
        {
            if (modalRoot == null) return;

            if (cardTransform != null)
            {
                if (_activeModalRoutine != null) StopCoroutine(_activeModalRoutine);
                _activeModalRoutine = StartCoroutine(ModalSmoothCloseRoutine(modalRoot, cardTransform));
            }
            else
            {
                modalRoot.SetActive(false);
            }
        }

        private IEnumerator ModalElasticOpenRoutine(Transform card)
        {
            card.localScale = Vector3.zero;
            float elapsed = 0f;

            while (elapsed < _modalElasticDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _modalElasticDuration);

                // OutElastic 모달 등장 곡선
                float p = 0.35f;
                float elasticT = (t == 0f || t == 1f) ? t : Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - p / 4f) * (2f * Mathf.PI) / p) + 1f;

                card.localScale = Vector3.one * elasticT;
                yield return null;
            }

            card.localScale = Vector3.one;
            _activeModalRoutine = null;
        }

        private IEnumerator ModalSmoothCloseRoutine(GameObject modalRoot, Transform card)
        {
            float elapsed = 0f;
            float duration = 0.16f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                card.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.82f, t);
                yield return null;
            }

            modalRoot.SetActive(false);
            card.localScale = Vector3.one;
            _activeModalRoutine = null;
        }

        private void CloseAllModalsInstant()
        {
            if (_modeSelectModal != null) _modeSelectModal.SetActive(false);
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
                    DifficultyType.Casual => UITheme.AccentNeonMint,
                    DifficultyType.Pro => UITheme.AccentNeonBlue,
                    DifficultyType.Hell => UITheme.AccentNeonPink,
                    _ => UITheme.AccentNeonMint
                };
            }
        }

        #endregion
    }
}
