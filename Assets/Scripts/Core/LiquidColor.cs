using System;
using UnityEngine;

namespace WaterSort.Core
{
    /// <summary>
    /// 게임 내에서 사용되는 액체 색상 ID 및 파스텔톤 비주얼 컬러 데이터 정의
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
    /// 모던 파스텔톤 기본 색상 팔레트 레퍼런스
    /// </summary>
    public static class PastelPalette
    {
        public static readonly LiquidColor[] Colors = new LiquidColor[]
        {
            new LiquidColor(1,  "Pastel Rose",       new Color(1.00f, 0.60f, 0.67f, 1f)), // #FF99AA
            new LiquidColor(2,  "Pastel Mint",       new Color(0.58f, 0.90f, 0.77f, 1f)), // #94E6C4
            new LiquidColor(3,  "Pastel Lavender",   new Color(0.78f, 0.72f, 0.96f, 1f)), // #C7B8F5
            new LiquidColor(4,  "Pastel Sky",        new Color(0.56f, 0.83f, 0.98f, 1f)), // #8FD4FA
            new LiquidColor(5,  "Pastel Lemon",      new Color(1.00f, 0.94f, 0.58f, 1f)), // #FFF094
            new LiquidColor(6,  "Pastel Peach",      new Color(1.00f, 0.76f, 0.62f, 1f)), // #FFC29E
            new LiquidColor(7,  "Pastel Lilac",      new Color(0.92f, 0.64f, 0.90f, 1f)), // #EBA3E6
            new LiquidColor(8,  "Pastel Coral",      new Color(1.00f, 0.52f, 0.52f, 1f)), // #FF8585
            new LiquidColor(9,  "Pastel Emerald",    new Color(0.52f, 0.88f, 0.62f, 1f)), // #85E09E
            new LiquidColor(10, "Pastel Deep Ocean", new Color(0.43f, 0.62f, 0.89f, 1f)), // #6E9EE3
            new LiquidColor(11, "Pastel Apricot",    new Color(0.98f, 0.82f, 0.58f, 1f)), // #FAD194
            new LiquidColor(12, "Pastel Periwinkle", new Color(0.68f, 0.73f, 0.98f, 1f))  // #AEBAFA
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
            Color dynamicPastel = Color.HSVToRGB(hue, 0.45f, 0.95f);
            return new LiquidColor(id, $"Custom {id}", dynamicPastel);
        }
    }
}
