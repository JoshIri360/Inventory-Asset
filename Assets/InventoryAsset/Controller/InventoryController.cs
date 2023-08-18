using System.Collections.Generic;
using UnityEngine;


/*
 * This class defines an inventory controller, which allows for creating new inventories and defining valid types of objects.
 * Only one InventoryController should be instantiated within a project. Multiple inventories can be created from one controller.
 * This controller manages all information transfered between the inventories.
 */

public class InventoryController : MonoBehaviour
{
    [SerializeField]
    private Transform UI; // UI canvas to build inventories on.
    [SerializeField]
    public List<ItemInitializer> items; // Accepted items that can be added by name to the inventory.
    [Header("Once Initialized Edit Inventory Under UI.")]
    [SerializeField]
    private List<InventoryInitializer> initializeInventory; // Information about the inventory specified through the manager.

    [SerializeField, HideInInspector]
    private List<InventoryInitializer> prevInventoryTracker; // Previously initialized inventories, so they are not initialized again.


    [SerializeField]
    private GameObject inventoryManagerObj; // Prefab for the inventory manager.

    [SerializeField, HideInInspector]
    private List<GameObject> allInventoryUI = new List<GameObject>(); // Holds all inventory UI instances for each inventory created.
    private Dictionary<string, Inventory> inventoryManager = new Dictionary<string, Inventory>(); // Dictionary to map inventory names to their objects.
    private Dictionary<string, Item> itemManager = new Dictionary<string, Item>(); // Dictionary to map item names to their objects.
    private Dictionary<string, List<GameObject>> EnableDisableDict = new Dictionary<string, List<GameObject>>();

    public static InventoryController instance; // Shared instance of the InventoryController to enforce only one being created.

    // TODO: rename this
    [SerializeField]
    private bool isInstance = false; // Whether to use this object as the sole instance of InventoryController (cannot have multiple set to true).

