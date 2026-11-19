using System.Collections;
using UnityEngine;
using WaterSort.Core;
using WaterSort.Visual;

namespace WaterSort.View
{
    /// <summary>
    /// 액체를 따를 때 단순 직선이 아닌 부드러운 유체 역학적 3차 베지어 곡선(Cubic Bezier Curve)으로
    /// 떨어지는 물줄기를 사실적이고 감각적으로 렌더링하는 컴포넌트.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class StreamRenderer : MonoBehaviour
    {
        [Header("Curve Resolution & Geometry")]
        [Range(10, 32)]
        [SerializeField] private int _curveResolution = 20;
        [SerializeField] private float _arcHeightMultiplier = 0.45f;
        [SerializeField] private float _forwardMomentumMultiplier = 0.65f;

        [Header("Taper & Flow Width")]
        [SerializeField] private float _streamMaxWidth = 0.16f;
        [SerializeField] private AnimationCurve _widthCurve;

        [Header("Splash FX (Optional)")]
        [SerializeField] private ParticleSystem _splashParticles;

        private LineRenderer _lineRenderer;
        private Vector3[] _curvePoints;
        private Coroutine _streamCoroutine;
        private bool _isStreaming = false;

        public bool IsStreaming => _isStreaming;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _curvePoints = new Vector3[_curveResolution];
            _lineRenderer.positionCount = _curveResolution;
            _lineRenderer.useWorldSpace = true;

            // 기본 폭 커브 설정 (주둥이 쪽은 살짝 굵고, 낙하하면서 부드러워지는 유체 형태)
            if (_widthCurve == null || _widthCurve.keys.Length == 0)
            {
                _widthCurve = new AnimationCurve(
                    new Keyframe(0.0f, 0.85f),
                    new Keyframe(0.2f, 1.00f),
                    new Keyframe(0.8f, 0.75f),
                    new Keyframe(1.0f, 0.50f)
                );
            }

            _lineRenderer.widthCurve = _widthCurve;
            _lineRenderer.widthMultiplier = _streamMaxWidth;
            _lineRenderer.enabled = false;
        }

        /// <summary>
        /// 베지어 물줄기 스트림 시작
        /// </summary>
        public void BeginStream(Transform sourcePourPoint, Transform targetMouthPoint, LiquidColor color, float pourDuration)
        {
            StopStream();
            _isStreaming = true;
            _streamCoroutine = StartCoroutine(StreamRoutine(sourcePourPoint, targetMouthPoint, color, pourDuration));
        }

        /// <summary>
        /// 베지어 물줄기 즉시 정지
        /// </summary>
        public void StopStream()
        {
            if (_streamCoroutine != null)
            {
                StopCoroutine(_streamCoroutine);
                _streamCoroutine = null;
            }

            if (_splashParticles != null && _splashParticles.isPlaying)
            {
                _splashParticles.Stop();
            }

            if (_lineRenderer != null)
            {
                _lineRenderer.enabled = false;
            }

            _isStreaming = false;
        }

        private IEnumerator StreamRoutine(Transform source, Transform target, LiquidColor color, float duration)
        {
            if (source == null || target == null)
            {
                _isStreaming = false;
                yield break;
            }

            // 네온 파스텔 컬러 데이터 가져오기
            PastelColorData palette = ColorPaletteManager.GetColorData(color.Id);
            Color topColor = palette != null ? palette.TopColor : color.ColorValue;
            Color bottomColor = palette != null ? palette.BottomColor : color.ColorValue;

            // LineRenderer 그라데이션 적용
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(topColor, 0.0f), new GradientColorKey(bottomColor, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.95f, 0.0f), new GradientAlphaKey(0.95f, 1.0f) }
            );
            _lineRenderer.colorGradient = gradient;
            _lineRenderer.enabled = true;

            if (_splashParticles != null)
            {
                var main = _splashParticles.main;
                main.startColor = bottomColor;
                _splashParticles.transform.position = target.position;
                _splashParticles.Play();
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);

                // 부어지는 시작/종료 시 물줄기 두께 팽창 및 수축 탄성 효과
                float widthScale = Mathf.Sin(progress * Mathf.PI);
                _lineRenderer.widthMultiplier = Mathf.Lerp(0.02f, _streamMaxWidth, Mathf.SmoothStep(0f, 1f, widthScale * 1.5f));

                if (source != null && target != null)
                {
                    UpdateBezierStreamPositions(source.position, target.position, source.right);
                    if (_splashParticles != null)
                    {
                        _splashParticles.transform.position = target.position;
                    }
                }

                yield return null;
            }

            // 물줄기 마무리 페이드아웃 (Tail Falloff)
            float fadeOut = 0.08f;
            float fadeElapsed = 0f;
            while (fadeElapsed < fadeOut)
            {
                fadeElapsed += Time.deltaTime;
                float t = 1f - (fadeElapsed / fadeOut);
                _lineRenderer.widthMultiplier = _streamMaxWidth * t;
                yield return null;
            }

            StopStream();
        }

        /// <summary>
        /// 3차 베지어 곡선(Cubic Bezier Curve) 제어점 계산 및 라인 렌더러 버텍스 갱신
        /// </summary>
        private void UpdateBezierStreamPositions(Vector3 p0, Vector3 p3, Vector3 sourceTangent)
        {
            // P0: 소스 병 주둥이
            // P3: 타깃 병 투입구
            // P1: 소스 주둥이의 방출 각도와 모멘텀을 고려한 포물선 시작점
            // P2: 타깃으로 수직 중력 낙하하도록 유도하는 제어점

            Vector3 direction = p3 - p0;
            float distance = direction.magnitude;

            // 주둥이가 향하는 방향으로 약간 밀어낸 후 중력 방향으로 곡선 유도
            Vector3 p1 = p0 + (sourceTangent * (_forwardMomentumMultiplier * distance * 0.4f)) + (Vector3.up * _arcHeightMultiplier);
            Vector3 p2 = p3 + (Vector3.up * (distance * 0.5f));

            for (int i = 0; i < _curveResolution; i++)
            {
                float t = i / (float)(_curveResolution - 1);
                _curvePoints[i] = CalculateCubicBezierPoint(p0, p1, p2, p3, t);
            }

            _lineRenderer.SetPositions(_curvePoints);
        }

        private Vector3 CalculateCubicBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            Vector3 point = uuu * p0; // (1-t)^3 * P0
            point += 3f * uu * t * p1; // 3(1-t)^2 * t * P1
            point += 3f * u * tt * p2; // 3(1-t) * t^2 * P2
            point += ttt * p3;         // t^3 * P3

            return point;
        }
    }
}
