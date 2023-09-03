
using System.Collections.Generic;
using System.Text;
using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;
using UnityEngine.Events;
using static UnityEditor.PlayerSettings;
//Author: Jaxon Schauer
/// <summary>
/// This class creates an Inventory that tracks and controls the inventory list. This class tells the InventoryUIManager what objects each slot holds
/// </summary>


[System.Serializable]
public class Inventory 
{
    private Dictionary<string, List<int>> itemPositions;//Holds all positions of a given itemType in the list

    private List<InventoryItem> inventoryList;//Holds all inventory items in a list

    [SerializeField, HideInInspector]
    private string inventoryName;
    [SerializeField, HideInInspector]
    private GameObject InventoryUIManager;//holds the linked InventoryUIManager GameObject
    [SerializeField, HideInInspector]
    InventoryUIManager InventoryUIManagerInstance;//Holds an instance of the linked InventoryUIManager class
    [SerializeField, HideInInspector]
    int size;
    [SerializeField, HideInInspector]
    bool saveInventory;//is true if the user decides to save the inventory
    private bool clickItemOnEnter;
    private bool acceptAll;
    private bool rejectAll;
    private HashSet<string> exceptions;
    private Dictionary<int, UnityEvent> enterDict;
    private Dictionary<int, UnityEvent> exitDict;
    private Dictionary<int, bool> itemAction;


    /// <summary>
    /// Assigns essential variables for the Inventory
    /// </summary>
    public Inventory(GameObject InventoryUIManager,string name,int size)
    {
        this.InventoryUIManager = InventoryUIManager;
        this.inventoryName = name;
        inventoryList = new List<InventoryItem>(size);
        this.size = size;
        FillInventory(size);
        InventoryUIManagerInstance = InventoryUIManager.GetComponent<InventoryUIManager>();
    }
    /// <summary>
    /// Initializes aspects of the inventory that do not transfer into play mode.
    /// </summary>
    public void Init()
    {
        exceptions = new HashSet<string>();
        itemPositions = new Dictionary<string, List<int>>();
    }
    public void InitList()
    {
        inventoryList = new List<InventoryItem>(size);
        FillInventory(size);
    }
    /// <summary>
    /// Resizes the inventory when <see cref="InventoryUIManager.UpdateInventoryUI"/> is called
    /// </summary>
    public void Resize(int newSize)
    {
        if(inventoryList != null)
        {
            itemPositions = new Dictionary<string, List<int>>();
            List<InventoryItem> newlist = new List<InventoryItem>();

            if (size < newSize)
            {
                for (int i = 0; i < inventoryList.Count; i++)
                {
                    InventoryItem item = inventoryList[i];
                    newlist.Add(item);
                    AddItemPosDict(item, i,false);

                }
                for (int i = newlist.Count; i < newSize; i++)
                {
                    InventoryItem filler = new InventoryItem(true);
                    newlist.Add(filler);
                }
            }
            else
            {
                for (int i = 0; i < newSize; i++)
                {
                    InventoryItem item = inventoryList[i];
                    newlist.Add(item);
                    AddItemPosDict(item, i,false);

                }
            }
            inventoryList.Clear();

            inventoryList = newlist;

        }
        size = newSize;
    }
    public Dictionary<string, List<int>> TestPrintItemPosDict()
    {
        StringBuilder output = new StringBuilder();

        output.Append(itemPositions.Count + " | ");

        foreach (KeyValuePair<string, List<int>> pair in itemPositions)
        {
            output.Append(pair.Key + ": ");
            foreach (int position in pair.Value)
            {
                output.Append(position + " ");
            }
            output.Append("| ");
        }

        Debug.Log(output.ToString());
        return itemPositions;
    }
    /// <summary>
    /// Adds an item to a specified position, updating the <see cref="itemPositions"/> for efficient tracking of the items
    /// </summary>
    public void AddItemPos(int index,InventoryItem item)
    {
        if (inventoryList == null)
        {
            Debug.LogError("Items List Null");
            return;
        }
        else if (index > size - 1)
        {
            Debug.LogWarning("Out of Range Adding to closest Index: " + index);
            index = size - 1;
        }
        else if (index < 0)
        {
            Debug.LogWarning("Out of Range Adding to Closest Index: " + index);
            index = 0;
        }
        if(!CheckAcceptance(item.GetItemType()))
        {
            Debug.LogWarning("Item Acceptance is false. Overruling and adding item.");
        }
        InventoryItem newItem = new InventoryItem(item, item.GetAmount());
        InventoryItem curItem = inventoryList[index];
        newItem.SetPosition(index);
        newItem.SetInventory(inventoryName);
        if (curItem.GetIsNull())
        {
            inventoryList[index] = newItem;
            AddItemPosDict(newItem, index);

            InventoryUIManagerInstance.UpdateSlot(index);
        }
        else
        {
            if(curItem.GetItemType() == newItem.GetItemType())
            {
                if(curItem.GetAmount() + newItem.GetAmount() < curItem.GetItemStackAmount())
                {
                    curItem.SetAmount(curItem.GetAmount() + newItem.GetAmount());
                    InventoryUIManagerInstance.UpdateSlot(index);
                }
            }
        }
    }
    /// <summary>
    /// Takes an item as input
    /// Adds the item at the lowest possible inventory location, adding it into the <see cref="itemPositions"/> to allow for efficient tracking of the inventory items
    /// </summary>
    public void AddItemLinearly(InventoryItem item, int amount = 1)
    {
        if (!CheckAcceptance(item.GetItemType()))
        {
            Debug.LogWarning("Item Acceptance is false. Overruling and adding item.");
        }
        if (itemPositions.ContainsKey(item.GetItemType()))
        {
            for (int i = 0; i < itemPositions[item.GetItemType()].Count; i++)
            {
                int position = itemPositions[item.GetItemType()][i];
                if (inventoryList[position].GetItemStackAmount() > inventoryList[position].GetAmount())
                {
                    inventoryList[position].SetAmount(inventoryList[position].GetAmount() + item.GetAmount());
                    InventoryUIManager.GetComponent<InventoryUIManager>().UpdateSlot(position);
                    return;
                }
            }
            AddLinearly(item, amount);
        }
        else
        {
            AddLinearly(item, amount);
        }

    }
    /// <summary>
    /// Adds a new item in the lowest possible inventoryList position
    /// </summary>
    private void AddLinearly(InventoryItem item, int amount = 1)
    {
        for (int i = 0; i < inventoryList.Count; i++)
        {

            if (inventoryList[i].GetIsNull())
            {
                InventoryItem newItem = new InventoryItem(item, amount);
                newItem.SetPosition(i);
                newItem.SetInventory(inventoryName);
                inventoryList[i] = newItem;
                AddItemPosDict(newItem, i);
                break;
            }
        }
    }
    /// <summary>
    /// Adds itemstypes into <see cref="itemPositions"/> and tracks their positions for quick add/remove and count functions.
    /// </summary>
    private void AddItemPosDict(InventoryItem item, int pos, bool invokeEnterExit = true)
    {
        if (!item.GetIsNull())
        {
            if (!itemPositions.ContainsKey(item.GetItemType()))
            {
                itemPositions.Add(item.GetItemType(), new List<int>() { pos });
                InventoryUIManagerInstance.UpdateSlot(pos);
            }
            else
            {
                itemPositions[item.GetItemType()].Add(pos);
                InventoryUIManagerInstance.UpdateSlot(pos);

            }
            if (invokeEnterExit&&enterDict!= null && enterDict.ContainsKey(pos))
            {
                if (itemAction[pos])
                {
                    item.Selected();
                }
                enterDict[pos].Invoke();
            }

        }
    }


