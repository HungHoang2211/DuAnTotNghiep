using System;
using System.IO;
using UnityEngine;

namespace SimpleSurvival.SaveLoad
{
    public sealed class SaveStorage
    {
        private const string FileName = "save.json";
        private const string TempSuffix = ".tmp";
        private const string BackupSuffix = ".bak";

        private readonly string filePath;
        private readonly string tempPath;
        private readonly string backupPath;

        public SaveStorage()
        {
            filePath = Path.Combine(Application.persistentDataPath, FileName);
            tempPath = filePath + TempSuffix;
            backupPath = filePath + BackupSuffix;
        }

        public bool Exists()
        {
            return File.Exists(filePath);
        }

        public bool Write(GameSave save)
        {
            try
            {
                string json = JsonUtility.ToJson(save, true);
                File.WriteAllText(tempPath, json);
                Commit();
                return true;
            }
            catch (Exception error)
            {
                Debug.LogError($"Save write failed: {error.Message}");
                return false;
            }
        }

        public GameSave Read()
        {
            GameSave save = TryReadFrom(filePath);
            if (save != null)
                return save;

            if (File.Exists(backupPath))
                Debug.LogWarning("Primary save unreadable, falling back to backup.");

            return TryReadFrom(backupPath);
        }

        public void Delete()
        {
            TryDelete(filePath);
            TryDelete(backupPath);
            TryDelete(tempPath);
        }

        private void Commit()
        {
            if (File.Exists(filePath))
                File.Replace(tempPath, filePath, backupPath);
            else
                File.Move(tempPath, filePath);
        }

        private GameSave TryReadFrom(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return null;

                string json = File.ReadAllText(path);
                return JsonUtility.FromJson<GameSave>(json);
            }
            catch (Exception error)
            {
                Debug.LogError($"Save read failed for {path}: {error.Message}");
                return null;
            }
        }

        private void TryDelete(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}