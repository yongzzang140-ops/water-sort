using System;
using System.Collections.Generic;
using System.Text;
using WaterSort.Core;

namespace WaterSort.Level
{
    /// <summary>
    /// BFS(너비 우선 탐색) 기반 최단 클리어 이동 횟수(M_opt) 연산 및 유효성(Solvable) 검증 솔버.
    /// 메모리 친화적 상태 정규화(Canonical State Hashing) 및 대칭성 가지치기 적용.
    /// </summary>
    public static class WaterSortSolver
    {
        public const int MAX_DEPTH_LIMIT = 50; // 과도한 연산 방지용 상한
        public const int MAX_VISITED_LIMIT = 150000;

        public struct SolverMove
        {
            public int From;
            public int To;

            public SolverMove(int from, int to)
            {
                From = from;
                To = to;
            }
        }

        public class SolverState
        {
            public byte[][] Tubes; // 각 병의 색상 ID 배열 (길이 0~4)
            public int Depth;
            public SolverState Parent;
            public SolverMove LastMove;

            public SolverState(int tubeCount)
            {
                Tubes = new byte[tubeCount][];
                Depth = 0;
                Parent = null;
            }

            public bool IsSolved()
            {
                for (int i = 0; i < Tubes.Length; i++)
                {
                    byte[] tube = Tubes[i];
                    if (tube.Length == 0) continue;
                    if (tube.Length != Bottle.CAPACITY) return false;
                    byte first = tube[0];
                    for (int j = 1; j < tube.Length; j++)
                    {
                        if (tube[j] != first) return false;
                    }
                }
                return true;
            }

            /// <summary>
            /// 병 순서와 무관한 정규화(Canonical) 키 생성하여 동일 보드 대칭 상태 중복 탐색 방지
            /// </summary>
            public string GetCanonicalKey()
            {
                string[] tubeKeys = new string[Tubes.Length];
                for (int i = 0; i < Tubes.Length; i++)
                {
                    byte[] t = Tubes[i];
                    if (t.Length == 0)
                    {
                        tubeKeys[i] = "0";
                    }
                    else
                    {
                        char[] chars = new char[t.Length];
                        for (int j = 0; j < t.Length; j++)
                        {
                            chars[j] = (char)('A' + t[j]);
                        }
                        tubeKeys[i] = new string(chars);
                    }
                }
                Array.Sort(tubeKeys, StringComparer.Ordinal);
                return string.Join("|", tubeKeys);
            }
        }

