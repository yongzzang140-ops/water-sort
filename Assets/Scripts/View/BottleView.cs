using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WaterSort.Core;
using WaterSort.Visual;

namespace WaterSort.View
{
    /// <summary>
    /// 2025-2026 트렌디 글래스모피즘(Glassmorphic) 비주얼과 수면 찰랑임(Wobble),
    /// 탄성 넘치는 손맛(Elastic Ease Out) 모션을 제공하는 병 뷰 컴포넌트.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BottleView : MonoBehaviour
    {
        [Header("2D Glassmorphic Layer Structure")]
        [Tooltip("후면 반투명 틴트 및 굴절 음영 레이어")]
        [SerializeField] private SpriteRenderer _backGlassRenderer;

        [Tooltip("액체 4층 시각화 배열 (하단 0 ~ 상단 3)")]
        [SerializeField] private LiquidVisualLayer[] _layerVisuals = new LiquidVisualLayer[Bottle.CAPACITY];

        [Tooltip("수면이 출렁이는 표면 찰랑임(Wobble) 트랜스폼 및 림 하이라이트")]
        [SerializeField] private Transform _surfaceWobbleRoot;
        [SerializeField] private SpriteRenderer _surfaceFoamRenderer;

        [Tooltip("전면 2D 글래스 반사광, 곡면 하이라이트(Fresnel Rim Specular) 및 광택 스트라이프")]
        [SerializeField] private SpriteRenderer _frontHighlightRenderer;

        [Tooltip("모던 미니멀 라운드 아웃라인 테두리")]
        [SerializeField] private SpriteRenderer _bottleOutlineRenderer;

        [Tooltip("기울여 부을 때 회전 중심 피벗 및 물줄기 발사 주둥이")]
        [SerializeField] private Transform _pourPoint;

        [Tooltip("병 완성 축하 FX (반짝임, 파티클, 햅틱)")]
        [SerializeField] private BottleCompletionFX _completionFX;

        [Header("Juicy Elastic Motion (DOTween Style)")]
        [SerializeField] private float _selectedLiftAmount = 0.65f;
        [SerializeField] private float _elasticDuration = 0.38f;
        [SerializeField] private Vector3 _liftSquashScale = new Vector3(0.92f, 1.12f, 1.0f);

        [Header("Liquid Surface Wobble Physics Parameters")]
        [Tooltip("수면 출렁임 고유 진동 주파수")]
        [Range(1f, 8f)]
        [SerializeField] private float _wobbleFrequency = 4.2f;

        [Tooltip("수면 진동 감쇠율 (높을수록 빠르게 안정화)")]
        [Range(0.5f, 6f)]
        [SerializeField] private float _wobbleDecay = 2.8f;

        [Tooltip("최대 수면 기울기 한계 각도 (도)")]
        [Range(5f, 30f)]
        [SerializeField] private float _maxWobbleAngle = 18f;

        [Tooltip("병 이동 속도에 따른 출렁임 민감도")]
        [SerializeField] private float _movementSensitivity = 2.4f;

        private Bottle _logicalBottle;
        private Vector3 _originalPosition;
        private Quaternion _originalRotation;
        private Vector3 _originalScale = Vector3.one;

        private bool _isSelected = false;
        private Coroutine _motionCoroutine;

        // Wobble 물리 시뮬레이션 상태 변수
        private Vector3 _lastFramePosition;
        private float _lastFrameZRotation;
        private float _wobbleIntensity = 0f;
        private float _wobbleTimer = 0f;

        public event Action<BottleView> OnBottleClicked;

        public Bottle LogicalBottle => _logicalBottle;
        public Transform PourPoint => _pourPoint != null ? _pourPoint : transform;
        public Vector3 OriginalPosition => _originalPosition;
        public Quaternion OriginalRotation => _originalRotation;
        public bool IsSelected => _isSelected;
        public BottleCompletionFX CompletionFX => _completionFX;

        private void Awake()
        {
            _originalPosition = transform.position;
            _originalRotation = transform.rotation;
            _originalScale = transform.localScale;
            _lastFramePosition = transform.position;
            _lastFrameZRotation = transform.eulerAngles.z;

            if (_completionFX == null)
            {
                _completionFX = GetComponent<BottleCompletionFX>();
            }

            ApplyGlassmorphicStyling();
        }

        /// <summary>
        /// 2025-2026 글래스모피즘 기본 머티리얼 및 컬러 스타일링
        /// </summary>
        private void ApplyGlassmorphicStyling()
        {
            // 후면 글래스: 부드럽고 맑은 반투명 틴트
            if (_backGlassRenderer != null)
            {
                _backGlassRenderer.color = new Color(1.0f, 1.0f, 1.0f, 0.18f);
            }

            // 전면 글래스 하이라이트: 은은한 글래스 림 스페큘러 (빛의 반사광)
            if (_frontHighlightRenderer != null)
            {
                _frontHighlightRenderer.color = new Color(1.0f, 1.0f, 1.0f, 0.65f);
            }

            // 아웃라인: 매끄러운 화이트-소프트 블루 림
            if (_bottleOutlineRenderer != null)
            {
                _bottleOutlineRenderer.color = new Color(0.92f, 0.95f, 1.0f, 0.85f);
            }
        }

        public void Bind(Bottle bottle)
        {
            if (_logicalBottle != null)
            {
                _logicalBottle.OnStateChanged -= HandleBottleStateChanged;
            }

            _logicalBottle = bottle;
            _originalPosition = transform.position;
            _originalRotation = transform.rotation;
            _originalScale = transform.localScale;

            if (_logicalBottle != null)
            {
                _logicalBottle.OnStateChanged += HandleBottleStateChanged;
                RefreshVisuals();
            }
        }

        private void OnDestroy()
        {
            if (_logicalBottle != null)
            {
                _logicalBottle.OnStateChanged -= HandleBottleStateChanged;
            }
        }

        private void HandleBottleStateChanged(Bottle bottle)
        {
            RefreshVisuals();
        }

        /// <summary>
        /// 4개 층의 액체 색상 동기화 및 수면 위치/거품 컬러 정렬
        /// </summary>
        public void RefreshVisuals()
        {
            if (_logicalBottle == null || _layerVisuals == null) return;

            IReadOnlyList<LiquidColor> layers = _logicalBottle.Layers;

            for (int i = 0; i < Bottle.CAPACITY; i++)
            {
                if (i < _layerVisuals.Length && _layerVisuals[i] != null)
                {
                    if (i < layers.Count)
                    {
                        _layerVisuals[i].gameObject.SetActive(true);
                        _layerVisuals[i].SetColor(layers[i]);
                        _layerVisuals[i].SetInstantHeight(1f);
                    }
                    else
                    {
                        _layerVisuals[i].gameObject.SetActive(false);
                    }
                }
            }

            // 수면 Wobble 피벗을 최상단 채워진 층의 표면 위치로 조정
            UpdateSurfaceWobblePlacement(layers);
        }

        private void UpdateSurfaceWobblePlacement(IReadOnlyList<LiquidColor> layers)
        {
            if (_surfaceWobbleRoot == null) return;

            int count = layers != null ? layers.Count : 0;
            if (count == 0)
            {
                _surfaceWobbleRoot.gameObject.SetActive(false);
                return;
            }

            _surfaceWobbleRoot.gameObject.SetActive(true);

            // 최상단 층의 높이에 맞춘 Y 오프셋 산출 (각 층 높이를 0.55f 단위로 가정)
            float surfaceY = -0.85f + (count * 0.55f);
            _surfaceWobbleRoot.localPosition = new Vector3(0f, surfaceY, 0f);

            // 최상단 수면 림/거품 컬러를 해당 색상의 하이라이트 파스텔 톤과 일치화
            if (_surfaceFoamRenderer != null)
            {
                LiquidColor topColor = layers[count - 1];
                PastelColorData palette = ColorPaletteManager.GetColorData(topColor.Id);
                _surfaceFoamRenderer.color = palette != null
                    ? palette.SurfaceHighlightColor
                    : new Color(1f, 1f, 1f, 0.85f);
            }
        }

        public LiquidVisualLayer GetLayerVisual(int layerIndex)
        {
            if (layerIndex >= 0 && layerIndex < _layerVisuals.Length)
            {
                return _layerVisuals[layerIndex];
            }
            return null;
        }

        /// <summary>
        /// 병 선택 토글 시 탄성 넘치는 Elastic Ease Out(DOTween 모션) 연출 적용
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (_isSelected == selected) return;
            _isSelected = selected;

            if (_motionCoroutine != null)
            {
                StopCoroutine(_motionCoroutine);
            }

            Vector3 targetPos = _isSelected ? _originalPosition + (Vector3.up * _selectedLiftAmount) : _originalPosition;
            Vector3 peakScale = _isSelected ? _liftSquashScale : new Vector3(1.08f, 0.94f, 1.0f);

            _motionCoroutine = StartCoroutine(ElasticBounceMotionRoutine(targetPos, peakScale, _elasticDuration));

            // 선택 시 출렁이는 수면 Wobble 임펄스 가산
            AddWobbleImpulse(0.85f);
        }

