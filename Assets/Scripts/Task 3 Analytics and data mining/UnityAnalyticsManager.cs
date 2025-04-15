using System;
using System.Collections.Generic;
using Unity.Services.Core;
using Unity.Services.Analytics;
using UnityEngine;

public class UnityAnalyticsManager : MonoBehaviour
{
    public static UnityAnalyticsManager Instance { get; private set; }

    private async void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        await UnityServices.InitializeAsync();
        Debug.Log("Unity Gaming Services Initialized for Analytics");
    }

    public void LogMatchStart(string matchId)
    {
        var evt = new MatchStartEvent();
        evt.match_id = matchId;
        evt.timestamp = DateTime.UtcNow.ToString("o");

        // Record the custom event.
        AnalyticsService.Instance.RecordEvent(evt);
        Debug.Log("Recorded match start event with match_id: " + matchId);
    }

    public void LogMatchEnd(string matchId, string outcome)
    {
        var evt = new MatchEndEvent();
        evt.match_id = matchId;
        evt.outcome = outcome;
        evt.timestamp = DateTime.UtcNow.ToString("o");

        AnalyticsService.Instance.RecordEvent(evt);
        Debug.Log("Recorded match end event with match_id: " + matchId);
    }

    public void LogDLCPurchase(string itemId, float price)
    {
        var evt = new DLCPurchaseEvent();
        evt.item_id = itemId;
        evt.price = price;
        evt.timestamp = DateTime.UtcNow.ToString("o");

        AnalyticsService.Instance.RecordEvent(evt);
        Debug.Log("Recorded DLC purchase event for item: " + itemId);
    }
}

// Custom event classes
public class MatchStartEvent : Unity.Services.Analytics.Event
{
    public MatchStartEvent() : base("match_start") { }

    public string match_id { set { SetParameter("match_id", value); } }
    public string timestamp { set { SetParameter("timestamp", value); } }
}

public class MatchEndEvent : Unity.Services.Analytics.Event
{
    public MatchEndEvent() : base("match_end") { }

    public string match_id { set { SetParameter("match_id", value); } }
    public string outcome { set { SetParameter("outcome", value); } }
    public string timestamp { set { SetParameter("timestamp", value); } }
}

public class DLCPurchaseEvent : Unity.Services.Analytics.Event
{
    public DLCPurchaseEvent() : base("dlc_purchase") { }

    public string item_id { set { SetParameter("item_id", value); } }
    public float price { set { SetParameter("price", value); } }
    public string timestamp { set { SetParameter("timestamp", value); } }
}
