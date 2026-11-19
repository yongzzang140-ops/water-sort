using System;
using System.Collections.Generic;
using UnityEngine;
using WaterSort.Core;

namespace WaterSort.Visual
{
    /// <summary>
    /// 2025-2026 모바일 캐주얼/퍼즐 트렌드를 선도하는 '네온-파스텔 글래스모피즘' 액체 컬러 메타데이터.
    /// 단순 단색(Flat RGB)을 탈피하여 빛의 투과와 깊이감을 주는 상/하단 그라데이션, 수면 거품 하이라이트 및 발광(Glow) 컬러 포함.
    /// </summary>
    [Serializable]
    public class PastelColorData
    {
        public int Id;
        public string Name;
        
        [Header("Gradient Definition")]
        [Tooltip("상단 빛을 받는 밝고 부드러운 파스텔 톤")]
        public Color TopColor;
        
        [Tooltip("하단 액체의 깊이감을 형성하는 채도 높은 네온-파스텔 톤")]
        public Color BottomColor;
        
        [Header("Optical Effects")]
        [Tooltip("수면 반사광 및 표면 림 라이트 컬러")]
        public Color SurfaceHighlightColor;
        
        [Tooltip("완성 연출 및 블룸(Bloom) 파티클용 네온 발광 컬러")]
        public Color GlowColor;

        public PastelColorData(int id, string name, Color top, Color bottom, Color highlight, Color glow)
        {
            Id = id;
            Name = name;
            TopColor = top;
            BottomColor = bottom;
            SurfaceHighlightColor = highlight;
            GlowColor = glow;
        }

        /// <summary>
        /// 액체 평균 대표 컬러 (기존 시스템 호환용)
        /// </summary>
        public Color RepresentativeColor => Color.Lerp(TopColor, BottomColor, 0.45f);
    }

    /// <summary>
    /// 트렌디한 12종 네온-파스텔 색상 팔레트 관리 및 런타임 셰이더/머티리얼 색상 공급 매니저.
    /// </summary>
    public class ColorPaletteManager : MonoBehaviour
    {
        public static ColorPaletteManager Instance { get; private set; }

        [Header("Palette Settings")]
        [SerializeField] private bool _useGradients = true;
        [SerializeField] private List<PastelColorData> _customPalette = new List<PastelColorData>();

