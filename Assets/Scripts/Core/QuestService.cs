using System;
using UnityEngine;

namespace GrowNa.Core
{
    public enum QuestKind { Kill = 0, StageClear = 1, Offer = 2, SkillDraw = 3, PetDraw = 4 }

    public readonly struct QuestDef
    {
        public readonly QuestKind kind;
        public readonly string title;
        public readonly string unit;
        public readonly int target;
        public readonly double gem;

        public QuestDef(QuestKind kind, string title, string unit, int target, double gem)
        {
            this.kind = kind;
            this.title = title;
            this.unit = unit;
            this.target = target;
            this.gem = gem;
        }

        public string Describe(int progress)
            => $"{title} {Mathf.Min(progress, target)}/{target}{unit}";
    }

    public static class QuestTable
    {
        static readonly QuestDef[] Defs =
        {
            new QuestDef(QuestKind.Kill,       "몬스터 처치",  "마리", 25, 10),
            new QuestDef(QuestKind.StageClear, "스테이지 클리어", "회",  3, 15),
            new QuestDef(QuestKind.Offer,      "등불 봉헌",    "회",  10,  6),
            new QuestDef(QuestKind.SkillDraw,  "스킬 뽑기",    "회",  10, 12),
            new QuestDef(QuestKind.PetDraw,    "펫 뽑기",      "회",   5, 15),
        };

        public static int Count => Defs.Length;

        public static QuestDef Get(QuestKind kind) => Defs[(int)kind];

        public static QuestDef Get(int index) => Defs[Math.Clamp(index, 0, Defs.Length - 1)];

        public static bool IsValid(int index) => index >= 0 && index < Defs.Length;

        public static int Target(QuestKind kind) => Get(kind).target;

        public static double Gem(QuestKind kind) => Get(kind).gem;

        public static bool IsReady(QuestKind kind, int progress) => progress >= Target(kind);

        public static int Remaining(QuestKind kind, int progress)
            => Math.Max(0, Target(kind) - progress);

        public static double Ratio(QuestKind kind, int progress)
            => Mathf.Clamp01(progress / (float)Target(kind));
    }

    public class QuestService : MonoBehaviour
    {
        Wallet wallet;

        [SerializeField] int[] progress = new int[QuestTable.Count];
        [SerializeField] int[] rounds = new int[QuestTable.Count];

        public event Action Changed;
        public event Action<double> GemRewarded;

        public void Bind(Wallet boundWallet) => wallet = boundWallet;

        public int Progress(QuestKind kind) => progress[(int)kind];

        public int Rounds(QuestKind kind) => rounds[(int)kind];

        public bool IsReady(QuestKind kind) => QuestTable.IsReady(kind, Progress(kind));

        public int Remaining(QuestKind kind) => QuestTable.Remaining(kind, Progress(kind));

        public double Ratio(QuestKind kind) => QuestTable.Ratio(kind, Progress(kind));

        public int ReadyCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < QuestTable.Count; i++)
                    if (IsReady((QuestKind)i)) count++;
                return count;
            }
        }

        public void Report(QuestKind kind, int amount = 1)
        {
            if (amount <= 0) return;
            progress[(int)kind] += amount;
            Changed?.Invoke();
        }

        public void ReportKill(bool boss)
        {
            progress[(int)QuestKind.Kill]++;
            if (boss) progress[(int)QuestKind.StageClear]++;
            Changed?.Invoke();
        }

        public bool TryClaim(QuestKind kind)
        {
            if (!IsReady(kind)) return false;

            progress[(int)kind] -= QuestTable.Target(kind);
            rounds[(int)kind]++;

            double gem = QuestTable.Gem(kind);
            wallet?.Add(CurrencyKind.Gem, gem);
            GemRewarded?.Invoke(gem);
            Changed?.Invoke();
            return true;
        }

        public int ClaimAll()
        {
            int claimed = 0;
            for (int i = 0; i < QuestTable.Count; i++)
                if (TryClaim((QuestKind)i)) claimed++;
            return claimed;
        }

        public void LoadFrom(int[] savedProgress, int[] savedRounds)
        {
            Copy(savedProgress, progress);
            Copy(savedRounds, rounds);
            Changed?.Invoke();
        }

        static void Copy(int[] source, int[] target)
        {
            Array.Clear(target, 0, target.Length);
            if (source == null) return;
            for (int i = 0; i < target.Length && i < source.Length; i++)
                target[i] = Math.Max(0, source[i]);
        }

        public int[] ProgressSnapshot() => (int[])progress.Clone();

        public int[] RoundsSnapshot() => (int[])rounds.Clone();
    }
}
