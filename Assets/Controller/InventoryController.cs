using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEditor.Progress;

public class InventoryController : MonoBehaviour
{

    [SerializeField] private Transform UI;
    [SerializeField] private List<Item> items;
    [SerializeField] private List<InventoryInitializer> intializeInventory;



    [SerializeField, HideInInspector]
    private List<InventoryInitializer> prevIntializeInventory;


    [SerializeField] private GameObject inventoryUIObject;

    [SerializeField, HideInInspector]
    private List<GameObject> allInventoryUI = new List<GameObject>();
    [SerializeField, HideInInspector]
    private SerializableDictionary<string, Inventory> InventoryManager= new SerializableDictionary<string, Inventory>();
    [SerializeField, HideInInspector]
    private Dictionary<string, Item> ItemManager = new Dictionary<string, Item>();
    public static InventoryController instance;


    [SerializeField] private bool isInstance;

    private void Awake()
    {
        if (isInstance)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        InitializeInventories();
    }
    public void InitializeInventories()
    {

        ItemManager.Clear();
        foreach (Item item in items)
        {
            ItemManager.Add(item.GetItemType(), item);
        }


        AllignDictionaries();
        List<GameObject> toremove= new List<GameObject>();
        foreach (InventoryInitializer initializer in prevIntializeInventory)
        {
            if(!intializeInventory.Contains(initializer))
            {
                foreach(GameObject UI in allInventoryUI)
                {
                    InventoryUI UIInstance = UI.GetComponent<InventoryUI>();
                    print(UIInstance.GetInventoryName());
                    if (UIInstance.GetInventoryName() == initializer.GetInventoryName())
                    {                        
                        toremove.Add(UI);
                        InventoryManager.Remove(UIInstance.GetInventoryName());
                    }
                }
            }
        }

        foreach (GameObject remove in toremove)
        {
            allInventoryUI.Remove(remove);
            DestroyImmediate(remove);

        }

        foreach (InventoryInitializer initializer in intializeInventory)
        {
            if (!prevIntializeInventory.Contains(initializer))
            {
                GameObject tempinventoryUI = Instantiate(inventoryUIObject, transform.position, Quaternion.identity, UI);

                allInventoryUI.Add(tempinventoryUI);
                InventoryUI inventoryUI = tempinventoryUI.GetComponent<InventoryUI>();
                string inventoryName = initializer.GetInventoryName();
                int InventorySize = initializer.GetRow() * initializer.GetCol();
                Inventory curInventory = new Inventory(inventoryName, InventorySize);
                curInventory.setManager(tempinventoryUI);
                InventoryManager.Add(inventoryName, curInventory);
                inventoryUI.SetInventory(ref curInventory);

                inventoryUI.SetRowCol(initializer.GetRow(), initializer.GetCol());
                inventoryUI.SetInventoryName(initializer.GetInventoryName());
                inventoryUI.UpdateInventoryDisplay();
            }
        }
        prevIntializeInventory.Clear();
        for (int i = 0; i < intializeInventory.Count; i++)
        {
            InventoryInitializer InitilizerCopy = new InventoryInitializer();
            InitilizerCopy.Copy(intializeInventory[i]);
            prevIntializeInventory.Add(InitilizerCopy);
        }
    }
    public Inventory GetInventory(string name)
    {
        return InventoryManager[name];
    }
    public void AddItem(string inventoryName, string itemType)
    {
        Inventory inventory = InventoryManager[inventoryName];
        Item item = ItemManager[itemType];
        inventory.AddItem(item);
    }
    public void ResetInventory()
    {
        InventoryManager.Clear();
        ItemManager.Clear();
        prevIntializeInventory.Clear();
        foreach (Item item in items)
        {
            ItemManager.Add(item.GetItemType(), item);
        }
        foreach (GameObject obj in allInventoryUI)
        {
            DestroyImmediate(obj);
        }
        allInventoryUI.Clear();
    }
    public void AllignDictionaries()
    {
        InventoryManager.Clear();
        foreach (GameObject inventories in allInventoryUI)
        {
            InventoryUI inventoryInstance = inventories.GetComponent<InventoryUI>();
            InventoryManager.Add(inventoryInstance.GetInventoryName(), inventoryInstance.GetInventory());
        }
    }

}