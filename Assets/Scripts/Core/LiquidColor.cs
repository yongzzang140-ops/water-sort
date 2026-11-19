using System;
using UnityEngine;

namespace WaterSort.Core
{
    /// <summary>
    /// 게임 내에서 사용되는 액체 색상 ID 및 트렌디 네온-파스텔 비주얼 컬러 데이터 정의
    /// </summary>
    [Serializable]
    public struct LiquidColor : IEquatable<LiquidColor>
    {
        public int Id;
        public string Name;
        public Color ColorValue;

        public LiquidColor(int id, string name, Color colorValue)
        {
            Id = id;
            Name = name;
            ColorValue = colorValue;
        }

        public static LiquidColor None => new LiquidColor(0, "Empty", Color.clear);

        public bool IsValid => Id > 0;

        public bool Equals(LiquidColor other)
        {
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is LiquidColor other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public static bool operator ==(LiquidColor left, LiquidColor right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LiquidColor left, LiquidColor right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"[{Id}] {Name}";
        }
    }

    /// <summary>
    /// 2025-2026 트렌디 네온-파스텔 12종 시그니처 색상 팔레트 레퍼런스
    /// </summary>
    public static class PastelPalette
    {
        public static readonly LiquidColor[] Colors = new LiquidColor[]
        {
            new LiquidColor(1,  "Neon Candy Pink",   new Color(0.98f, 0.42f, 0.62f, 1f)), // #FA6B9E
            new LiquidColor(2,  "Mint Fizz",         new Color(0.24f, 0.85f, 0.68f, 1f)), // #3DD9AD
            new LiquidColor(3,  "Electric Lilac",    new Color(0.70f, 0.48f, 0.98f, 1f)), // #B37AFA
            new LiquidColor(4,  "Cyber Peach",       new Color(0.98f, 0.58f, 0.40f, 1f)), // #FA9466
            new LiquidColor(5,  "Neon Sky",          new Color(0.30f, 0.72f, 1.00f, 1f)), // #4DB8FF
            new LiquidColor(6,  "Lemon Glaze",       new Color(0.98f, 0.88f, 0.32f, 1f)), // #FAE052
            new LiquidColor(7,  "Aurora Coral",      new Color(0.98f, 0.42f, 0.46f, 1f)), // #FA6B75
            new LiquidColor(8,  "Cyber Periwinkle",  new Color(0.52f, 0.58f, 0.98f, 1f)), // #8594FA
            new LiquidColor(9,  "Hyper Emerald",     new Color(0.32f, 0.85f, 0.48f, 1f)), // #52D97A
            new LiquidColor(10, "Deep Ocean Glow",   new Color(0.30f, 0.52f, 0.92f, 1f)), // #4D85EB
            new LiquidColor(11, "Apricot Sorbet",    new Color(0.98f, 0.70f, 0.42f, 1f)), // #FAB36B
            new LiquidColor(12, "Cosmic Lavender",   new Color(0.82f, 0.52f, 0.92f, 1f))  // #D185EB
        };

        public static LiquidColor GetColorById(int id)
        {
            if (id <= 0) return LiquidColor.None;
            for (int i = 0; i < Colors.Length; i++)
            {
                if (Colors[i].Id == id) return Colors[i];
            }
            // fallback generated color if out of bounds
            float hue = ((id * 37) % 360) / 360f;
            Color dynamicPastel = Color.HSVToRGB(hue, 0.50f, 0.95f);
            return new LiquidColor(id, $"Custom Pastel {id}", dynamicPastel);
        }
    }
}