    /// <summary>
    /// Check whether an instance of InventoryController has already been created. If it has, delete this instance.
    /// Initialize inventories specified by the user in the controller.
    /// </summary>
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
        if (!TestSetup()) return;
        if (!TestInstance()) return;
        TestChildObject();
        AllignDictionaries();
        InitializeItems();
    }
    private void Update()
    {
        DisableEnableOnKeyInput();
    }


    /// <summary>
    /// 
    /// </summary>
    public void InitializeInventories()
    {

        if (!TestSetup()) return;

        RemoveDeletedInventories();
        InitializeNewInventories();
        UpdateInventoryTracker();
        AllignDictionaries();
        InitializeItems();
    }
    public Inventory GetInventory(string name)
    {
        return inventoryManager[name];
    }
    private void UpdateInventoryTracker()
    {
        prevInventoryTracker.Clear();
        for (int i = 0; i < initializeInventory.Count; i++)
        {
            InventoryInitializer InitilizerCopy = new InventoryInitializer();
            InitilizerCopy.Copy(initializeInventory[i]);
            prevInventoryTracker.Add(InitilizerCopy);
        }
    }
    private void InitializeNewInventories()
    {
        foreach (InventoryInitializer initializer in initializeInventory)
        {
            if (!prevInventoryTracker.Contains(initializer))
            {
                initializer.SetInitialized(true);
                GameObject tempinventoryUI = Instantiate(inventoryManagerObj, transform.position, Quaternion.identity, UI);
                tempinventoryUI.SetActive(true);
                tempinventoryUI.name = initializer.GetInventoryName();
                allInventoryUI.Add(tempinventoryUI);

                string inventoryName = initializer.GetInventoryName();
                int InventorySize = initializer.GetRow() * initializer.GetCol();
                Inventory curInventory = new Inventory(tempinventoryUI,inventoryName, InventorySize);

                inventoryManager.Add(inventoryName, curInventory);

                InventoryUIManager inventoryUI = tempinventoryUI.GetComponent<InventoryUIManager>();

                inventoryUI.SetEnableDisable(initializer.GetEnableDisable());
                inventoryUI.SetInventory(ref curInventory);
                inventoryUI.SetHighlightable(initializer.GetHightlightable());
                inventoryUI.SetDraggable(initializer.GetDraggable());
                inventoryUI.SetRowCol(initializer.GetRow(), initializer.GetCol());
                inventoryUI.SetInventoryName(initializer.GetInventoryName());
                inventoryUI.UpdateInventoryUI();
            }
        }
        foreach(GameObject inObjects in allInventoryUI)
        {
            inObjects.GetComponent<InventoryUIManager>().UpdateInventoryUI();
        }
    }
    private void RemoveDeletedInventories()
    {

        List<GameObject> toremove = new List<GameObject>();
        foreach (InventoryInitializer initializer in prevInventoryTracker)
        {
            if (!initializeInventory.Contains(initializer))
            {
                foreach (GameObject UI in allInventoryUI)
                {
                    InventoryUIManager UIInstance = UI.GetComponent<InventoryUIManager>();
                    if (UIInstance.GetInventoryName() == initializer.GetInventoryName())
                    {
                        toremove.Add(UI);
                        inventoryManager.Remove(UIInstance.GetInventoryName());
                    }
                }
            }
        }

        foreach (GameObject remove in toremove)
        {
            allInventoryUI.Remove(remove);
            DestroyImmediate(remove);

        }
    }
    private void InitializeItems()
    {
        itemManager.Clear();
        foreach (ItemInitializer item in items)
        {
            Item newItem = new Item(item);
            itemManager.Add(item.GetItemType(), newItem);
        }
    }
    public void AddItem(string inventoryName, string itemType)
    {
        if(inventoryManager.ContainsKey(inventoryName))
        {
            Inventory inventory = inventoryManager[inventoryName];
            if (itemManager.ContainsKey(itemType))
            {
                Item item = itemManager[itemType];
                inventory.AddItem(item);

            }
            else
            {
                Debug.LogError("No Initialized Item with item Type: " + itemType);

            }
        }
        else
        {
            Debug.LogError("No Initialized Inventory with Name: " + inventoryName);
        }

    }
    public void AddItem(string inventoryName, Item itemType, int position)
    {
        Inventory inventory = inventoryManager[inventoryName];
        Item item = itemType;
        inventory.AddItem(item, position);
    }
    public void ResetInventory()
    {
        inventoryManager.Clear();
        itemManager.Clear();
        prevInventoryTracker.Clear();
        foreach (ItemInitializer item in items)
        {
            itemManager.Add(item.GetItemType(), new Item(item));
        }
        foreach (GameObject obj in allInventoryUI)
        {
            DestroyImmediate(obj);
        }
        allInventoryUI.Clear();
    }
    public void AllignDictionaries()
    {
        inventoryManager.Clear();
        foreach (GameObject inventories in allInventoryUI)
        {
            InventoryUIManager inventoryInstance = inventories.GetComponent<InventoryUIManager>();
            inventoryInstance.GetInventory().Init();
            inventoryManager.Add(inventoryInstance.GetInventoryName(), inventoryInstance.GetInventory());
            if(EnableDisableDict.ContainsKey(inventoryInstance.GetEnableDisable()))
            {
                EnableDisableDict[inventoryInstance.GetEnableDisable()].Add(inventories);
            }
            else
            {
                EnableDisableDict.Add(inventoryInstance.GetEnableDisable(), new List<GameObject>());
                EnableDisableDict[inventoryInstance.GetEnableDisable()].Add(inventories);
            }
        }
    }
    private void DisableEnableOnKeyInput()
    {
        if (Input.anyKeyDown)
        {
            string input = Input.inputString;
            if (EnableDisableDict.ContainsKey(input))
            {
                List<GameObject> inventoryUIs = EnableDisableDict[input];
                foreach (GameObject inventoryUI in inventoryUIs)
                {
                    if (inventoryUI.activeSelf)
                    {
                        inventoryUI.SetActive(false);
                    }
                    else
                    {
                        inventoryUI.SetActive(true);
                    }
                }
            }
        }
    }
    public bool TestSetup()
    {
        return TestInventoryUI()
            && TestinventoryManagerObjSetup()
            && TestInveInitializerListSetup()
            && TestUISetup();
            

    }
    private bool TestInventoryUI()
    {
        foreach (GameObject inventories in allInventoryUI)
        {
            if (inventories == null)
            {
                Debug.LogError("Inventories Are Null, Try Unpacking IventoryController");
                return false;
            }
        }
        return true;
    }
    public bool TestinventoryManagerObjSetup()
    {
        if (inventoryManagerObj == null)
        {
            Debug.LogError("Inventory Manager Object Not Set Correclty.");
            return false;
        }
        return true;
    }
    private bool TestInveInitializerListSetup()
    {
        for (int i = 0; i < initializeInventory.Count; i++)
        {
            int countInstance = 0;

            for (int j = 0; j < initializeInventory.Count; j++)
            {
                if (initializeInventory[i].GetInventoryName().Equals(initializeInventory[j].GetInventoryName()))
                {
                    countInstance++;
                }
                if (countInstance > 1)
                {
                    Debug.LogError("You can only have one of each inventory name");

                    return false;
                }
            }

        }
        return true;
    }
    private bool TestUISetup()
    {
        if (UI == null)
        {
            Debug.LogError("UI Canvas Not Set Correctly");
            return false;
        }
        return true;
    }
    public bool TestInstance()
    {
        if (instance == null)
        {
            Debug.LogError("You must choose ONE InventoryController instance by clicking the bool isInstance inside InventoryController script");
            return false;
        }
        return true;
    }
    private void TestChildObject()
    {
        InventoryUIManager manager = transform.GetComponentInChildren<InventoryUIManager>();

        if (manager != null)
        {
            if (manager.gameObject.activeSelf)
            {
                Debug.LogWarning("The Child of Inventory Controller, InventoryUIManager is active. Disabling it now.");
                manager.gameObject.SetActive(false);
            }
        }
        else
        {
            if(transform.childCount == 0)
            Debug.LogWarning("Inventory Controller Does Not Have Child Object with InventoryUIManager");
        }
    }
    public Transform GetUI()
    {
        return UI;
    }
    public List<ItemInitializer> GetItems()
    {
        return items;
    }
}
