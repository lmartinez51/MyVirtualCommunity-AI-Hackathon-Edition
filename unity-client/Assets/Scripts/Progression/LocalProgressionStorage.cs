using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace MyVirtualCommunity.Client.Progression
{
    public enum ProgressionLoadStatus
    {
        Loaded,
        LoadedFromBackup,
        NotFound,
        Corrupted,
        UnsupportedVersion,
        IoError
    }

    public sealed class ProgressionLoadResult
    {
        public ProgressionLoadStatus Status { get; }
        public ProgressionSaveData Data { get; }
        public string SourcePath { get; }
        public bool BackupUsed { get; }
        public int DetectedSchemaVersion { get; }
        public string ErrorMessage { get; }

        public bool IsSuccess => Status == ProgressionLoadStatus.Loaded || Status == ProgressionLoadStatus.LoadedFromBackup;

        public ProgressionLoadResult(
            ProgressionLoadStatus status,
            ProgressionSaveData data,
            string sourcePath,
            bool backupUsed,
            int detectedSchemaVersion,
            string errorMessage)
        {
            Status = status;
            Data = data;
            SourcePath = sourcePath ?? string.Empty;
            BackupUsed = backupUsed;
            DetectedSchemaVersion = detectedSchemaVersion;
            ErrorMessage = errorMessage ?? string.Empty;
        }
    }

    public sealed class ProgressionSaveResult
    {
        public bool Success { get; }
        public string TargetPath { get; }
        public bool BackupCreated { get; }
        public string ArchivedPath { get; }
        public string ErrorMessage { get; }

        public ProgressionSaveResult(
            bool success,
            string targetPath,
            bool backupCreated,
            string archivedPath,
            string errorMessage)
        {
            Success = success;
            TargetPath = targetPath ?? string.Empty;
            BackupCreated = backupCreated;
            ArchivedPath = archivedPath ?? string.Empty;
            ErrorMessage = errorMessage ?? string.Empty;
        }
    }

    /// <summary>
    /// Offline, atomic, versioned local file storage for single-player narrative progression.
    /// Uses Application.persistentDataPath by default, maintains a verified .bak backup,
    /// and refuses to silently overwrite malformed or unsupported save files.
    /// </summary>
    public sealed class LocalProgressionStorage
    {
        public const string DefaultSubdirectory = "Saves";
        public const string DefaultSaveFileName = "progression_save.json";

        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false, true);

        public string SaveDirectory { get; }
        public string PrimaryFilePath { get; }
        public string BackupFilePath { get; }
        public string TempFilePath { get; }

        public LocalProgressionStorage()
            : this(Path.Combine(Application.persistentDataPath, DefaultSubdirectory), DefaultSaveFileName)
        {
        }

        public LocalProgressionStorage(string saveDirectory, string saveFileName = DefaultSaveFileName)
        {
            if (string.IsNullOrWhiteSpace(saveDirectory))
            {
                throw new ArgumentException("Save directory cannot be empty.", nameof(saveDirectory));
            }
            if (string.IsNullOrWhiteSpace(saveFileName))
            {
                saveFileName = DefaultSaveFileName;
            }

            SaveDirectory = Path.GetFullPath(saveDirectory);
            PrimaryFilePath = Path.Combine(SaveDirectory, saveFileName);
            BackupFilePath = PrimaryFilePath + ".bak";
            TempFilePath = PrimaryFilePath + ".tmp";
        }

        public bool HasPrimaryFile => File.Exists(PrimaryFilePath);
        public bool HasBackupFile => File.Exists(BackupFilePath);

        /// <summary>
        /// Loads local progression from the primary file, falling back to the verified backup
        /// only when the primary file is missing, malformed, or truncated (never downgrading an
        /// unsupported newer schema file).
        /// </summary>
        public ProgressionLoadResult Load()
        {
            bool primaryExists = File.Exists(PrimaryFilePath);
            bool backupExists = File.Exists(BackupFilePath);

            if (!primaryExists && !backupExists)
            {
                return new ProgressionLoadResult(
                    ProgressionLoadStatus.NotFound,
                    null,
                    PrimaryFilePath,
                    backupUsed: false,
                    detectedSchemaVersion: 0,
                    errorMessage: "No local progression save file exists.");
            }

            ProgressionLoadResult primaryResult = null;
            if (primaryExists)
            {
                primaryResult = TryReadAndValidateFile(PrimaryFilePath, isBackup: false);
                if (primaryResult.Status == ProgressionLoadStatus.Loaded)
                {
                    return primaryResult;
                }

                // Never silently downgrade or bypass a save from an unsupported newer schema version.
                if (primaryResult.Status == ProgressionLoadStatus.UnsupportedVersion)
                {
                    return primaryResult;
                }
            }

            // Primary was missing or corrupted/truncated — attempt backup recovery.
            if (backupExists)
            {
                ProgressionLoadResult backupResult = TryReadAndValidateFile(BackupFilePath, isBackup: true);
                if (backupResult.Status == ProgressionLoadStatus.Loaded)
                {
                    string reason = primaryResult != null
                        ? $"Primary save could not be loaded ({primaryResult.ErrorMessage}); recovered from backup."
                        : "Primary save file was missing; recovered from backup.";

                    return new ProgressionLoadResult(
                        ProgressionLoadStatus.LoadedFromBackup,
                        backupResult.Data,
                        BackupFilePath,
                        backupUsed: true,
                        detectedSchemaVersion: backupResult.DetectedSchemaVersion,
                        errorMessage: reason);
                }

                if (primaryResult == null)
                {
                    return new ProgressionLoadResult(
                        backupResult.Status,
                        null,
                        BackupFilePath,
                        backupUsed: true,
                        detectedSchemaVersion: backupResult.DetectedSchemaVersion,
                        errorMessage: $"Primary save missing and backup failed to load: {backupResult.ErrorMessage}");
                }

                return new ProgressionLoadResult(
                    primaryResult.Status,
                    null,
                    PrimaryFilePath,
                    backupUsed: false,
                    detectedSchemaVersion: primaryResult.DetectedSchemaVersion,
                    errorMessage: $"Primary save failed ({primaryResult.ErrorMessage}) and backup also failed ({backupResult.ErrorMessage}).");
            }

            return primaryResult ?? new ProgressionLoadResult(
                ProgressionLoadStatus.NotFound,
                null,
                PrimaryFilePath,
                backupUsed: false,
                detectedSchemaVersion: 0,
                errorMessage: "No local progression save file exists.");
        }

        /// <summary>
        /// Safely writes progression state atomically via a flushed temporary file.
        /// Preserves the prior valid primary file as .bak and refuses to silently clobber
        /// an unreadable or unsupported primary file unless explicitly archived.
        /// </summary>
        public ProgressionSaveResult Save(ProgressionSaveData data, bool archiveUnreadableExisting = false)
        {
            if (data == null)
            {
                return new ProgressionSaveResult(false, PrimaryFilePath, false, string.Empty, "ProgressionSaveData is null.");
            }

            if (data.schemaVersion < 1 || data.schemaVersion > ProgressionSaveData.CurrentSchemaVersion)
            {
                return new ProgressionSaveResult(
                    false,
                    PrimaryFilePath,
                    false,
                    string.Empty,
                    $"Refusing to save invalid schemaVersion {data.schemaVersion}.");
            }

            try
            {
                Directory.CreateDirectory(SaveDirectory);

                ProgressionSaveData snapshot = data.DeepClone();
                string json = JsonUtility.ToJson(snapshot, true);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new ProgressionSaveResult(false, PrimaryFilePath, false, string.Empty, "JSON serialization returned empty content.");
                }

                // Sanity-check serialized output before touching existing files
                ProgressionLoadResult selfCheck = ParseAndValidateJson(json, TempFilePath, isBackup: false);
                if (selfCheck.Status != ProgressionLoadStatus.Loaded)
                {
                    return new ProgressionSaveResult(false, PrimaryFilePath, false, string.Empty, $"Serialized JSON failed self-check: {selfCheck.ErrorMessage}");
                }

                bool primaryExists = File.Exists(PrimaryFilePath);
                bool primaryIsValid = false;
                string archivedPath = string.Empty;

                if (primaryExists)
                {
                    ProgressionLoadResult existingCheck = TryReadAndValidateFile(PrimaryFilePath, isBackup: false);
                    primaryIsValid = existingCheck.Status == ProgressionLoadStatus.Loaded;

                    if (!primaryIsValid)
                    {
                        if (!archiveUnreadableExisting)
                        {
                            return new ProgressionSaveResult(
                                false,
                                PrimaryFilePath,
                                false,
                                string.Empty,
                                $"Refusing to overwrite unreadable or unsupported existing save ({existingCheck.Status}: {existingCheck.ErrorMessage}).");
                        }

                        string suffix = existingCheck.Status == ProgressionLoadStatus.UnsupportedVersion
                            ? "unsupported"
                            : "corrupt";
                        archivedPath = ArchiveFileSafely(PrimaryFilePath, suffix);
                        primaryExists = File.Exists(PrimaryFilePath);
                    }
                }

                WriteAllTextFlushed(TempFilePath, json);

                bool backupCreated = false;
                if (primaryExists && primaryIsValid)
                {
                    backupCreated = ReplacePrimaryKeepingValidBackup();
                }
                else
                {
                    if (File.Exists(PrimaryFilePath))
                    {
                        File.Delete(PrimaryFilePath);
                    }
                    File.Move(TempFilePath, PrimaryFilePath);
                    backupCreated = File.Exists(BackupFilePath);
                }

                CleanupTempFileQuietly();
                return new ProgressionSaveResult(true, PrimaryFilePath, backupCreated, archivedPath, string.Empty);
            }
            catch (Exception ex)
            {
                CleanupTempFileQuietly();
                return new ProgressionSaveResult(false, PrimaryFilePath, false, string.Empty, ex.Message);
            }
        }

        /// <summary>
        /// Archives the current primary file to a timestamped copy so starting a new game
        /// never destroys a potentially recoverable file silently.
        /// </summary>
        public string ArchiveExistingPrimary(string reasonTag = "archived")
        {
            if (!File.Exists(PrimaryFilePath)) return string.Empty;
            return ArchiveFileSafely(PrimaryFilePath, reasonTag);
        }

        /// <summary>
        /// Archives both primary and stale .bak files before starting a fresh playthrough
        /// so a new game never inherits a prior playthrough's backup while keeping all
        /// previous save files recoverable on disk.
        /// </summary>
        public string ArchiveExistingSavesForNewGame(string reasonTag = "newgame")
        {
            string archivedPrimary = File.Exists(PrimaryFilePath)
                ? ArchiveFileSafely(PrimaryFilePath, reasonTag)
                : string.Empty;

            if (File.Exists(BackupFilePath))
            {
                string archivedBackup = ArchiveFileSafely(BackupFilePath, reasonTag + "_bak");
                if (string.IsNullOrEmpty(archivedPrimary))
                {
                    archivedPrimary = archivedBackup;
                }
            }

            CleanupTempFileQuietly();
            return archivedPrimary;
        }

        private bool ReplacePrimaryKeepingValidBackup()
        {
            try
            {
                // On Windows NTFS, File.Replace atomically replaces Primary with Temp and moves prior valid Primary to Backup.
                File.Replace(TempFilePath, PrimaryFilePath, BackupFilePath, ignoreMetadataErrors: true);
                return File.Exists(BackupFilePath);
            }
            catch (PlatformNotSupportedException)
            {
                return FallbackCopyBackupAndReplace();
            }
            catch (IOException)
            {
                return FallbackCopyBackupAndReplace();
            }
        }

        private bool FallbackCopyBackupAndReplace()
        {
            if (File.Exists(PrimaryFilePath))
            {
                File.Copy(PrimaryFilePath, BackupFilePath, overwrite: true);
                File.Delete(PrimaryFilePath);
            }
            File.Move(TempFilePath, PrimaryFilePath);
            return File.Exists(BackupFilePath);
        }

        private string ArchiveFileSafely(string sourcePath, string tag)
        {
            if (!File.Exists(sourcePath)) return string.Empty;
            Directory.CreateDirectory(SaveDirectory);

            string safeTag = string.IsNullOrWhiteSpace(tag) ? "archived" : tag.Trim();
            string stamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ");
            string baseName = Path.GetFileNameWithoutExtension(PrimaryFilePath);
            string ext = Path.GetExtension(PrimaryFilePath);
            string candidate = Path.Combine(SaveDirectory, $"{baseName}.{safeTag}.{stamp}{ext}");
            int attempt = 1;
            while (File.Exists(candidate))
            {
                candidate = Path.Combine(SaveDirectory, $"{baseName}.{safeTag}.{stamp}_{attempt}{ext}");
                attempt++;
            }

            File.Move(sourcePath, candidate);
            return candidate;
        }

        private static void WriteAllTextFlushed(string path, string content)
        {
            byte[] bytes = Utf8NoBom.GetBytes(content);
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            {
                fs.Write(bytes, 0, bytes.Length);
                fs.Flush(flushToDisk: true);
            }
        }

        private void CleanupTempFileQuietly()
        {
            try
            {
                if (File.Exists(TempFilePath))
                {
                    File.Delete(TempFilePath);
                }
            }
            catch
            {
                // Ignore best-effort temp cleanup errors
            }
        }

        private ProgressionLoadResult TryReadAndValidateFile(string path, bool isBackup)
        {
            string raw;
            try
            {
                raw = File.ReadAllText(path, Utf8NoBom);
            }
            catch (Exception ex)
            {
                return new ProgressionLoadResult(
                    ProgressionLoadStatus.IoError,
                    null,
                    path,
                    isBackup,
                    0,
                    $"Failed to read file: {ex.Message}");
            }

            return ParseAndValidateJson(raw, path, isBackup);
        }

        private static ProgressionLoadResult ParseAndValidateJson(string raw, string sourcePath, bool isBackup)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return new ProgressionLoadResult(
                    ProgressionLoadStatus.Corrupted,
                    null,
                    sourcePath,
                    isBackup,
                    0,
                    "Save file is empty.");
            }

            string trimmed = raw.Trim();
            if (!trimmed.StartsWith("{", StringComparison.Ordinal) || !trimmed.EndsWith("}", StringComparison.Ordinal))
            {
                return new ProgressionLoadResult(
                    ProgressionLoadStatus.Corrupted,
                    null,
                    sourcePath,
                    isBackup,
                    0,
                    "Save file JSON is truncated or malformed.");
            }

            // Verify balanced braces outside JSON strings to catch subtle truncated files that end with a nested '}'
            if (!HasBalancedJsonBraces(trimmed))
            {
                return new ProgressionLoadResult(
                    ProgressionLoadStatus.Corrupted,
                    null,
                    sourcePath,
                    isBackup,
                    0,
                    "Save file JSON has unbalanced braces (truncated or corrupted).");
            }

            // Require schemaVersion field explicitly so arbitrary JSON objects are rejected as malformed saves
            if (trimmed.IndexOf("\"schemaVersion\"", StringComparison.Ordinal) < 0)
            {
                return new ProgressionLoadResult(
                    ProgressionLoadStatus.Corrupted,
                    null,
                    sourcePath,
                    isBackup,
                    0,
                    "Save file is missing required 'schemaVersion' field.");
            }

            SchemaProbe probe;
            try
            {
                probe = JsonUtility.FromJson<SchemaProbe>(trimmed);
            }
            catch (Exception ex)
            {
                return new ProgressionLoadResult(
                    ProgressionLoadStatus.Corrupted,
                    null,
                    sourcePath,
                    isBackup,
                    0,
                    $"JSON parse error: {ex.Message}");
            }

            if (probe == null)
            {
                return new ProgressionLoadResult(
                    ProgressionLoadStatus.Corrupted,
                    null,
                    sourcePath,
                    isBackup,
                    0,
                    "Deserialized save header is null.");
            }

            if (probe.schemaVersion < 1 || probe.schemaVersion > ProgressionSaveData.CurrentSchemaVersion)
            {
                return new ProgressionLoadResult(
                    ProgressionLoadStatus.UnsupportedVersion,
                    null,
                    sourcePath,
                    isBackup,
                    probe.schemaVersion,
                    $"Unsupported save schemaVersion {probe.schemaVersion} (supported: 1..{ProgressionSaveData.CurrentSchemaVersion}).");
            }

            ProgressionSaveData parsed;
            try
            {
                parsed = JsonUtility.FromJson<ProgressionSaveData>(trimmed);
            }
            catch (Exception ex)
            {
                return new ProgressionLoadResult(
                    ProgressionLoadStatus.Corrupted,
                    null,
                    sourcePath,
                    isBackup,
                    probe.schemaVersion,
                    $"JSON deserialization failed: {ex.Message}");
            }

            if (parsed == null || string.IsNullOrWhiteSpace(parsed.saveId))
            {
                return new ProgressionLoadResult(
                    ProgressionLoadStatus.Corrupted,
                    null,
                    sourcePath,
                    isBackup,
                    probe.schemaVersion,
                    "Save file is missing required 'saveId' identifier.");
            }

            parsed.Normalize();
            return new ProgressionLoadResult(
                ProgressionLoadStatus.Loaded,
                parsed,
                sourcePath,
                isBackup,
                parsed.schemaVersion,
                string.Empty);
        }

        private static bool HasBalancedJsonBraces(string json)
        {
            int depth = 0;
            bool inString = false;
            bool escape = false;
            bool seenRoot = false;

            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];
                if (inString)
                {
                    if (escape)
                    {
                        escape = false;
                    }
                    else if (c == '\\')
                    {
                        escape = true;
                    }
                    else if (c == '"')
                    {
                        inString = false;
                    }
                    continue;
                }

                if (c == '"')
                {
                    inString = true;
                }
                else if (c == '{')
                {
                    if (seenRoot && depth == 0) return false;
                    depth++;
                    seenRoot = true;
                }
                else if (c == '}')
                {
                    depth--;
                    if (depth < 0) return false;
                }
            }

            return !inString && seenRoot && depth == 0;
        }

        [Serializable]
        private class SchemaProbe
        {
            public int schemaVersion = 0;
        }
    }
}
