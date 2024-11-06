using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour, ISaveManager
{
    public static GameManager instance;

    private Transform player;

    [SerializeField] private Checkpoint[] checkpoints;
    [SerializeField] private string closestCheckpointId;

    [Header("Lost currency")]
    [SerializeField] private GameObject lostCurrencyPrefab;
    public int lostCurrencyAmount;
    [SerializeField] private float lostCurrencyX;
    [SerializeField] private float lostCurrencyY;
    private bool pausedGame;

    [Header("Level Management")]
    [SerializeField] private GameObject[] enemyPrefabs; // Array to hold different enemy prefabs
    [SerializeField] private GameObject bossPrefab; // Prefab for the boss
    private int currentLevel = 1;
    private int enemiesToSpawn = 5;
    private float mapSize = 50f; // Size of the map
    private bool isLevelInProgress = false; // Flag to ensure enemies are not spawned continuously

    private void Awake()
    {
        if (instance != null)
            Destroy(instance.gameObject);
        else
            instance = this;
    }

    private void Start()
    {
        checkpoints = FindObjectsOfType<Checkpoint>();
        player = PlayerManager.instance.player.transform;
        GenerateNewLevel();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
            RestartScene();

        if (Input.GetKeyDown(KeyCode.G))
        {
            pausedGame = !pausedGame;
            PauseGame(pausedGame);
        }

        // Check if level is in progress and if there are no enemies or bosses left
        if (!isLevelInProgress && GameObject.FindGameObjectsWithTag("Enemy").Length == 0 && GameObject.FindGameObjectsWithTag("Boss").Length == 0)
        {
            isLevelInProgress = true;
            currentLevel++;
            enemiesToSpawn += 5; // Increase the number of enemies to spawn for each new level
            GenerateNewLevel();
        }
    }

    public void RestartScene()
    {
        SaveManager.instance.SaveGame();
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }

    public void LoadData(GameData _data) => StartCoroutine(LoadWithDelay(_data));

    private void LoadCheckpoints(GameData _data)
    {
        foreach (KeyValuePair<string, bool> pair in _data.checkpoints)
        {
            foreach (Checkpoint checkpoint in checkpoints)
            {
                if (checkpoint.id == pair.Key && pair.Value == true)
                    checkpoint.ActivateCheckpoint();
            }
        }
    }

    private void LoadLostCurrency(GameData _data)
    {
        lostCurrencyAmount = _data.lostCurrencyAmount;
        lostCurrencyX = _data.lostCurrencyX;
        lostCurrencyY = _data.lostCurrencyY;

        if (lostCurrencyAmount > 0)
        {
            GameObject newLostCurrency = Instantiate(lostCurrencyPrefab, new Vector3(lostCurrencyX, lostCurrencyY), Quaternion.identity);
            newLostCurrency.GetComponent<LostCurrencyController>().currency = lostCurrencyAmount;
        }

        lostCurrencyAmount = 0;
    }

    private IEnumerator LoadWithDelay(GameData _data)
    {
        yield return new WaitForSeconds(.1f);

        LoadCheckpoints(_data);
        LoadClosestCheckpoint(_data);
        LoadLostCurrency(_data);
    }

    public void SaveData(ref GameData _data)
    {
        _data.lostCurrencyAmount = lostCurrencyAmount;
        _data.lostCurrencyX = player.position.x;
        _data.lostCurrencyY = player.position.y;

        if (FindClosestCheckpoint() != null)
            _data.closestCheckpointId = FindClosestCheckpoint().id;

        _data.checkpoints.Clear();

        foreach (Checkpoint checkpoint in checkpoints)
        {
            _data.checkpoints.Add(checkpoint.id, checkpoint.activationStatus);
        }
    }

    private void LoadClosestCheckpoint(GameData _data)
    {
        if (_data.closestCheckpointId == null)
            return;

        closestCheckpointId = _data.closestCheckpointId;

        foreach (Checkpoint checkpoint in checkpoints)
        {
            if (closestCheckpointId == checkpoint.id)
                player.position = checkpoint.transform.position;
        }
    }

    private Checkpoint FindClosestCheckpoint()
    {
        float closestDistance = Mathf.Infinity;
        Checkpoint closestCheckpoint = null;

        foreach (var checkpoint in checkpoints)
        {
            float distanceToCheckpoint = Vector2.Distance(player.position, checkpoint.transform.position);

            if (distanceToCheckpoint < closestDistance && checkpoint.activationStatus == true)
            {
                closestDistance = distanceToCheckpoint;
                closestCheckpoint = checkpoint;
            }
        }

        return closestCheckpoint;
    }

    public void PauseGame(bool _pause)
    {
        Time.timeScale = _pause ? 0 : 1;
    }

    private void GenerateNewLevel()
    {
        ClearEnemiesAndBoss();

        if (currentLevel % 2 == 0)
        {
            // Spawn Boss Level
            for (int i = 0; i < currentLevel / 2; i++)
            {
                SpawnBoss();
            }
        }
        else
        {
            // Spawn Regular Enemies
            for (int i = 0; i < enemiesToSpawn; i++)
            {
                SpawnEnemy();
            }
        }

        isLevelInProgress = false; // Mark level as in progress after enemies are generated
    }

    private void SpawnEnemy()
    {
        int randomIndex = Random.Range(0, enemyPrefabs.Length);
        Vector3 randomPosition = new Vector3(Random.Range(-mapSize / 2, mapSize / 2), 0, 0);
        GameObject newEnemy = Instantiate(enemyPrefabs[randomIndex], randomPosition, Quaternion.identity);
        newEnemy.tag = "Enemy"; // Assign the "Enemy" tag to the newly created Enemy object
    }

    private void SpawnBoss()
    {
        Vector3 bossPosition = new Vector3(Random.Range(-mapSize / 2, mapSize / 2), 0, 0); // Define a suitable boss position
        GameObject newBoss = Instantiate(bossPrefab, bossPosition, Quaternion.identity);
        newBoss.tag = "Boss";
    }

    private void ClearEnemiesAndBoss()
    {
        foreach (GameObject enemy in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            Destroy(enemy);
        }
        foreach (GameObject boss in GameObject.FindGameObjectsWithTag("Boss"))
        {
            Destroy(boss);
        }
    }
}