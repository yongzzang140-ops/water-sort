using System;
using UnityEngine;

namespace WaterSort.Monetization
{
    /// <summary>
    /// AdMob / Unity Ads SDK 미임포트 환경 및 에디터 테스트를 위한 가상 광고 서비스
    /// </summary>
    public class MockAdService : IAdService
    {
        public bool IsInterstitialReady => true;
        public bool IsRewardedReady => true;

        public void ShowInterstitial(Action onClosed)
        {
            Debug.Log("[AdService] 전면 광고(Interstitial) 재생 시뮬레이션 완료.");
            onClosed?.Invoke();
        }

        public void ShowRewardedVideo(Action onRewardEarned, Action onFailed)
        {
            Debug.Log("[AdService] 리워드 보상형 광고(Rewarded Video) 시청 시뮬레이션 완료 -> 보상 지급!");
            onRewardEarned?.Invoke();
        }
    }
}
