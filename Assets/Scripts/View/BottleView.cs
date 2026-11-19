using System;
using System.Collections.Generic;
using UnityEngine;
using WaterSort.Core;

namespace WaterSort.View
{
    /// <summary>
    /// 개별 병(Flask)의 2D 렌더링, 4층 액체 레이어 시각화 및 선택/바운스 애니메이션 제어
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BottleView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer _bottleOutlineRenderer;
        [SerializeField] private Transform _pourPoint; // 주둥이 위치 (기울여 쏟을 때의 피벗)
        [SerializeField] private ParticleSystem _completionParticle;
        [SerializeField] private LiquidVisualLayer[] _layerVisuals = new LiquidVisualLayer[Bottle.CAPACITY];

        [Header("Animation Tuning")]
        [SerializeField] private float _selectedLiftAmount = 0.55f;
        [SerializeField] private float _liftSmoothTime = 0.10f;

        private Bottle _logicalBottle;
        private Vector3 _originalPosition;
        private Quaternion _originalRotation;
        private Vector3 _targetPosition;
        private Vector3 _positionVelocity;
        private bool _isSelected = false;

        public event Action<BottleView> OnBottleClicked;

        public Bottle LogicalBottle => _logicalBottle;
        public Transform PourPoint => _pourPoint != null ? _pourPoint : transform;
        public Vector3 OriginalPosition => _originalPosition;
        public Quaternion OriginalRotation => _originalRotation;
        public bool IsSelected => _isSelected;

        private void Awake()
        {
            _originalPosition = transform.position;
            _originalRotation = transform.rotation;
            _targetPosition = _originalPosition;
        }

        /// <summary>
        /// 논리 Bottle 모델과 바인딩
        /// </summary>
        public void Bind(Bottle bottle)
        {
            if (_logicalBottle != null)
            {
                _logicalBottle.OnStateChanged -= HandleBottleStateChanged;
            }

            _logicalBottle = bottle;
            _originalPosition = transform.position;
            _targetPosition = _originalPosition;
            _originalRotation = transform.rotation;

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
        /// 4개 층의 액체 색상 및 높이 비주얼 동기화
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
        }

        /// <summary>
        /// 특정 층 비주얼 반환
        /// </summary>
        public LiquidVisualLayer GetLayerVisual(int layerIndex)
        {
            if (layerIndex >= 0 && layerIndex < _layerVisuals.Length)
            {
                return _layerVisuals[layerIndex];
            }
            return null;
        }

        /// <summary>
        /// 병 선택/해제 토글 시 위로 바운스
        /// </summary>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            _targetPosition = _isSelected
                ? _originalPosition + Vector3.up * _selectedLiftAmount
                : _originalPosition;
        }

        public void ResetTransformImmediate()
        {
            _targetPosition = _originalPosition;
            transform.position = _originalPosition;
            transform.rotation = _originalRotation;
            _isSelected = false;
        }

        private void Update()
        {
            // 선택 리프트 부드러운 감쇠 (Smooth Damping)
            if (Vector3.Distance(transform.position, _targetPosition) > 0.001f && !_logicalBottle.IsLocked)
            {
                transform.position = Vector3.SmoothDamp(transform.position, _targetPosition, ref _positionVelocity, _liftSmoothTime);
            }
        }

        private void OnMouseDown()
        {
            OnBottleClicked?.Invoke(this);
        }

        public void PlayCompletionCelebration()
        {
            if (_completionParticle != null)
            {
                _completionParticle.Play();
            }
        }
    }
}
