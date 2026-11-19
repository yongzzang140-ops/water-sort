using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using WaterSort.Core;
using WaterSort.Visual;

namespace WaterSort.View
{
    /// <summary>
    /// 한 병의 정렬이 완성되었을 때(동일한 4칸의 색상이 모두 채워짐)
    /// 유리병 반짝임(Shine Flash), 스파클 파티클 방출, 모바일 햅틱 진동(Vibration)을 총괄하는 FX 컴포넌트.
    /// </summary>
    public class BottleCompletionFX : MonoBehaviour
    {
        [Header("FX Target References")]
        [SerializeField] private BottleView _bottleView;
        [SerializeField] private SpriteRenderer _glassShineRenderer;
        [SerializeField] private ParticleSystem _sparkleParticleSystem;

        [Header("Visual Flash & Bounce Settings")]
        [SerializeField] private Color _flashColor = new Color(1f, 1f, 1f, 0.95f);
        [SerializeField] private float _flashDuration = 0.28f;
        [SerializeField] private float _bounceScaleY = 1.15f;
        [SerializeField] private float _bounceScaleX = 0.88f;

        [Header("Haptic Feedback Settings")]
        [SerializeField] private bool _enableHaptics = true;
        [SerializeField] private float _hapticDelay = 0.05f;

        [Header("Events")]
        public UnityEvent OnCompletionCelebrationStarted;
        public UnityEvent OnCompletionCelebrationFinished;

        private Coroutine _celebrationRoutine;

        private void Reset()
        {
            _bottleView = GetComponent<BottleView>();
        }

        /// <summary>
        /// 병 완성 기념 축하 연출 재생
        /// </summary>
        public void PlayCelebration(LiquidColor completedColor, Action onFinished = null)
        {
            if (_celebrationRoutine != null)
            {
                StopCoroutine(_celebrationRoutine);
            }

            _celebrationRoutine = StartCoroutine(CelebrationRoutine(completedColor, onFinished));
        }

        private IEnumerator CelebrationRoutine(LiquidColor completedColor, Action onFinished)
        {
            OnCompletionCelebrationStarted?.Invoke();

            // 1. 모바일 햅틱 피드백 즉시 트리거 (비동기 2단계 기분 좋은 리듬)
            if (_enableHaptics)
            {
                TriggerMobileHapticFeedback();
            }

            // 2. 파스텔 글로우 색상으로 스파클 파티클 터짐
            if (_sparkleParticleSystem != null)
            {
                PastelColorData palette = ColorPaletteManager.GetColorData(completedColor.Id);
                Color glowColor = palette != null ? palette.GlowColor : completedColor.ColorValue;

                var main = _sparkleParticleSystem.main;
                main.startColor = new ParticleSystem.MinMaxGradient(Color.white, glowColor);
                _sparkleParticleSystem.transform.position = transform.position + Vector3.up * 0.5f;
                _sparkleParticleSystem.Play();
            }

            // 3. 통통 튀는 스쿼시 & 스트레치 (Squash & Stretch Bounce) + 글래스 림 섬광
            Vector3 originalScale = transform.localScale;
            Color originalShineColor = _glassShineRenderer != null ? _glassShineRenderer.color : Color.clear;

            float elapsed = 0f;
            float totalDuration = 0.45f;

            while (elapsed < totalDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / totalDuration);

                // 탄성 감쇠 진동 수식 (Elastic Damping)
                // 감쇠 사인파로 튀어오름 재현: Sin(t * PI * 3) * (1 - t)
                float dampFactor = Mathf.Exp(-5f * t);
                float wave = Mathf.Sin(t * Mathf.PI * 4f) * dampFactor;

                float scaleY = originalScale.y * (1f + wave * (_bounceScaleY - 1f));
                float scaleX = originalScale.x * (1f - wave * (1f - _bounceScaleX));
                transform.localScale = new Vector3(scaleX, scaleY, originalScale.z);

                // 유리병 반짝임(Shine Highlight Flash) 페이드
                if (_glassShineRenderer != null)
                {
                    float flashAlpha = Mathf.Max(0f, 1f - (elapsed / _flashDuration));
                    _glassShineRenderer.color = Color.Lerp(originalShineColor, _flashColor, flashAlpha);
                }

                yield return null;
            }

            // 원상 복귀
            transform.localScale = originalScale;
            if (_glassShineRenderer != null)
            {
                _glassShineRenderer.color = originalShineColor;
            }

            OnCompletionCelebrationFinished?.Invoke();
            onFinished?.Invoke();
            _celebrationRoutine = null;
        }

        /// <summary>
        /// 모바일 기기(iOS/Android) 햅틱 진동 피드백
        /// </summary>
        private void TriggerMobileHapticFeedback()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    if (vibrator != null && vibrator.Call<bool>("hasVibrator"))
                    {
                        // 2단계 성공 진동 패턴: 대기 0ms -> 35ms 탭 -> 대기 45ms -> 50ms 탄력 탭
                        long[] pattern = new long[] { 0, 35, 45, 50 };
                        vibrator.Call("vibrate", pattern, -1);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BottleCompletionFX] Android 햅틱 진동 호출 실패: {ex.Message}");
            }
#endif

#if UNITY_IOS && !UNITY_EDITOR
            // iOS 모바일 햅틱 피드백 연동
            Handheld.Vibrate();
            return;
#endif

            // 에디터 및 표준 폴백
#if UNITY_STANDALONE || UNITY_EDITOR
            // 에디터에서는 디버그 로그 및 시각적 안내
            // Debug.Log("<color=#73ADF5>[Haptic]</color> 병 완성 성공 햅틱 진동 발생!");
#else
            Handheld.Vibrate();
#endif
        }
    }
}
