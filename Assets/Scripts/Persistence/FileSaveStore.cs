using System;
using System.IO;
using UnityEngine;

namespace GrowNa.Persistence
{
    public sealed class FileSaveStore : ISaveStore
    {
        public const string FileName = "grow_na_save.json";

        readonly string path;

        public FileSaveStore(string explicitPath = null)
        {
            path = string.IsNullOrEmpty(explicitPath)
                ? System.IO.Path.Combine(Application.persistentDataPath, FileName)
                : explicitPath;
        }

        public string Path => path;
        public DateTime? LastWriteUtc => File.Exists(path) ? File.GetLastWriteTimeUtc(path) : (DateTime?)null;
        public int WriteCount { get; private set; }
        public int DeleteCount { get; private set; }
        public int RenameCount { get; private set; }
        public int TruncateCount { get; private set; }

        public SaveStoreRead Read()
        {
            try
            {
                if (!File.Exists(path)) return SaveStoreRead.NotFound();
                return SaveStoreRead.BytesOf(File.ReadAllBytes(path));
            }
            catch (UnauthorizedAccessException e)
            {
                return SaveStoreRead.Io(e.Message);
            }
            catch (DirectoryNotFoundException)
            {
                return SaveStoreRead.NotFound();
            }
            catch (FileNotFoundException)
            {
                return SaveStoreRead.NotFound();
            }
            catch (IOException e)
            {
                return SaveStoreRead.Io(e.Message);
            }
        }

        public bool Write(byte[] bytes)
        {
            if (bytes == null) return false;
            WriteCount++;
            string directory = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            string staging = path + ".tmp";
            try
            {
                File.WriteAllBytes(staging, bytes);
                if (File.Exists(path)) File.Delete(path);
                File.Move(staging, path);
                return true;
            }
            catch (IOException e)
            {
                Debug.LogWarning($"[GrowNa] 세이브 실패: {e.Message}");
                try { if (File.Exists(staging)) File.Delete(staging); } catch (IOException) { }
                return false;
            }
        }

        public bool Delete()
        {
            DeleteCount++;
            try
            {
                if (File.Exists(path)) File.Delete(path);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }

        public bool Rename(string destinationPath)
        {
            RenameCount++;
            try
            {
                File.Move(path, destinationPath);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }

        public bool Truncate()
        {
            TruncateCount++;
            try
            {
                File.WriteAllBytes(path, Array.Empty<byte>());
                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }
    }

    public sealed class MemorySaveStore : ISaveStore
    {
        byte[] bytes;
        bool exists;

        public string Path { get; } = "memory://save";
        public DateTime? LastWriteUtc { get; private set; }
        public int WriteCount { get; private set; }
        public int DeleteCount { get; private set; }
        public int RenameCount { get; private set; }
        public int TruncateCount { get; private set; }
        public bool FailNextWrite { get; set; }

        public void Seed(byte[] seeded)
        {
            bytes = seeded == null ? null : (byte[])seeded.Clone();
            exists = seeded != null;
            LastWriteUtc = exists ? DateTime.UtcNow : (DateTime?)null;
        }

        public byte[] Snapshot() => exists && bytes != null ? (byte[])bytes.Clone() : null;

        public SaveStoreRead Read()
        {
            if (!exists) return SaveStoreRead.NotFound();
            return SaveStoreRead.BytesOf(bytes == null ? Array.Empty<byte>() : (byte[])bytes.Clone());
        }

        public bool Write(byte[] next)
        {
            WriteCount++;
            if (FailNextWrite)
            {
                FailNextWrite = false;
                return false;
            }
            bytes = next == null ? Array.Empty<byte>() : (byte[])next.Clone();
            exists = true;
            LastWriteUtc = DateTime.UtcNow;
            return true;
        }

        public bool Delete()
        {
            DeleteCount++;
            exists = false;
            bytes = null;
            LastWriteUtc = null;
            return true;
        }

        public bool Rename(string destinationPath)
        {
            RenameCount++;
            return false;
        }

        public bool Truncate()
        {
            TruncateCount++;
            if (!exists) return false;
            bytes = Array.Empty<byte>();
            LastWriteUtc = DateTime.UtcNow;
            return true;
        }
    }
}
