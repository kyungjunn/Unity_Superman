using System;
using System.Text;

namespace GrowNa.Core
{
    /// <summary>
    /// 방치형 수치 표기 규약.
    ///
    /// 자료형 결정: <b>double 유지.</b>
    /// - 성장 곡선은 스테이지당 ×1.18 (StageTable). double 한계 1.7e308 = 스테이지 약 4,000 구간.
    /// - 1.0 목표 콘텐츠(월드 7 = 스테이지 70)와 짚 갈이 배수를 다 곱해도 1e60을 넘지 않는다.
    /// - 유효숫자 15자리라 화면에 찍는 3자리 표기에서 오차가 드러나지 않는다.
    /// 따라서 커스텀 BigNumber 구조체는 도입하지 않고, 표기만 이 클래스로 일원화한다.
    /// (한계에 닿으면 여기 Format 시그니처만 유지한 채 내부 자료형을 갈아끼운다.)
    /// </summary>
    public static class BigNum
    {
        const int PlainLimit = 1000;
        static readonly string[] SmallSuffix = { "", "K", "M", "B", "T" };
        static readonly string[] LongSuffix = BuildLongSuffix();

        static string[] BuildLongSuffix()
        {
            // aa, ab, ... zz — 1e15 부터 3자리씩. double 한계(1e308)를 한참 넘는다.
            var list = new string[676];
            var sb = new StringBuilder(2);
            for (int i = 0; i < list.Length; i++)
            {
                sb.Clear();
                sb.Append((char)('a' + i / 26));
                sb.Append((char)('a' + i % 26));
                list[i] = sb.ToString();
            }
            return list;
        }

        public static string Suffix(int tier)
        {
            if (tier < 0) return "";
            if (tier < SmallSuffix.Length) return SmallSuffix[tier];
            int index = tier - SmallSuffix.Length;
            return index < LongSuffix.Length ? LongSuffix[index] : "∞";
        }

        public static string Format(double value)
        {
            if (double.IsNaN(value)) return "0";
            if (double.IsInfinity(value)) return value > 0 ? "∞" : "-∞";

            bool negative = value < 0;
            value = Math.Abs(value);
            if (value < PlainLimit) return (negative ? "-" : "") + Math.Floor(value).ToString("0");

            int tier = 0;
            while (value >= PlainLimit && tier < 1000)
            {
                value /= PlainLimit;
                tier++;
            }

            string body = value < 10 ? value.ToString("0.00")
                        : value < 100 ? value.ToString("0.0")
                                      : value.ToString("0");
            return (negative ? "-" : "") + body + Suffix(tier);
        }

        public static string Percent(double ratio, int decimals = 2)
            => (ratio * 100.0).ToString("F" + Math.Clamp(decimals, 0, 4)) + "%";

        public static string Duration(double seconds)
        {
            if (seconds < 60) return $"{Math.Floor(seconds):0}초";
            int total = (int)Math.Floor(seconds);
            int hours = total / 3600;
            int minutes = total % 3600 / 60;
            if (hours <= 0) return $"{minutes}분";
            return minutes > 0 ? $"{hours}시간 {minutes}분" : $"{hours}시간";
        }
    }
}
