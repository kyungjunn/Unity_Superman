using System;
using GrowNa.Gear;

namespace GrowNa.Persistence
{
    public enum SaveReadStatus
    {
        NotFound = 0,
        Loaded = 1,
        UnsupportedVersion = 2,
        InvalidData = 3,
        IoFailure = 4,
    }

    public readonly struct SaveReadResult
    {
        public readonly SaveReadStatus Status;
        public readonly SaveData Data;
        public readonly int ParsedVersion;
        public readonly string ErrorCode;
        public readonly string JsonPath;
        public readonly string Message;

        SaveReadResult(SaveReadStatus status, SaveData data, int parsedVersion, string errorCode, string jsonPath, string message)
        {
            Status = status;
            Data = data;
            ParsedVersion = parsedVersion;
            ErrorCode = errorCode ?? "";
            JsonPath = jsonPath ?? "";
            Message = message ?? "";
        }

        public bool CanProceed => Status == SaveReadStatus.NotFound || Status == SaveReadStatus.Loaded;

        public static SaveReadResult NotFound()
            => new SaveReadResult(SaveReadStatus.NotFound, null, 0, "", "", "");

        public static SaveReadResult Loaded(SaveData data)
            => new SaveReadResult(SaveReadStatus.Loaded, data, SaveData.CurrentVersion, "", "", "");

        public static SaveReadResult Unsupported(int version, string path)
            => new SaveReadResult(SaveReadStatus.UnsupportedVersion, null, version, "unsupported_version", path, $"version={version}");

        public static SaveReadResult Invalid(string code, string path, string message)
            => new SaveReadResult(SaveReadStatus.InvalidData, null, 0, code, path, message);

        public static SaveReadResult Io(string message)
            => new SaveReadResult(SaveReadStatus.IoFailure, null, 0, "io_failure", "", message);
    }

    public enum SaveWriteStatus
    {
        Opened = 0,
        Blocked = 1,
        Closed = 2,
        InvalidCapture = 3,
        IoFailure = 4,
        Succeeded = 5,
    }

    public readonly struct SaveWriteResult
    {
        public readonly SaveWriteStatus Status;
        public readonly string Message;

        public SaveWriteResult(SaveWriteStatus status, string message = "")
        {
            Status = status;
            Message = message ?? "";
        }

        public bool Succeeded => Status == SaveWriteStatus.Succeeded;
    }

    public enum BootstrapPhase
    {
        Cold = 0,
        Initializing = 1,
        Ready = 2,
        Failed = 3,
        Stopped = 4,
        InProgress = 5,
    }

    public readonly struct BootstrapResult
    {
        public readonly BootstrapPhase Phase;
        public readonly SaveReadResult Read;
        public readonly string Failure;

        public BootstrapResult(BootstrapPhase phase, SaveReadResult read = default, string failure = "")
        {
            Phase = phase;
            Read = read;
            Failure = failure ?? "";
        }

        public bool IsReady => Phase == BootstrapPhase.Ready;
        public bool IsFailed => Phase == BootstrapPhase.Failed;
    }

    public static class EmptyGear
    {
        public static GearItem Slot(int index)
        {
            return new GearItem
            {
                slot = (GearSlot)index,
                tier = GearTier.Worn,
                plus = 0,
                owned = false,
                level = 0,
            };
        }
    }
}
