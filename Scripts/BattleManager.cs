using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("Battle Settings")]
    public int maxTurns = 30;
    public float turnDuration = 30f;
    public float actionDelay = 0.5f;
    public int maxAnimalsPerPlayer = 5;

    [Header("References")]
    public BoardManager boardManager;
    public UIManager uiManager;
    public SummoningTower playerOneTower;
    public SummoningTower playerTwoTower;

    [System.Serializable]
    public class BattleEvents
    {
        public UnityEvent onBattleStart;
        public UnityEvent onBattleEnd;
        public UnityEvent<int> onTurnStart;
        public UnityEvent<int> onTurnEnd;
        public UnityEvent<Animal> onAnimalDeath;
        public UnityEvent<Animal> onAnimalSummoned;
    }
    public BattleEvents events;

    private int currentTurn = 1;
    private float currentTurnTime;
    private bool isBattleActive;
    private int currentPlayerTurn = 1; // 1 for player one, 2 for player two

    private Dictionary<int, List<Animal>> playerAnimals = new Dictionary<int, List<Animal>>();
    private Dictionary<int, int> playerEnergy = new Dictionary<int, int>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeBattle();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeBattle()
    {
        // Initialize player collections
        playerAnimals[1] = new List<Animal>();
        playerAnimals[2] = new List<Animal>();

        // Set initial energy
        playerEnergy[1] = 100;
        playerEnergy[2] = 100;

        // Subscribe to events
        if (playerOneTower != null)
            playerOneTower.onSummoningComplete.AddListener(OnAnimalSummoned);
        
        if (playerTwoTower != null)
            playerTwoTower.onSummoningComplete.AddListener(OnAnimalSummoned);
    }

    private void Update()
    {
        if (!isBattleActive) return;

        // Update turn timer
        if (currentTurnTime > 0)
        {
            currentTurnTime -= Time.deltaTime;
            uiManager.UpdateTurnTimer(currentTurnTime);

            if (currentTurnTime <= 0)
            {
                EndTurn();
            }
        }
    }

    #region Battle Flow

    public void StartBattle()
    {
        isBattleActive = true;
        currentTurn = 1;
        currentPlayerTurn = 1;
        
        // Reset battle state
        ResetBattleState();
        
        // Start first turn
        StartTurn();
        
        // Trigger battle start event
        events.onBattleStart?.Invoke();
    }

    private void StartTurn()
    {
        currentTurnTime = turnDuration;
        events.onTurnStart?.Invoke(currentTurn);
        
        // Regenerate energy for current player
        RegenerateEnergy(currentPlayerTurn);
        
        // Update UI
        uiManager.UpdateTurnDisplay(currentTurn, currentPlayerTurn);
        
        // Process start-of-turn effects
        ProcessTurnStartEffects();
    }

    private void EndTurn()
    {
        events.onTurnEnd?.Invoke(currentTurn);
        
        // Process end-of-turn effects
        ProcessTurnEndEffects();
        
        // Switch players
        currentPlayerTurn = currentPlayerTurn == 1 ? 2 : 1;
        
        // Check if we should start a new turn
        if (currentPlayerTurn == 1)
        {
            currentTurn++;
            if (currentTurn > maxTurns)
            {
                EndBattle(DetermineBattleWinner());
                return;
            }
        }
        
        StartTurn();
    }

    private void EndBattle(int winner)
    {
        isBattleActive = false;
        events.onBattleEnd?.Invoke();
        
        // Update UI with winner
        uiManager.ShowBattleResult(winner);
        
        // Save battle results
        SaveBattleResults(winner);
    }

    private void ResetBattleState()
    {
        // Clear all animals from the board
        foreach (var playerAnimalList in playerAnimals.Values)
        {
            foreach (var animal in playerAnimalList)
            {
                if (animal != null)
                    Destroy(animal.gameObject);
            }
            playerAnimalList.Clear();
        }

        // Reset energy
        playerEnergy[1] = 100;
        playerEnergy[2] = 100;

        // Reset towers
        if (playerOneTower != null)
            playerOneTower.ResetTower();
        
        if (playerTwoTower != null)
            playerTwoTower.ResetTower();

        // Clear board
        boardManager.ClearBoard();
    }

    #endregion

    #region Battle Mechanics

    public bool CanSummonAnimal(int playerId, AnimalData animalData)
    {
        if (!isBattleActive || currentPlayerTurn != playerId)
            return false;

        // Check energy cost
        if (playerEnergy[playerId] < GetSummonCost(animalData.rank))
            return false;

        // Check animal limit
        if (playerAnimals[playerId].Count >= maxAnimalsPerPlayer)
            return false;

        // Check rank restrictions
        if (!GameManager.Instance.CanSummonAnimal(animalData.rank))
            return false;

        return true;
    }

    public void SummonAnimal(int playerId, AnimalData animalData, Vector2Int position)
    {
        if (!CanSummonAnimal(playerId, animalData))
            return;

        // Deduct energy
        playerEnergy[playerId] -= GetSummonCost(animalData.rank);

        // Get appropriate tower
        SummoningTower tower = playerId == 1 ? playerOneTower : playerTwoTower;
        
        // Start summoning process
        if (tower != null)
            tower.StartSummoning(animalData, boardManager.GridToWorldPosition(position));
    }

    private void OnAnimalSummoned(Animal animal)
    {
        int playerId = animal.transform.position.y > 0 ? 1 : 2;
        playerAnimals[playerId].Add(animal);
        events.onAnimalSummoned?.Invoke(animal);
    }

    public void ProcessAnimalDeath(Animal animal)
    {
        int playerId = animal.transform.position.y > 0 ? 1 : 2;
        playerAnimals[playerId].Remove(animal);
        events.onAnimalDeath?.Invoke(animal);

        // Check for battle end condition
        if (playerAnimals[playerId].Count == 0)
        {
            EndBattle(playerId == 1 ? 2 : 1);
        }
    }

    private void RegenerateEnergy(int playerId)
    {
        int regenerationAmount = 20; // Base energy regeneration per turn
        playerEnergy[playerId] = Mathf.Min(playerEnergy[playerId] + regenerationAmount, 100);
        uiManager.UpdateEnergyDisplay(playerId, playerEnergy[playerId]);
    }

    private void ProcessTurnStartEffects()
    {
        foreach (var animal in playerAnimals[currentPlayerTurn])
        {
            animal.OnTurnStart();
        }
    }

    private void ProcessTurnEndEffects()
    {
        foreach (var animal in playerAnimals[currentPlayerTurn])
        {
            animal.OnTurnEnd();
        }
    }

    private int DetermineBattleWinner()
    {
        // Calculate score based on remaining animals and their health
        int player1Score = CalculatePlayerScore(1);
        int player2Score = CalculatePlayerScore(2);

        if (player1Score > player2Score)
            return 1;
        else if (player2Score > player1Score)
            return 2;
        else
            return 0; // Draw
    }

    private int CalculatePlayerScore(int playerId)
    {
        int score = 0;
        foreach (var animal in playerAnimals[playerId])
        {
            score += animal.GetCurrentHealth();
            score += (int)animal.stats.rank * 100; // Bonus points for higher rank animals
        }
        return score;
    }

    private int GetSummonCost(AnimalRank rank)
    {
        switch (rank)
        {
            case AnimalRank.SSS:
                return 100;
            case AnimalRank.SS:
                return 75;
            case AnimalRank.S:
                return 50;
            case AnimalRank.A:
                return 25;
            default:
                return 10;
        }
    }

    private void SaveBattleResults(int winner)
    {
        // Save battle statistics
        SaveManager.Instance.AddBattleResult(new SaveManager.BattleResult
        {
            winner = winner,
            turns = currentTurn,
            player1Score = CalculatePlayerScore(1),
            player2Score = CalculatePlayerScore(2),
            timestamp = System.DateTime.Now
        });
    }

    #endregion

    #region Public Utility Methods

    public bool IsPlayerTurn(int playerId)
    {
        return isBattleActive && currentPlayerTurn == playerId;
    }

    public int GetCurrentTurn()
    {
        return currentTurn;
    }

    public float GetRemainingTurnTime()
    {
        return currentTurnTime;
    }

    public int GetPlayerEnergy(int playerId)
    {
        return playerEnergy.ContainsKey(playerId) ? playerEnergy[playerId] : 0;
    }

    public List<Animal> GetPlayerAnimals(int playerId)
    {
        return playerAnimals.ContainsKey(playerId) ? playerAnimals[playerId] : new List<Animal>();
    }

    public void ForfeitBattle(int playerId)
    {
        EndBattle(playerId == 1 ? 2 : 1);
    }

    #endregion
}
