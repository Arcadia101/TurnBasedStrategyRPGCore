using System;
using UnityEngine;

[Serializable]
public class RuntimeGesture
{
    [SerializeField] private GestureData gestureData;
    [SerializeField] private int currentUses;

    public RuntimeGesture(GestureData data)
    {
        gestureData = data;
        currentUses = data != null ? data.GetMaxUses() : 0;
    }

    public GestureData GetData() => gestureData;
    public int GetCurrentUses() => currentUses;
    public bool HasUses() => currentUses > 0;

    public void ConsumeUse()
    {
        currentUses = Mathf.Max(0, currentUses - 1);
    }

    public void ResetUses()
    {
        if (gestureData != null)
        {
            currentUses = gestureData.GetMaxUses();
        }
    }
}