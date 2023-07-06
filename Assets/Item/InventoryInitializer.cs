using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class InventoryInitializer
{
    [SerializeField] private string inventoryName;
    [SerializeField] private int row;
    [SerializeField] private int col;

    public string GetInventoryName()
    {
        return inventoryName;
    }
    public int GetRow()
    {
        return row;
    }
    public int GetCol()
    {
        return col;
    }
    public void SetRow(int row)
    {
        this.row = row;
    }
    public void SetCol(int col)
    {
        this.col = col;
    }
    public void SetInventoryName(string inventoryName)
    {
        this.inventoryName = inventoryName;
    }


    public override int GetHashCode()
    {
        return HashCode.Combine(inventoryName, row, col);
    }

    public override bool Equals(object obj)
    {
        return obj is InventoryInitializer initializer &&
               inventoryName == initializer.inventoryName &&
               row == initializer.row &&
               col == initializer.col;
    }
    public void Copy(InventoryInitializer initilizer)
    {
        inventoryName = initilizer.inventoryName;  
        row = initilizer.row;
        col = initilizer.col;
    }
}