        /// <summary>
        /// DOTween Elastic Ease Out을 수학적으로 완벽히 구현한 코루틴 탄성 애니메이션.
        /// (외부 플러그인 종속 없이 즉각 동작하며 필요 시 DOTween 대체 확장 호환)
        /// </summary>
        private IEnumerator ElasticBounceMotionRoutine(Vector3 targetPosition, Vector3 squashScale, float duration)
        {
            Vector3 startPos = transform.position;
            Vector3 startScale = transform.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // OutElastic 이징 수식
                float elasticT = EvaluateOutElastic(t);

                transform.position = Vector3.LerpUnclamped(startPos, targetPosition, elasticT);
                
                // 스쿼시 & 스트레치 탄성 진동
                float scaleT = Mathf.Sin(t * Mathf.PI) * Mathf.Exp(-3.5f * t);
                transform.localScale = Vector3.Lerp(Vector3.one, squashScale, scaleT);

                yield return null;
            }

            transform.position = targetPosition;
            transform.localScale = _originalScale;
            _motionCoroutine = null;
        }

        private float EvaluateOutElastic(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;

            float p = 0.3f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - p / 4f) * (2f * Mathf.PI) / p) + 1f;
        }

        public void ResetTransformImmediate()
        {
            if (_motionCoroutine != null)
            {
                StopCoroutine(_motionCoroutine);
                _motionCoroutine = null;
            }

            transform.position = _originalPosition;
            transform.rotation = _originalRotation;
            transform.localScale = _originalScale;
            _isSelected = false;
            _wobbleIntensity = 0f;
        }

        private void Update()
        {
            SimulateSurfaceWobble();
        }

        /// <summary>
        /// 병의 가속도 및 틸트 관성에 따른 2D 액체 수면 찰랑임(Wobble) 물리 감쇠 시뮬레이션
        /// </summary>
        private void SimulateSurfaceWobble()
        {
            // 1. 프레임 간 이동 및 회전 델타를 통한 관성 가속도 측정
            Vector3 currentPos = transform.position;
            float currentZRot = transform.eulerAngles.z;

            float moveDelta = (currentPos - _lastFramePosition).magnitude;
            float rotDelta = Mathf.DeltaAngle(_lastFrameZRotation, currentZRot);

            float totalImpulse = (moveDelta * _movementSensitivity) + (Mathf.Abs(rotDelta) * 0.08f);
            if (totalImpulse > 0.001f)
            {
                AddWobbleImpulse(totalImpulse);
            }

            _lastFramePosition = currentPos;
            _lastFrameZRotation = currentZRot;

            // 2. 수면 Wobble 진동자 감쇠 계산 (Damped Harmonic Oscillator)
            if (_wobbleIntensity > 0.001f && _surfaceWobbleRoot != null)
            {
                _wobbleTimer += Time.deltaTime * _wobbleFrequency * Mathf.PI * 2f;
                _wobbleIntensity = Mathf.Max(0f, _wobbleIntensity - (Time.deltaTime * _wobbleDecay));

                // 감쇠 사인파로 수면 기울기 각도 및 스케일 펄스 산출
                float wobbleAngle = Mathf.Sin(_wobbleTimer) * _wobbleIntensity * _maxWobbleAngle;
                _surfaceWobbleRoot.localRotation = Quaternion.Euler(0f, 0f, wobbleAngle);

                float wobbleScaleY = 1f + (Mathf.Cos(_wobbleTimer * 1.5f) * _wobbleIntensity * 0.25f);
                _surfaceWobbleRoot.localScale = new Vector3(1f, wobbleScaleY, 1f);
            }
            else if (_surfaceWobbleRoot != null)
            {
                _surfaceWobbleRoot.localRotation = Quaternion.identity;
                _surfaceWobbleRoot.localScale = Vector3.one;
            }
        }

        public void AddWobbleImpulse(float impulse)
        {
            _wobbleIntensity = Mathf.Clamp(_wobbleIntensity + impulse, 0f, 1.5f);
        }

        private void OnMouseDown()
        {
            OnBottleClicked?.Invoke(this);
        }

        /// <summary>
        /// 한 병이 완성되었을 때 화사한 스파클, 반짝임 및 햅틱 FX 재생
        /// </summary>
        public void PlayCompletionCelebration()
        {
            LiquidColor topColor = (_logicalBottle != null && _logicalBottle.TopColor.IsValid)
                ? _logicalBottle.TopColor
                : LiquidColor.None;

            if (_completionFX != null)
            {
                _completionFX.PlayCelebration(topColor);
            }
        }
    }
}
