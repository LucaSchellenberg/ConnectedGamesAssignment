using Firebase.Analytics;
using System;
using UnityEngine;

public class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void LogMatchStart(string matchId)
    {
        FirebaseAnalytics.LogEvent("match_start",
            new Parameter("match_id", matchId),
            new Parameter("timestamp", DateTime.UtcNow.ToString("o")));
        Debug.Log("Logged match start event with match_id: " + matchId);
    }

    public void LogMatchEnd(string matchId, string outcome)
    {
        FirebaseAnalytics.LogEvent("match_end",
            new Parameter("match_id", matchId),
            new Parameter("outcome", outcome),
            new Parameter("timestamp", DateTime.UtcNow.ToString("o")));
        Debug.Log("Logged match end event with match_id: " + matchId);
    }

    public void LogDLCPurchase(string itemId, float price)
    {
        FirebaseAnalytics.LogEvent("dlc_purchase",
            new Parameter("item_id", itemId),
            new Parameter("price", price),
            new Parameter("timestamp", DateTime.UtcNow.ToString("o")));
        Debug.Log("Logged DLC purchase event for item_id: " + itemId);
    }
}
