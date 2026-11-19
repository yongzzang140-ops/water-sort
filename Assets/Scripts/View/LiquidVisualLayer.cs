using UnityEngine;
using WaterSort.Core;

namespace WaterSort.View
{
    /// <summary>
    /// 병 내부의 단일 액체 층(Layer) 2D 비주얼 렌더러.
    /// Smooth Damping을 통해 차오르거나 줄어드는 수면 높이 연출 지원.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class LiquidVisualLayer : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;

        private float _targetHeight = 1f;
        private float _currentHeight = 1f;
        private float _heightVelocity = 0f;
        private const float SMOOTH_TIME = 0.12f;
        private bool _isAnimatingHeight = false;

        public LiquidColor CurrentColor { get; private set; }

        private void Awake()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        public void SetColor(LiquidColor color)
        {
            CurrentColor = color;
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = color.ColorValue;
                _spriteRenderer.enabled = color.IsValid;
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
                _currentHeight = Mathf.SmoothDamp(_currentHeight, _targetHeight, ref _heightVelocity, SMOOTH_TIME);
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
