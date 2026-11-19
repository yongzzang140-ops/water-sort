using UnityEngine;

namespace WaterSort.UI
{
    /// <summary>
    /// 모던 2D 파스텔톤 테마 색상 팔레트 및 UI 스타일 가이드
    /// </summary>
    public static class UITheme
    {
        // 배경 및 패널 컬러
        public static readonly Color BackgroundLight = new Color(0.97f, 0.98f, 1.00f, 1.00f); // #F7FAFF
        public static readonly Color PanelSurface    = new Color(1.00f, 1.00f, 1.00f, 0.94f); // Pure White Soft
        public static readonly Color CardOutline     = new Color(0.90f, 0.92f, 0.96f, 1.00f);

        // 텍스트 컬러
        public static readonly Color TextPrimary     = new Color(0.18f, 0.22f, 0.28f, 1.00f); // Deep Slate
        public static readonly Color TextSecondary   = new Color(0.48f, 0.54f, 0.62f, 1.00f); // Cool Grey
        public static readonly Color TextAccent      = new Color(0.35f, 0.45f, 0.85f, 1.00f); // Indigo Accent

        // 난이도별 시그니처 파스텔
        public static readonly Color CasualMint      = new Color(0.48f, 0.85f, 0.70f, 1.00f); // #7AD9B2
        public static readonly Color ProBlue         = new Color(0.45f, 0.68f, 0.96f, 1.00f); // #73ADF5
        public static readonly Color HellCrimson     = new Color(0.96f, 0.46f, 0.56f, 1.00f); // #F5758F

        // 버튼 상태 컬러
        public static readonly Color ButtonNormal    = new Color(0.92f, 0.94f, 0.98f, 1.00f);
        public static readonly Color ButtonHover     = new Color(0.85f, 0.89f, 0.96f, 1.00f);
        public static readonly Color ButtonDisabled  = new Color(0.88f, 0.88f, 0.88f, 0.50f);
    }
}