        /// <summary>
        /// 2025-2026 트렌디 네온-파스텔 시그니처 12종 컬러 정의
        /// </summary>
        public static readonly PastelColorData[] NeonPastel12 = new PastelColorData[]
        {
            // 1. Neon Candy Pink (딸기 마카롱 핑크)
            new PastelColorData(
                1, "Neon Candy Pink",
                new Color(1.00f, 0.65f, 0.78f, 1f), // #FFA6C7
                new Color(0.98f, 0.38f, 0.60f, 1f), // #FA6199
                new Color(1.00f, 0.88f, 0.94f, 1f), // #FFE0F0
                new Color(1.00f, 0.30f, 0.65f, 1.2f)
            ),
            // 2. Pastel Mint Fizz (청량한 탄산 민트)
            new PastelColorData(
                2, "Mint Fizz",
                new Color(0.55f, 0.95f, 0.82f, 1f), // #8CF2D1
                new Color(0.24f, 0.82f, 0.65f, 1f), // #3DD1A6
                new Color(0.85f, 1.00f, 0.95f, 1f), // #D9FFF2
                new Color(0.20f, 0.98f, 0.75f, 1.2f)
            ),
            // 3. Electric Lilac (몽환적인 일렉트릭 라일락)
            new PastelColorData(
                3, "Electric Lilac",
                new Color(0.82f, 0.70f, 1.00f, 1f), // #D1B3FF
                new Color(0.66f, 0.45f, 0.96f, 1f), // #A873F5
                new Color(0.93f, 0.88f, 1.00f, 1f), // #EDE0FF
                new Color(0.75f, 0.45f, 1.00f, 1.2f)
            ),
            // 4. Cyber Peach (부드럽고 감각적인 복숭아)
            new PastelColorData(
                4, "Cyber Peach",
                new Color(1.00f, 0.76f, 0.60f, 1f), // #FFC299
                new Color(0.98f, 0.52f, 0.35f, 1f), // #FA8559
                new Color(1.00f, 0.90f, 0.82f, 1f), // #FFE6D1
                new Color(1.00f, 0.55f, 0.35f, 1.2f)
            ),
            // 5. Neon Sky Breeze (맑은 사이버 스카이 블루)
            new PastelColorData(
                5, "Neon Sky",
                new Color(0.52f, 0.85f, 1.00f, 1f), // #85D9FF
                new Color(0.25f, 0.68f, 0.98f, 1f), // #40AEFA
                new Color(0.85f, 0.95f, 1.00f, 1f), // #D9F2FF
                new Color(0.20f, 0.80f, 1.00f, 1.2f)
            ),
            // 6. Creamy Lemon Glaze (상큼한 레몬 글레이즈)
            new PastelColorData(
                6, "Lemon Glaze",
                new Color(1.00f, 0.95f, 0.55f, 1f), // #FFF28C
                new Color(0.98f, 0.84f, 0.28f, 1f), // #FAD647
                new Color(1.00f, 0.98f, 0.82f, 1f), // #FFFA space
                new Color(1.00f, 0.92f, 0.30f, 1.2f)
            ),
            // 7. Aurora Coral (오로라 코랄)
            new PastelColorData(
                7, "Aurora Coral",
                new Color(1.00f, 0.62f, 0.62f, 1f), // #FF9E9E
                new Color(0.96f, 0.36f, 0.42f, 1f), // #F55C6B
                new Color(1.00f, 0.86f, 0.86f, 1f), // #FFDBDB
                new Color(1.00f, 0.40f, 0.50f, 1.2f)
            ),
            // 8. Cyber Periwinkle (세련된 페리윙클 바이올렛)
            new PastelColorData(
                8, "Cyber Periwinkle",
                new Color(0.70f, 0.75f, 1.00f, 1f), // #B3BFFF
                new Color(0.48f, 0.55f, 0.96f, 1f), // #7A8CF5
                new Color(0.88f, 0.90f, 1.00f, 1f), // #E0E5FF
                new Color(0.55f, 0.62f, 1.00f, 1.2f)
            ),
            // 9. Hyper Emerald (네온 머스캣 에메랄드)
            new PastelColorData(
                9, "Hyper Emerald",
                new Color(0.58f, 0.92f, 0.65f, 1f), // #94EBA6
                new Color(0.28f, 0.82f, 0.45f, 1f), // #47D173
                new Color(0.88f, 0.98f, 0.90f, 1f), // #E0FAE6
                new Color(0.30f, 0.95f, 0.50f, 1.2f)
            ),
            // 10. Deep Ocean Glow (신비로운 오션 딥 글로우)
            new PastelColorData(
                10, "Deep Ocean Glow",
                new Color(0.45f, 0.68f, 0.96f, 1f), // #73ADF5
                new Color(0.25f, 0.45f, 0.85f, 1f), // #4073D9
                new Color(0.80f, 0.90f, 1.00f, 1f), // #CCE6FF
                new Color(0.30f, 0.60f, 1.00f, 1.2f)
            ),
            // 11. Apricot Sorbet (달콤한 살구 소르베)
            new PastelColorData(
                11, "Apricot Sorbet",
                new Color(1.00f, 0.84f, 0.65f, 1f), // #FFD6A6
                new Color(0.98f, 0.66f, 0.38f, 1f), // #FAA861
                new Color(1.00f, 0.94f, 0.86f, 1f), // #FFF0DC
                new Color(1.00f, 0.72f, 0.40f, 1.2f)
            ),
            // 12. Cosmic Lavender (코스믹 라벤더 펄)
            new PastelColorData(
                12, "Cosmic Lavender",
                new Color(0.92f, 0.72f, 0.95f, 1f), // #EBB8F2
                new Color(0.78f, 0.48f, 0.88f, 1f), // #C77AE0
                new Color(0.97f, 0.88f, 0.98f, 1f), // #F7E0FA
                new Color(0.88f, 0.55f, 0.96f, 1.2f)
            )
        };

        private static readonly Dictionary<int, PastelColorData> _paletteLookup = new Dictionary<int, PastelColorData>();

        static ColorPaletteManager()
        {
            RebuildLookup(NeonPastel12);
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                if (_customPalette != null && _customPalette.Count > 0)
                {
                    RebuildLookup(_customPalette);
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private static void RebuildLookup(IEnumerable<PastelColorData> dataList)
        {
            _paletteLookup.Clear();
            foreach (var item in dataList)
            {
                if (item != null && !_paletteLookup.ContainsKey(item.Id))
                {
                    _paletteLookup.Add(item.Id, item);
                }
            }
        }

        /// <summary>
        /// 액체 ID에 해당하는 네온-파스텔 컬러 데이터 반환
        /// </summary>
        public static PastelColorData GetColorData(int id)
        {
            if (id <= 0) return null;

            if (_paletteLookup.TryGetValue(id, out var data))
            {
                return data;
            }

            // Fallback: 12가지 범위를 벗어날 경우 동적 절차적 파스텔 생성
            float hue = ((id * 37) % 360) / 360f;
            Color top = Color.HSVToRGB(hue, 0.38f, 0.98f);
            Color bottom = Color.HSVToRGB(hue, 0.65f, 0.88f);
            Color highlight = Color.HSVToRGB(hue, 0.15f, 1.0f);
            Color glow = Color.HSVToRGB(hue, 0.80f, 1.0f);

            var generated = new PastelColorData(id, $"Custom Pastel {id}", top, bottom, highlight, glow);
            _paletteLookup[id] = generated;
            return generated;
        }

        /// <summary>
        /// Core의 LiquidColor 구조체로부터 PastelColorData 반환
        /// </summary>
        public static PastelColorData GetColorData(LiquidColor liquidColor)
        {
            return GetColorData(liquidColor.Id);
        }

        /// <summary>
        /// 레거시 LiquidColor 구조체 생성 도우미 (기존 코드와의 완벽한 호환성 제공)
        /// </summary>
        public static LiquidColor ToLiquidColor(int id)
        {
            var data = GetColorData(id);
            if (data == null) return LiquidColor.None;
            return new LiquidColor(data.Id, data.Name, data.RepresentativeColor);
        }
    }
}
