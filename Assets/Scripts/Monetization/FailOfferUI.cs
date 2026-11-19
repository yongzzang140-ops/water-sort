using UnityEngine;
using UnityEngine.UI;
using WaterSort.Core;

namespace WaterSort.Monetization
{
    /// <summary>
    /// $4.99 패배 즉시구제(Fail Offer) 팝업 모달 UI 컨트롤러.
    /// 이동 횟수 소진 시 노출되어 IAP 결제 또는 보상형 광고 시청을 유도합니다.
    /// </summary>
    public class FailOfferUI : MonoBehaviour
    {
        [Header("Modal Panels")]
        [SerializeField] private GameObject _failOfferPanel;

        [Header("UI Buttons")]
        [SerializeField] private Button _purchaseButton;      // $4.99 즉시구제 패키지 버튼
        [SerializeField] private Button _watchAdButton;        // 광고 보고 +5회 버튼
        [SerializeField] private Button _giveUpButton;         // 포기하고 다시 시작 버튼

        [Header("Texts")]
        [SerializeField] private Text _offerPriceText;
        [SerializeField] private Text _offerDescriptionText;

        private void Start()
        {
            if (_purchaseButton != null) _purchaseButton.onClick.AddListener(OnClickPurchase);
            if (_watchAdButton != null) _watchAdButton.onClick.AddListener(OnClickWatchAd);
            if (_giveUpButton != null) _giveUpButton.onClick.AddListener(OnClickGiveUp);

            if (MonetizationManager.Instance != null)
            {
                MonetizationManager.Instance.OnFailOfferRequested += ShowModal;
                MonetizationManager.Instance.OnEmergencyRescued += HideModal;
            }

            HideModal();
        }

        private void OnDestroy()
        {
            if (MonetizationManager.Instance != null)
            {
                MonetizationManager.Instance.OnFailOfferRequested -= ShowModal;
                MonetizationManager.Instance.OnEmergencyRescued -= HideModal;
            }
        }

        public void ShowModal()
        {
            if (_failOfferPanel != null)
            {
                _failOfferPanel.SetActive(true);
            }
        }

        public void HideModal()
        {
            if (_failOfferPanel != null)
            {
                _failOfferPanel.SetActive(false);
            }
        }

        private void OnClickPurchase()
        {
            if (MonetizationManager.Instance != null)
            {
                MonetizationManager.Instance.PurchaseFailOffer();
            }
        }

        private void OnClickWatchAd()
        {
            if (MonetizationManager.Instance != null)
            {
                MonetizationManager.Instance.WatchAdForRescue();
            }
        }

        private void OnClickGiveUp()
        {
            HideModal();
            if (WaterSortGameManager.Instance != null)
            {
                WaterSortGameManager.Instance.RestartCurrentLevel();
            }
        }
    }
}