        /// <summary>
        /// 주어진 병 리스트로부터 최단 해결 경로(M_opt)를 계산.
        /// 풀이가 불가능한 경우 -1 반환.
        /// </summary>
        public static int Solve(List<Bottle> bottles, out List<SolverMove> solutionMoves)
        {
            solutionMoves = new List<SolverMove>();
            if (bottles == null || bottles.Count == 0) return -1;

            int n = bottles.Count;
            SolverState initial = new SolverState(n);
            for (int i = 0; i < n; i++)
            {
                var layers = bottles[i].Layers;
                initial.Tubes[i] = new byte[layers.Count];
                for (int j = 0; j < layers.Count; j++)
                {
                    initial.Tubes[i][j] = (byte)layers[j].Id;
                }
            }

            if (initial.IsSolved()) return 0;

            Queue<SolverState> queue = new Queue<SolverState>();
            HashSet<string> visited = new HashSet<string>();

            string startKey = initial.GetCanonicalKey();
            visited.Add(startKey);
            queue.Enqueue(initial);

            SolverState goalState = null;

            while (queue.Count > 0)
            {
                SolverState curr = queue.Dequeue();

                if (curr.Depth >= MAX_DEPTH_LIMIT || visited.Count >= MAX_VISITED_LIMIT)
                {
                    break;
                }

                // 가능한 모든 이동 전개
                bool emptyTubeTargetUsed = false;

                for (int from = 0; from < n; from++)
                {
                    byte[] fromTube = curr.Tubes[from];
                    if (fromTube.Length == 0) continue;

                    // 이미 완성된 병(단일 색상 4개)은 붓지 않음
                    if (fromTube.Length == Bottle.CAPACITY && IsAllSame(fromTube)) continue;

                    byte topColor = fromTube[fromTube.Length - 1];

                    // 연속된 동일 색상 수
                    int consecutive = 0;
                    for (int k = fromTube.Length - 1; k >= 0; k--)
                    {
                        if (fromTube[k] == topColor) consecutive++;
                        else break;
                    }

                    for (int to = 0; to < n; to++)
                    {
                        if (from == to) continue;
                        byte[] toTube = curr.Tubes[to];

                        if (toTube.Length >= Bottle.CAPACITY) continue;

                        // 빈 병 대칭성 가지치기 (빈 병 하나에 부어봤으면 다른 빈 병에 붓는 것은 동일)
                        if (toTube.Length == 0)
                        {
                            if (consecutive == fromTube.Length)
                            {
                                // 이미 단일 색상으로만 차 있는 상태를 빈 병으로 옮기는 것은 낭비
                                continue;
                            }
                            if (emptyTubeTargetUsed) continue;
                        }
                        else
                        {
                            if (toTube[toTube.Length - 1] != topColor) continue;
                        }

                        // 역이동 방지 (직전 이동의 반대)
                        if (curr.Parent != null && curr.LastMove.From == to && curr.LastMove.To == from)
                        {
                            continue;
                        }

                        int space = Bottle.CAPACITY - toTube.Length;
                        int amount = Math.Min(consecutive, space);
                        if (amount <= 0) continue;

                        // 새 상태 생성
                        SolverState next = CloneState(curr);
                        next.Depth = curr.Depth + 1;
                        next.Parent = curr;
                        next.LastMove = new SolverMove(from, to);

                        // 이동 실행
                        ApplyMove(next.Tubes, from, to, amount);

                        if (toTube.Length == 0)
                        {
                            emptyTubeTargetUsed = true;
                        }

                        if (next.IsSolved())
                        {
                            goalState = next;
                            break;
                        }

                        string key = next.GetCanonicalKey();
                        if (!visited.Contains(key))
                        {
                            visited.Add(key);
                            queue.Enqueue(next);
                        }
                    }

                    if (goalState != null) break;
                }

                if (goalState != null) break;
            }

            if (goalState != null)
            {
                // 경로 역추적
                SolverState tracer = goalState;
                while (tracer.Parent != null)
                {
                    solutionMoves.Insert(0, tracer.LastMove);
                    tracer = tracer.Parent;
                }
                return goalState.Depth;
            }

            return -1; // Unsolvable or limit exceeded
        }

        private static bool IsAllSame(byte[] tube)
        {
            byte f = tube[0];
            for (int i = 1; i < tube.Length; i++)
            {
                if (tube[i] != f) return false;
            }
            return true;
        }

        private static SolverState CloneState(SolverState source)
        {
            SolverState clone = new SolverState(source.Tubes.Length);
            for (int i = 0; i < source.Tubes.Length; i++)
            {
                byte[] orig = source.Tubes[i];
                byte[] copy = new byte[orig.Length];
                Array.Copy(orig, copy, orig.Length);
                clone.Tubes[i] = copy;
            }
            return clone;
        }

        private static void ApplyMove(byte[][] tubes, int from, int to, int amount)
        {
            byte[] fromArr = tubes[from];
            byte[] toArr = tubes[to];
            byte color = fromArr[fromArr.Length - 1];

            int newFromLen = fromArr.Length - amount;
            byte[] newFrom = new byte[newFromLen];
            Array.Copy(fromArr, newFrom, newFromLen);

            int newToLen = toArr.Length + amount;
            byte[] newTo = new byte[newToLen];
            Array.Copy(toArr, newTo, toArr.Length);
            for (int i = toArr.Length; i < newToLen; i++)
            {
                newTo[i] = color;
            }

            tubes[from] = newFrom;
            tubes[to] = newTo;
        }
    }
}
