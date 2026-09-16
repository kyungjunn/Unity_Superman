using System;

namespace GrowNa.Persistence
{
    public interface ISaveStore
    {
        SaveStoreRead Read();
        bool Write(byte[] bytes);
        bool Delete();
        bool Rename(string destinationPath);
        bool Truncate();
        string Path { get; }
        DateTime? LastWriteUtc { get; }
        int WriteCount { get; }
        int DeleteCount { get; }
        int RenameCount { get; }
        int TruncateCount { get; }
    }

    public readonly struct SaveStoreRead
    {
        public readonly SaveStoreReadKind Kind;
        public readonly byte[] Bytes;
        public readonly string Message;

        public SaveStoreRead(SaveStoreReadKind kind, byte[] bytes = null, string message = "")
        {
            Kind = kind;
            Bytes = bytes;
            Message = message ?? "";
        }

        public static SaveStoreRead NotFound() => new SaveStoreRead(SaveStoreReadKind.NotFound);
        public static SaveStoreRead BytesOf(byte[] bytes) => new SaveStoreRead(SaveStoreReadKind.Bytes, bytes);
        public static SaveStoreRead Io(string message) => new SaveStoreRead(SaveStoreReadKind.IoFailure, null, message);
    }

    public enum SaveStoreReadKind
    {
        NotFound = 0,
        Bytes = 1,
        IoFailure = 2,
    }

    public interface IClock
    {
        DateTime UtcNow { get; }
    }

    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }

    public sealed class FixedClock : IClock
    {
        public DateTime UtcNow { get; set; }

        public FixedClock(DateTime utcNow) => UtcNow = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
    }
}
