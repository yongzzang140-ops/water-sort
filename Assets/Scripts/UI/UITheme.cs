using UnityEngine;

namespace WaterSort.UI
{
    /// <summary>
    /// 2025-2026 트렌디 모바일 게임 UI 디자인 시스템:
    /// '글래스모피즘(Glassmorphism) & 네온-파스텔 비주얼' 디자인 토큰 및 레이아웃 가이드.
    /// </summary>
    public static class UITheme
    {
        // -------------------------------------------------------------
        // 1. 코너 라운딩 및 레이아웃 토큰 (Geometry & Elevation)
        // -------------------------------------------------------------
        public const float CornerRadiusLarge = 28f;      // 플로팅 모달 카드 28pt 라운딩
        public const float CornerRadiusMedium = 20f;     // 하단 Thumb-Zone 버튼 20pt 라운딩
        public const float CornerRadiusSmall = 12f;      // 상단 뱃지 12pt 라운딩

        public const float ThumbZoneBottomOffset = 24f;  // 하단 SafeArea 여백
        public const float ThumbZoneHeight = 76f;        // 한 손 조작용 인체공학적 터치 높이

        // -------------------------------------------------------------
        // 2. 글래스모피즘 서피스 & 보더 (Frosted Glass Surfaces)
        // -------------------------------------------------------------
        // 캔버스 배경 (매우 부드러운 그라데이션 베이스)
        public static readonly Color CanvasBackgroundStart = new Color(0.96f, 0.97f, 1.00f, 1.00f); // #F5F8FF
        public static readonly Color CanvasBackgroundEnd   = new Color(0.91f, 0.93f, 0.98f, 1.00f); // #E8EDFA
        public static readonly Color BackgroundLight       = CanvasBackgroundStart; // 이전 버전 호환 필드
        public static readonly Color PanelSurface          = new Color(1.00f, 1.00f, 1.00f, 0.92f);
        public static readonly Color CardOutline           = new Color(0.90f, 0.92f, 0.96f, 1.00f);

        // 플로팅 모달 카드 (반투명 프로스티드 글래스)
        public static readonly Color ModalGlassSurface    = new Color(1.00f, 1.00f, 1.00f, 0.88f); // 88% White Glass
        public static readonly Color ModalGlassBorder     = new Color(1.00f, 1.00f, 1.00f, 0.95f); // 1px 하이라이트 림
        public static readonly Color ModalBackdropDim     = new Color(0.12f, 0.16f, 0.28f, 0.45f); // 부드러운 배경 딤

        // 하단 플로팅 독 (Floating Thumb-Dock)
        public static readonly Color DockGlassSurface     = new Color(1.00f, 1.00f, 1.00f, 0.82f);
        public static readonly Color DockGlassBorder      = new Color(0.92f, 0.94f, 1.00f, 0.70f);

        // -------------------------------------------------------------
        // 3. 텍스트 타이포그래피 (Modern Typography)
        // -------------------------------------------------------------
        public static readonly Color TextPrimary   = new Color(0.12f, 0.16f, 0.24f, 1.00f); // 깊고 선명한 잉크 네이비
        public static readonly Color TextSecondary = new Color(0.48f, 0.54f, 0.65f, 1.00f); // 소프트 쿨 그레이
        public static readonly Color TextWhite     = new Color(0.98f, 0.99f, 1.00f, 1.00f);
        public static readonly Color TextAccent    = new Color(0.35f, 0.45f, 0.85f, 1.00f);

        // 레거시 난이도 매핑
        public static readonly Color CasualMint    = new Color(0.32f, 0.88f, 0.70f, 1.00f);
        public static readonly Color ProBlue       = new Color(0.35f, 0.62f, 1.00f, 1.00f);
        public static readonly Color HellCrimson   = new Color(1.00f, 0.42f, 0.65f, 1.00f);

        // -------------------------------------------------------------
        // 4. 네온-파스텔 시그니처 액센트 (Interactive Accents)
        // -------------------------------------------------------------
        public static readonly Color AccentNeonPink   = new Color(1.00f, 0.42f, 0.65f, 1.00f); // #FF6BA6
        public static readonly Color AccentNeonMint   = new Color(0.32f, 0.88f, 0.70f, 1.00f); // #52E0B2
        public static readonly Color AccentNeonBlue   = new Color(0.35f, 0.62f, 1.00f, 1.00f); // #599EFF
        public static readonly Color AccentNeonPurple = new Color(0.72f, 0.52f, 1.00f, 1.00f); // #B885FF
        public static readonly Color AccentNeonYellow = new Color(1.00f, 0.85f, 0.35f, 1.00f); // #FFD959

        // -------------------------------------------------------------
        // 5. Thumb-Zone 아이템 버튼 컬러
        // -------------------------------------------------------------
        public static readonly Color ItemUndoBg       = new Color(0.92f, 0.94f, 1.00f, 0.92f);
        public static readonly Color ItemAddBottleBg  = new Color(0.90f, 0.98f, 0.95f, 0.95f);
        public static readonly Color ItemShuffleBg    = new Color(0.98f, 0.93f, 1.00f, 0.95f);
        public static readonly Color ItemSettingsBg   = new Color(0.95f, 0.95f, 0.98f, 0.90f);

        // 버튼 비활성화 상태
        public static readonly Color ItemDisabled     = new Color(0.75f, 0.78f, 0.84f, 0.40f);
    }
}
