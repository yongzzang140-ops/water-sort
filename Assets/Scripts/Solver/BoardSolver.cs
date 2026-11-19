using System;
using System.Collections.Generic;
using WaterSort.Core;
using WaterSort.Level;

namespace WaterSort.Solver
{
    /// <summary>
    /// 너비 우선 탐색(BFS) 기반 보드 최적수 연산 솔버 (BoardSolver).
    /// 현재 스테이지의 최단 클리어 이동 횟수(M_opt)를 계산합니다.
    /// </summary>
    public static class BoardSolver
    {
        public static int CalculateOptimalMoves(List<Bottle> bottles)
        {
            if (bottles == null || bottles.Count == 0) return 0;

            // WaterSortSolver BFS 알고리즘을 활용한 최단 경로 탐색
            int mOpt = WaterSortSolver.Solve(bottles, out _);
            return mOpt > 0 ? mOpt : EstimateFallbackMoves(bottles);
        }

        private static int EstimateFallbackMoves(List<Bottle> bottles)
        {
            int filledCount = 0;
            for (int i = 0; i < bottles.Count; i++)
            {
                if (!bottles[i].IsEmpty) filledCount++;
            }
            return Math.Max(4, filledCount * 2);
        }
    }
}
