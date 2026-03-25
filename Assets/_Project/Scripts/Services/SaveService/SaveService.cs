using System;
using System.IO;
using Mergistry.Models;
using UnityEngine;

namespace Mergistry.Services
{
    /// <summary>
    /// Handles all JSON persistence:
    ///   - meta.json  (meta-progression, survives across runs)
    ///   - run.json   (mid-run save, deleted on run completion)
    ///
    /// Uses JsonUtility for simplicity + speed on mobile.
    /// Writes to Application.persistentDataPath.
    /// </summary>
    public class SaveService : ISaveService
    {
        private const string MetaFileName = "meta.json";
        private const string RunFileName  = "run.json";

        private readonly string _metaPath;
        private readonly string _runPath;

        public SaveService()
        {
            _metaPath = Path.Combine(Application.persistentDataPath, MetaFileName);
            _runPath  = Path.Combine(Application.persistentDataPath, RunFileName);
        }

        // ── ISaveService ─────────────────────────────────────────────────────

        public bool HasRunSave => File.Exists(_runPath);

        // ── Meta ──────────────────────────────────────────────────────────────

        public MetaProgressionModel LoadMeta()
        {
            if (!File.Exists(_metaPath))
                return new MetaProgressionModel();

            try
            {
                string json = File.ReadAllText(_metaPath);
                var    meta = JsonUtility.FromJson<MetaProgressionModel>(json);
                if (meta == null) return new MetaProgressionModel();
                return meta;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveService] Failed to load meta.json: {e.Message}. Starting fresh.");
                return new MetaProgressionModel();
            }
        }

        public void SaveMeta(MetaProgressionModel meta)
        {
            try
            {
                string json = JsonUtility.ToJson(meta, prettyPrint: true);
                File.WriteAllText(_metaPath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveService] Failed to save meta.json: {e.Message}");
            }
        }

        // ── Run ───────────────────────────────────────────────────────────────

        public RunSaveData LoadRun()
        {
            if (!File.Exists(_runPath)) return null;

            try
            {
                string      json = File.ReadAllText(_runPath);
                RunSaveData data = JsonUtility.FromJson<RunSaveData>(json);
                if (data == null)
                {
                    Debug.LogWarning("[SaveService] run.json was empty or corrupt. Deleting.");
                    DeleteRunSave();
                    return null;
                }
                return data;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveService] Failed to load run.json: {e.Message}. Deleting corrupt save.");
                DeleteRunSave();
                return null;
            }
        }

        public void SaveRun(RunSaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint: false);
                File.WriteAllText(_runPath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveService] Failed to save run.json: {e.Message}");
            }
        }

        public void DeleteRunSave()
        {
            try
            {
                if (File.Exists(_runPath))
                    File.Delete(_runPath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveService] Failed to delete run.json: {e.Message}");
            }
        }
    }
}
