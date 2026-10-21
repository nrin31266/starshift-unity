using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace StarShift.Persistence
{
    // Chỉ sở hữu JSON/file transaction; không biết scene hay gameplay owner.
    public sealed class JsonSaveStore
    {
        public string Path { get; }
        public string Diagnostic { get; private set; }
        public JsonSaveStore(string path) { Path = path ?? throw new ArgumentNullException(nameof(path)); }
        public SaveLoadStatus Load(Func<SaveEnvelope, bool> validate, out SaveEnvelope data)
        {
            data = null;
            Diagnostic = null;
            try
            {
                var status = Read(Path, validate, out data);
                if (status == SaveLoadStatus.Loaded || status == SaveLoadStatus.UnsupportedVersion) return status;
                var backup = Read(Path + ".bak", validate, out var recovered);
                if (backup == SaveLoadStatus.Loaded) { data = recovered; Diagnostic = "Primary " + status + "; loaded backup."; return SaveLoadStatus.RecoveredBackup; }
                Diagnostic = "Primary " + status + "; backup " + backup;
                return status == SaveLoadStatus.Missing && backup != SaveLoadStatus.Missing ? backup : status;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            { Diagnostic = e.Message; return SaveLoadStatus.IoFailure; }
        }
        private static SaveLoadStatus Read(string path, Func<SaveEnvelope, bool> validate, out SaveEnvelope data)
        {
            data = null;
            if (!File.Exists(path)) return SaveLoadStatus.Missing;
            try
            {
                var candidate = JsonUtility.FromJson<SaveEnvelope>(File.ReadAllText(path));
                if (candidate == null || candidate.SchemaVersion <= 0) return SaveLoadStatus.Invalid;
                // V1 đã có Profile/ledger; chỉ bổ sung reserve checkpoint, giữ nguyên economy.
                if (candidate.SchemaVersion == 1)
                {
                    candidate.SchemaVersion = SaveValidation.CurrentVersion;
                    if (candidate.Campaign?.Checkpoint != null) candidate.Campaign.Checkpoint.Missiles = 1;
                }
                if (candidate.SchemaVersion != SaveValidation.CurrentVersion) return SaveLoadStatus.UnsupportedVersion;
                if (!validate(candidate)) return SaveLoadStatus.Invalid;
                data = candidate;
                return SaveLoadStatus.Loaded;
            }
            catch (ArgumentException) { return SaveLoadStatus.Invalid; }
        }
        internal SaveEnvelope ReadForRecovery()
        {
            try
            {
                var source = File.Exists(Path) ? Path : Path + ".bak";
                return JsonUtility.FromJson<SaveEnvelope>(File.ReadAllText(source));
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            { Diagnostic = e.Message; return null; }
        }
        public bool Write(SaveEnvelope data, Func<SaveEnvelope, bool> validate, bool resetHistory = false)
        {
            if (!validate(data)) { Diagnostic = "Refused invalid state."; return false; }
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(Path)));
                var bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data, true));
                using (var stream = new FileStream(Path + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
                { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                if (Read(Path + ".tmp", validate, out _) != SaveLoadStatus.Loaded) throw new IOException("Temp validation failed.");
                if (resetHistory)
                {
                    if (Read(Path, validate, out _) == SaveLoadStatus.UnsupportedVersion)
                    { Diagnostic = "Refused newer/unsupported save overwrite."; return false; }
                    // Xóa history trước commit; lỗi I/O vẫn giữ primary cũ, không phục hồi tiến độ đã reset từ backup.
                    File.Delete(Path + ".bak"); File.Delete(Path + ".invalid");
                    if (File.Exists(Path)) File.Replace(Path + ".tmp", Path, null);
                    else File.Move(Path + ".tmp", Path);
                    Diagnostic = null;
                    return true;
                }
                // Backup chỉ nhận primary hợp lệ; bản corrupt vẫn được giữ để chẩn đoán.
                if (File.Exists(Path))
                {
                    var prior = Read(Path, validate, out _);
                    if (prior == SaveLoadStatus.UnsupportedVersion) { Diagnostic = "Refused newer/unsupported save overwrite."; return false; }
                    if (prior == SaveLoadStatus.Loaded) File.Replace(Path + ".tmp", Path, Path + ".bak");
                    else
                    {
                        File.Copy(Path, Path + ".invalid", true);
                        File.Replace(Path + ".tmp", Path, null);
                    }
                }
                else File.Move(Path + ".tmp", Path);
                Diagnostic = null;
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            { Diagnostic = e.Message; return false; }
        }
    }
}
