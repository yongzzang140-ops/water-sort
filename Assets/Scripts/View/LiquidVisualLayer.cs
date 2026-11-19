using UnityEngine;
using WaterSort.Core;
using WaterSort.Visual;

namespace WaterSort.View
{
    /// <summary>
    /// 병 내부의 단일 액체 층(Layer) 2D 비주얼 렌더러.
    /// 트렌디한 파스텔 그라데이션, 수면 림 라이트 및 부드러운 차오름/줄어듦 Smooth Damping 지원.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class LiquidVisualLayer : MonoBehaviour
    {
        [Header("Renderers")]
        [SerializeField] private SpriteRenderer _bodyRenderer;
        [SerializeField] private SpriteRenderer _surfaceHighlightRenderer;

        [Header("Animation Tuning")]
        [SerializeField] private float _smoothTime = 0.12f;

        private float _targetHeight = 1f;
        private float _currentHeight = 1f;
        private float _heightVelocity = 0f;
        private bool _isAnimatingHeight = false;

        public LiquidColor CurrentColor { get; private set; }
        public PastelColorData CurrentPaletteData { get; private set; }

        private void Awake()
        {
            if (_bodyRenderer == null)
            {
                _bodyRenderer = GetComponent<SpriteRenderer>();
            }
        }

        /// <summary>
        /// 액체 컬러 지정 (2025-2026 트렌디 네온-파스텔 비주얼 매핑)
        /// </summary>
        public void SetColor(LiquidColor color)
        {
            CurrentColor = color;
            if (!color.IsValid)
            {
                if (_bodyRenderer != null) _bodyRenderer.enabled = false;
                if (_surfaceHighlightRenderer != null) _surfaceHighlightRenderer.enabled = false;
                return;
            }

            CurrentPaletteData = ColorPaletteManager.GetColorData(color.Id);

            if (_bodyRenderer != null)
            {
                _bodyRenderer.enabled = true;
                // 네온 파스텔 대표 색상 또는 바텀 깊이 색상 적용
                _bodyRenderer.color = CurrentPaletteData != null
                    ? CurrentPaletteData.RepresentativeColor
                    : color.ColorValue;
            }

            if (_surfaceHighlightRenderer != null)
            {
                _surfaceHighlightRenderer.enabled = true;
                _surfaceHighlightRenderer.color = CurrentPaletteData != null
                    ? CurrentPaletteData.SurfaceHighlightColor
                    : Color.white;
            }
        }

        public void SetInstantHeight(float heightNormalized)
        {
            _currentHeight = heightNormalized;
            _targetHeight = heightNormalized;
            _isAnimatingHeight = false;
            ApplyHeightScale(_currentHeight);
        }

        public void AnimateHeightTo(float targetHeightNormalized)
        {
            _targetHeight = targetHeightNormalized;
            _isAnimatingHeight = true;
        }

        private void Update()
        {
            if (_isAnimatingHeight)
            {
                _currentHeight = Mathf.SmoothDamp(_currentHeight, _targetHeight, ref _heightVelocity, _smoothTime);
                ApplyHeightScale(_currentHeight);

                if (Mathf.Abs(_currentHeight - _targetHeight) < 0.005f)
                {
                    _currentHeight = _targetHeight;
                    ApplyHeightScale(_currentHeight);
                    _isAnimatingHeight = false;
                }
            }
        }

        private void ApplyHeightScale(float height)
        {
            Vector3 scale = transform.localScale;
            scale.y = Mathf.Max(0f, height);
            transform.localScale = scale;
        }
    }
}