    /// <summary>
    /// Takes as input a position, remove the item from the given inventory position.
    /// </summary>
    public void RemoveItemInPosition(int pos, bool invokeEnterExit = true)
    {
        if (!inventoryList[pos].GetIsNull())
        {
            if (itemPositions.ContainsKey(inventoryList[pos].GetItemType()))
            {
                itemPositions[inventoryList[pos].GetItemType()].Remove(pos);
            }
            else
            {
                Debug.LogWarning("ItemPositions Dictitonary Setup Incorrectly");
            }
        }
        InventoryItem filler = new InventoryItem(true);
        inventoryList[pos] = filler;
        InventoryUIManagerInstance.UpdateSlot(pos);
        if (invokeEnterExit && exitDict != null && exitDict.ContainsKey(pos))
        {
            exitDict[pos].Invoke();
        }
    }
    public void RemoveItemInPosition(int pos, int amount)
    {
        InventoryItem item = inventoryList[pos];
        if (!item.GetIsNull())
        {
            if (itemPositions.ContainsKey(item.GetItemType()))
            {
                if(item.GetAmount() - amount > 0)
                {
                    item.SetAmount(item.GetAmount() - amount);
                    InventoryUIManagerInstance.UpdateSlot(pos);

                }
                else
                {
                    itemPositions[item.GetItemType()].Remove(pos);
                    InventoryItem filler = new InventoryItem(true);
                    inventoryList[pos] = filler;
                    InventoryUIManagerInstance.UpdateSlot(pos);
                }
                if (exitDict != null && exitDict.ContainsKey(pos))
                {
                    exitDict[pos].Invoke();
                    InventoryUIManagerInstance.UpdateSlot(pos);
                }
            }
            else
            {
                Debug.LogWarning("ItemPositions Dictitonary Setup Incorrectly");
            }
        }
    }
    public void RemoveItemInPosition(InventoryItem item, int amount)
    {
        int position = item.GetPosition();
        if (!item.GetIsNull())
        {
            if (itemPositions.ContainsKey(item.GetItemType()))
            {
                if(item.GetAmount() - amount > 0)
                {
                    item.SetAmount(item.GetAmount() - amount);
                }
                else
                {
                    itemPositions[item.GetItemType()].Remove(position);
                    InventoryItem filler = new InventoryItem(true);
                    inventoryList[position] = filler;
                }
                InventoryUIManagerInstance.UpdateSlot(position);
            }
            else
            {
                Debug.LogWarning("ItemPositions Dictitonary Setup Incorrectly");
            }
        }
    }
    public int Count(string itemType)
    {
        if(itemType == null)
        {
            Debug.LogError("String null. Returning 0");
            return 0;
        }
        if(!itemPositions.ContainsKey(itemType))
        {
            Debug.LogError("ItemPositions doesn contain itemType: " + itemType + ". Returning 0");
            return 0;
        }
        List<int> items = itemPositions[itemType];
        int itemsTotal = 0;
        foreach(int item in items)
        {
            itemsTotal+= inventoryList[item].GetAmount();  
        }
        return itemsTotal;
    }
    /// <summary>
    /// Fills the inventory with empty items 
    /// </summary>
    public void FillInventory(int size, bool highlightable = false)
    {
        if (inventoryList == null)
        {
            return;
        }
        for (int i = 0; i < size; i ++)
        {
            InventoryItem filler = new InventoryItem(true);
            filler.SetHighlightable(highlightable);
            inventoryList.Add(filler);
        }
    }
    /// <summary>
    /// Returns the item at a specific index of the inventory, returning the closest value if out of range
    /// </summary>
    public InventoryItem InventoryGetItem(int index)
    {
        if (inventoryList == null)
        {
            Debug.LogError("Items List Null");
            return null;
        }
        else if(index > size -1)
        {
            Debug.LogWarning("Out of Range Returning Closest Item: " + index);
            return inventoryList[size-1];
        }
        else if(index < 0)
        {
            Debug.LogWarning("Out of Range Returning Closest Item: " + index);
            return inventoryList[0];
        }
        return inventoryList[index];
    }
    /// <summary>
    /// Sets up values for the inventory to determine if an item should be accepted or rejected from the inventory
    /// </summary>
    public void SetupItemAcceptance(bool acceptAll, bool rejectAll, List<string>exceptions)
    {
        if(acceptAll && !rejectAll)
        {
            this.acceptAll= true;
            this.rejectAll= false;
        }
        else if(rejectAll && !acceptAll)
        {
            this.acceptAll = false;
            this.rejectAll = true;
        }
        else
        {
            Debug.LogError("Only one AcceptAll or RejectAll should Be True And False");
        }
        foreach (string exception in exceptions)
        {
            if (!this.exceptions.Contains(exception))
            {
                this.exceptions.Add(exception);
            }
            else
            {
                Debug.LogWarning("No Duplicate Items Should Exist In Exception List");
            }
        }
    }
    /// <summary>
    /// Returns a bool, true if an item can be transfered into an inventory and false otherwise.
    /// </summary>
    public bool CheckAcceptance(string itemType)
    {
        if((acceptAll && rejectAll) || (!acceptAll&&!rejectAll))
        {
            Debug.LogWarning("Acceptance Incorrectly Setup, Returning True Or False For All, and should only be for one. Return True for All.");
            return true;
        }
        if(acceptAll && !exceptions.Contains(itemType))
        {
            return true;
        }
        else if(rejectAll && exceptions.Contains(itemType))
        {
            return true;
        }
        return false;
    }
    public void SetSave(bool saveable)
    {
        this.saveInventory= saveable;
    }
    public string GetName()
    {
        return inventoryName;
    }
    public void SetManager(GameObject manager)
    {
        this.InventoryUIManager= manager;
    }
    public List<InventoryItem> GetList()
    {
        return inventoryList;
    }
    public bool GetSaveInventory()
    {
        return saveInventory;
    }
    public void SetclickItemOnEnter(bool clickItemOnEnter)
    {
        this.clickItemOnEnter = clickItemOnEnter;
    }
    public void SetExitEntranceDict(Dictionary<int, UnityEvent> enterDict, Dictionary<int, UnityEvent> exitDict, Dictionary<int, bool> itemAction)
    {
        this.enterDict = enterDict;
        this.exitDict = exitDict;
        this.itemAction = itemAction;
    }
}