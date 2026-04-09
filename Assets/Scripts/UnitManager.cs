using System;
using System.Collections.Generic;
using UnityEngine;

public class UnitManager : MonoBehaviour
{
    public static UnitManager Instance { get; private set; }
    
    private List<Unit> unitList;
    private List<Unit> friendlyUnitList;
    private List<Unit> EnemyUnitList;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There is already a UnitManager in the scene!");
            Destroy(gameObject);
        }
        Instance = this;
        
        unitList = new List<Unit>();
        friendlyUnitList = new List<Unit>();
        EnemyUnitList = new List<Unit>();
    }

    private void Start()
    {
        Unit.OnAnyUnitSpawned += Unit_OnAnyUnitSpawned;
        Unit.OnAnyUnitDead += Unit_OnAnyUnitDead;

    }

    private void Unit_OnAnyUnitSpawned(object sender, EventArgs e)
    {
        Unit unit = sender as Unit;
        
        unitList.Add(unit);
        
        if (unit.IsEnemy())
        {
            EnemyUnitList.Add(unit);
        }
        else
        {
            friendlyUnitList.Add(unit);
        }
    }

    private void Unit_OnAnyUnitDead(object sender, EventArgs e)
    {
        Unit unit = sender as Unit;
        
        unitList.Remove(unit);

        if (unit.IsEnemy())
        {
            EnemyUnitList.Remove(unit);
        }
        else
        {
            friendlyUnitList.Remove(unit);
        }
    }
    
    public List<Unit> GetUnitList()
    {
        return unitList;
    }
    
    public List<Unit> GetFriendlyUnitList()
    {
        return friendlyUnitList;
    }
    
    public List<Unit> GetEnemyUnitList()
    {
        return EnemyUnitList;
    }
}
