using System.Collections.Generic;
using UnityEngine;

public class FireManager : MonoBehaviour
{
    public static FireManager Instance;
    private List<Fire> allFires = new List<Fire>();
    private bool allExtinguished = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    public void RegisterFire(Fire fire)
    {
        if (!allFires.Contains(fire))
            allFires.Add(fire);
    }

    public void OnFireExtinguished(Fire fire)
    {
        // Check if all fires are out
        foreach (var f in allFires)
        {
            if (f == null) continue;
            if (!f.IsExtinguished())
                return; // still some burning
        }

        allExtinguished = true;
        Debug.Log("🔥 All fires extinguished! No new fire will start.");
    }

    public bool CanStartNewFire() => !allExtinguished;
}
