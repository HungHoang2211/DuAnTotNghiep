using System;
using System.Collections;
using SimpleSurvival.Building;
using SimpleSurvival.Quests;
using SimpleSurvival.SaveLoad;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SimpleSurvival.World
{
    public class MapLoader : MonoBehaviour
    {
        public static MapLoader Instance { get; private set; }

        [SerializeField] private Transform player;

        public event Action PlayerRepositioned;

        private string currentMapScene;

        public string CurrentMapScene => currentMapScene;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }
        public void RepositionToSpawn()
        {
            RepositionPlayerToSpawn();
        }
        public IEnumerator SwapRoutine(string mapScene)
        {
            if (!string.IsNullOrEmpty(currentMapScene))
            {
                yield return SceneManager.UnloadSceneAsync(currentMapScene);
            }

            yield return SceneManager.LoadSceneAsync(mapScene, LoadSceneMode.Additive);

            Scene loaded = SceneManager.GetSceneByName(mapScene);
            SceneManager.SetActiveScene(loaded);
            currentMapScene = mapScene;

            CorpseSaveRegistry.Instance?.RestoreForMap(mapScene);
            BuildModeController.Instance?.RestoreForMap(mapScene);

            RepositionPlayerToSpawn();
            SaveService.Instance?.Save();
        }
        private void RepositionPlayerToSpawn()
        {
            if (player == null) return;

            MapSpawnPoint spawn = FindSpawnPoint();

            if (spawn == null)
            {
                Debug.LogError($"[MapLoader] KHÔNG tìm thấy MapSpawnPoint trong scene '{currentMapScene}'!");
                return;
            }

            Debug.Log($"[MapLoader] Reposition -> {spawn.name} (id: {spawn.SpawnPointId}) tại {spawn.transform.position}, scene hiện tại: {currentMapScene}");

            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;

            player.SetPositionAndRotation(spawn.transform.position, spawn.transform.rotation);

            if (controller != null) controller.enabled = true;

            PlayerRepositioned?.Invoke();
        }

        private MapSpawnPoint FindSpawnPoint()
        {
            MapSpawnPoint[] spawnPoints = FindObjectsByType<MapSpawnPoint>(FindObjectsSortMode.None);
            if (spawnPoints.Length == 0) return null;

            string overrideId = QuestManager.Instance != null
                ? QuestManager.Instance.GetSpawnOverride(currentMapScene)
                : null;

            if (!string.IsNullOrEmpty(overrideId))
            {
                foreach (MapSpawnPoint sp in spawnPoints)
                {
                    if (sp.SpawnPointId == overrideId) return sp;
                }

                Debug.LogWarning($"[MapLoader] Không tìm thấy spawn point ID '{overrideId}' trong scene '{currentMapScene}', dùng spawn mặc định.");
            }

            foreach (MapSpawnPoint sp in spawnPoints)
            {
                if (sp.SpawnPointId == "Default") return sp;
            }

            return spawnPoints[0];
        }
    }
}