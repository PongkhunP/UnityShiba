using UnityEngine;

public enum GameState { Playing, InMenu, Pause }
public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Singleton { get; private set; }
    public GameState CurrentState { get; private set; }
    public ItemDatabases itemDatabases;
    public void Start()
    {
        if (Singleton == null)
        {
            Singleton = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetGameState(GameState gameState)
    {
        CurrentState = gameState;
    }
    public void SaveGame()
    {
        // Implement your save logic here
        Debug.Log("Game saved!");
    }

}
