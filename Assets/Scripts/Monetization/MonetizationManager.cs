using System;
using UnityEngine;
using WaterSort.Core;
using WaterSort.Solver;

namespace WaterSort.Monetization
{
    /// <summary>
    /// 하이브리드 수익화(IAA + IAP) 및 패배 즉시구제(Fail Offer) 총괄 매니저.
    /// $4.99 Fail Offer 결제 처리, 리워드 광고 보상, +5회 연장 및 비상 빈 병 1개 획득 핸들러,
    /// 180초(3분) 전면 광고 쿨다운 제어기를 포함합니다.
    /// </summary>
    public class MonetizationManager : MonoBehaviour
    {
        public static MonetizationManager Instance { get; private set; }

        public const float INTERSTITIAL_COOLDOWN_SECONDS = 180f; // 전면 광고 쿨다운 180초

        private IAdService _adService;
        private float _lastInterstitialTime = -999f;

        public event Action OnFailOfferRequested;    // 이동 수 소진 시 팝업 요청
        public event Action OnEmergencyRescued;      // 즉시 구제 성공 시 발생

        public bool CanShowInterstitial => (Time.time - _lastInterstitialTime) >= INTERSTITIAL_COOLDOWN_SECONDS;
        public float RemainingCooldown => Mathf.Max(0f, INTERSTITIAL_COOLDOWN_SECONDS - (Time.time - _lastInterstitialTime));

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                _adService = new MockAdService(); // 기본 목업 광고 서비스 주입
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
        }

        private void OnDestroy()
        {
            if (MoveLimitManager.Instance != null)
            {
                MoveLimitManager.Instance.OnStageFailed -= HandleStageFailed;
            }
        }

        public void SetAdService(IAdService adService)
        {
            _adService = adService;
        }

        private void HandleStageFailed()
        {
            // 이동 횟수가 소진되었을 때 $4.99 Fail Offer 팝업 모달 요청
            OnFailOfferRequested?.Invoke();
        }

        /// <summary>
        /// $4.99 Fail Offer 인앱 결제(IAP) 성공 시 실행
        /// 패키지 혜택: +5회 이동 연장 + 비상용 빈 병 1개 획득
        /// </summary>
        public void PurchaseFailOffer()
        {
            Debug.Log("[Monetization] $4.99 Fail Offer 구매 성공! +5회 이동 및 비상 병 1개를 지급합니다.");
            ApplyRescueReward();
        }

        /// <summary>
        /// 보상형 동영상 광고(Rewarded Ad) 시청 완료 시 실행 (+5회 연장)
        /// </summary>
        public void WatchAdForRescue()
        {
            if (_adService != null && _adService.IsRewardedReady)
            {
                _adService.ShowRewardedVideo(
                    onRewardEarned: () =>
                    {
                        Debug.Log("[Monetization] 리워드 광고 시청 완료! +5회 이동 연장을 지급합니다.");
                        ApplyRescueReward(includeEmergencyBottle: false);
                    },
                    onFailed: () =>
                    {
                        Debug.LogWarning("[Monetization] 광고 로드 실패.");
                    }
                );
            }
            else
            {
                // Fallback 즉시 보상
                ApplyRescueReward(includeEmergencyBottle: false);
            }
        }

        /// <summary>
        /// 즉시 구제 보상 적용: +5회 이동 연장 및 비상용 빈 병 스폰
        /// </summary>
        private void ApplyRescueReward(bool includeEmergencyBottle = true)
        {
            // 1. 이동 횟수 5회 연장
            if (MoveLimitManager.Instance != null)
            {
                MoveLimitManager.Instance.AddExtraMoves(5);
            }

            // 2. 비상용 빈 병 추가 스폰
            if (includeEmergencyBottle && WaterSortGameManager.Instance != null)
            {
                WaterSortGameManager.Instance.SpawnEmergencyBottle();
            }

            OnEmergencyRescued?.Invoke();
        }

        /// <summary>
        /// 스테이지 클리어 시 180초 쿨다운을 검사하여 전면 광고 재생
        /// </summary>
        public void TryShowInterstitialOnStageClear(Action onAdFinished)
        {
            if (CanShowInterstitial && _adService != null && _adService.IsInterstitialReady)
            {
                _lastInterstitialTime = Time.time;
                _adService.ShowInterstitial(() =>
                {
                    onAdFinished?.Invoke();
                });
            }
            else
            {
                onAdFinished?.Invoke();
            }
        }
    }
}
