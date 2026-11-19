using System;
using UnityEngine;

namespace WaterSort.Level
{
    public enum DifficultyType
    {
        Kid = 0,          // 어린이 모드: 큰 병, 2~3색, 2개 빈 병, 이동 횟수 무제한
        AdultCasual = 1,  // 어른 모드 - 일반: 5~7병, 3~4색, 2개 빈 병, M_opt + 15
        AdultPro = 2,     // 어른 모드 - 프로: 8~9병, 5~7색, 2개 빈 병, M_opt + 3
        AdultHell = 3     // 어른 모드 - 헬: 10~12병, 8~10색, 단 1개 빈 병, M_opt + 0
    }

    /// <summary>
    /// 난이도별 세부 설정 데이터 (어린이 모드 및 어른 모드 3단계 하위 난이도)
    /// </summary>
    [Serializable]
    public class DifficultyConfig
    {
        public DifficultyType Difficulty;
        public string DisplayName;
        public int MinBottles;
        public int MaxBottles;
        public int MinColors;
        public int MaxColors;
        public int EmptyBottles;
        public int DeltaMoves;
        public bool IsUnlimitedMoves;
        public Color ThemeColor;

        public DifficultyConfig(
            DifficultyType difficulty,
            string displayName,
            int minBottles,
            int maxBottles,
            int minColors,
            int maxColors,
            int emptyBottles,
            int deltaMoves,
            bool isUnlimitedMoves,
            Color themeColor)
        {
            Difficulty = difficulty;
            DisplayName = displayName;
            MinBottles = minBottles;
            MaxBottles = maxBottles;
            MinColors = minColors;
            MaxColors = maxColors;
            EmptyBottles = emptyBottles;
            DeltaMoves = deltaMoves;
            IsUnlimitedMoves = isUnlimitedMoves;
            ThemeColor = themeColor;
        }

        public static DifficultyConfig GetDefaultConfig(DifficultyType type)
        {
            switch (type)
            {
                case DifficultyType.Kid:
                    return new DifficultyConfig(
                        DifficultyType.Kid,
                        "Kid Mode",
                        minBottles: 4,
                        maxBottles: 5,
                        minColors: 2,
                        maxColors: 3,
                        emptyBottles: 2,
                        deltaMoves: 999,
                        isUnlimitedMoves: true, // 어린이 모드는 이동 제한 완전 없음
                        themeColor: new Color(0.98f, 0.75f, 0.40f) // Soft Warm Amber
                    );

                case DifficultyType.AdultCasual:
                    return new DifficultyConfig(
                        DifficultyType.AdultCasual,
                        "Adult - Casual",
                        minBottles: 5,
                        maxBottles: 7,
                        minColors: 3,
                        maxColors: 4,
                        emptyBottles: 2,
                        deltaMoves: 15,
                        isUnlimitedMoves: false,
                        themeColor: new Color(0.48f, 0.85f, 0.70f) // Soft Mint
                    );

                case DifficultyType.AdultPro:
                    return new DifficultyConfig(
                        DifficultyType.AdultPro,
                        "Adult - Pro",
                        minBottles: 8,
                        maxBottles: 10,
                        minColors: 5,
                        maxColors: 7,
                        emptyBottles: 2,
                        deltaMoves: 3,
                        isUnlimitedMoves: false,
                        themeColor: new Color(0.45f, 0.65f, 0.95f) // Electric Pastel Blue
                    );

                case DifficultyType.AdultHell:
                    return new DifficultyConfig(
                        DifficultyType.AdultHell,
                        "Adult - Hell",
                        minBottles: 10,
                        maxBottles: 12,
                        minColors: 8,
                        maxColors: 10,
                        emptyBottles: 1, // Hell 모드는 오직 단 1개의 빈 병만 제공!
                        deltaMoves: 0,   // 단 1회의 낭비도 불허 (M_limit = M_opt + 0)
                        isUnlimitedMoves: false,
                        themeColor: new Color(0.95f, 0.45f, 0.55f) // Crimson Pastel
                    );

                default:
                    return GetDefaultConfig(DifficultyType.AdultCasual);
            }
        }
    }
}
