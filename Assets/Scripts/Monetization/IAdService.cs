using System;

namespace WaterSort.Monetization
{
    /// <summary>
    /// AdMob 및 Unity Ads SDK 통합 연동을 위한 광고 서비스 인터페이스
    /// </summary>
    public interface IAdService
    {
        bool IsInterstitialReady { get; }
        bool IsRewardedReady { get; }

        void ShowInterstitial(Action onClosed);
        void ShowRewardedVideo(Action onRewardEarned, Action onFailed);
    }
}
