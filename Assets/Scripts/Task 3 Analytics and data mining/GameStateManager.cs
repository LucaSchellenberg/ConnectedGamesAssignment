using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    private DatabaseReference dbRef;

    private void Start()
    {
        // Ensure Firebase is initialized (this happens automatically if you added google-services.json)
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
    }

    public void SaveGameState(string userId, string gameState)
    {
        // Save the gameState string (e.g., a FEN or PGN string)
        dbRef.Child("gameStates").Child(userId).SetValueAsync(gameState).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("Game state saved successfully.");
            }
            else
            {
                Debug.LogError("Error saving game state: " + task.Exception);
            }
        });
    }
    public void LoadGameState(string userId)
    {
        dbRef.Child("gameStates").Child(userId).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                if (snapshot.Exists)
                {
                    string gameState = snapshot.Value.ToString();
                    // Ensure that you call GameManager.Instance.LoadGame on the main thread.
                    GameManager.Instance.LoadGame(gameState);
                }
                else
                {
                    Debug.LogWarning("No saved game state found for user: " + userId);
                }
            }
            else
            {
                Debug.LogError("Error loading game state: " + task.Exception);
            }
        });
    }

}
