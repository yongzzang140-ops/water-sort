using System;
using System.Collections.Generic;
using WaterSort.Core;

namespace WaterSort.Level
{
    /// <summary>
    /// 단일 스테이지 구성 데이터 (각 병의 초기 액체 층 정보 및 목표 최적수 보관)
    /// </summary>
    [Serializable]
    public class LevelData
    {
        public int LevelIndex;
        public DifficultyType Difficulty;
        public int TotalBottles;
        public int ColorCount;
        public int EmptyBottleCount;
        public int OptimalMoves; // M_opt
        public List<List<LiquidColor>> BottleConfigurations;

        public LevelData()
        {
            BottleConfigurations = new List<List<LiquidColor>>();
        }

        public LevelData(int levelIndex, DifficultyType difficulty, int totalBottles, int colorCount, int emptyBottleCount)
        {
            LevelIndex = levelIndex;
            Difficulty = difficulty;
            TotalBottles = totalBottles;
            ColorCount = colorCount;
            EmptyBottleCount = emptyBottleCount;
            OptimalMoves = 0;
            BottleConfigurations = new List<List<LiquidColor>>();
        }
    }
}
