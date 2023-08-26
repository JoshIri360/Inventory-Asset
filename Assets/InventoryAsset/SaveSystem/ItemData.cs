using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class ItemData
{
    public int amount;
    public int position;
    public string name;
    public ItemData(int amount, string name, int position)
    {
        this.amount = amount;
        this.name = name;
        this.position = position;
    }
}
