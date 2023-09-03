using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using UnityEngine.XR;
using static UnityEditor.Progress;



//Author: Jaxon Schauer
/// <summary>
/// This class defines an inventory controller, which allows for creating new inventories and defining valid types of objects.
/// Only one InventoryController should be instantiated within a project. Multiple inventories can be created from one controller.
/// This controller manages all information being given to an inventory
/// </summary>

public class InventoryController : MonoBehaviour 
{
    [Header("============[ Setup Confirmation ]============")]
    [Header("**********************************************")]
    [Header("Click the \"I Understand The Setup\" to show you understand")]
    [Header("there should only ever be one InventoryController and the")]
    [Header("InventoryController needs to be unpacked in the scene")]
    [Header("**********************************************")]
    [Tooltip("Toggle to confirm you understand the setup requirements.")]
    [SerializeField]
    private bool iUnderstandTheSetup = false; // Ensure this is true for the sole instance of InventoryController.

    [Space(10)] // Add some space for better organization

    [Header("============[ Inventory Controller Setup ]============")]
    [Space(20)] 
    [Tooltip("Assign Main Canvas Here")]
    [SerializeField]
    private Transform UI; // UI canvas to build inventories on.

    [Space(10)] // Add some space

    [Header("========[ Items Setup ]========")]
    [Header("NOTE: All changes to items must be made here")]
    [Tooltip("Add templates for each accepted item.")]
    [SerializeField]
    public List<ItemInitializer> items; // Accepted items to add to the inventory.

    [Space(10)] // Add some space

    [Header("========[ Inventory Setup ]========")]
    [Header("NOTE: After initialization, changes here won't take effect.")]
    [Header("Modify the inventory under the UI component.")]
    [Tooltip("Add templates for each inventory to be initialized.")]
    [SerializeField]
    private List<InventoryInitializer> initializeInventory = new List<InventoryInitializer>(); // Information about inventory setup.




    [SerializeField, HideInInspector]
    private List<InventoryInitializer> prevInventoryTracker; // Previously initialized inventories, so they are not initialized again.


    [SerializeField]
    private GameObject inventoryManagerObj; // Prefab for the inventory manager.

    [SerializeField, HideInInspector]
    private List<GameObject> allInventoryUI = new List<GameObject>(); // Holds all inventory UI instances for each inventory created.
    private Dictionary<string, Inventory> inventoryManager = new Dictionary<string, Inventory>(); // Dictionary to map inventory names to their objects.
    private Dictionary<string, GameObject> inventoryUIDict = new Dictionary<string, GameObject>(); // Dictionary to map inventory names to their objects.

    private Dictionary<string, InventoryItem> itemManager = new Dictionary<string, InventoryItem>(); // Dictionary to map item names to their objects.
    private Dictionary<string, List<GameObject>> EnableDisableDict = new Dictionary<string, List<GameObject>>();

    public static InventoryController instance; // Shared instance of the InventoryController to enforce only one being created.

    // TODO: rename this


    /// <summary>
    /// Check whether an instance of InventoryController has already been created. If it has, delete this instance.
    /// Initialize inventories specified by the user in the controller.
    /// </summary>
    private void Awake()
    {
        if (iUnderstandTheSetup)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        if (!TestInstance()) return;
        if (!TestSetup()) return;
        TestChildObject();
        AllignDictionaries();
        InitializeItems();
    }
    /// <summary>
    /// Loads any saved inventories on start <see cref="LoadSave"/>
    /// </summary>
    private void Start()
    {
        LoadSave();
    }
    /// <summary>
    /// Constantly checks for input to pass to HighLightOnButtonPress
    /// </summary>
    private void Update()
    {
        ToggleOnKeyInput();
    }

    /// <summary>
    /// Uses <see cref="TestSetup"/> to check that the user has correctly setup inventory
    /// If the user has setup inventory correctly, then the initiaization functions are run, loading the new inventories and deleting missing inventories
    /// </summary>
    public void InitializeInventories()
    {

        if (!TestSetup()) return;

        if (iUnderstandTheSetup)
        {
            instance = this;
        }
        AllignDictionaries();
        RemoveDeletedInventories();
        InitializeNewInventories();
        UpdateInventoryTracker();
        InitializeItems();
    }
    /// <summary>
    /// Handles the prevInventoryTracker list, allowing it to track the changes made from the previous initialization and stopping it from initializing
    /// or deleting the unecessary information in the functions running of <see cref="InitializeNewInventories"/>  
    /// and <see cref="RemoveDeletedInventories"/>
    /// </summary>
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
    /// <summary>
    /// Initializes any new inventories, giving the necessary information to the <see cref="Inventory"/> class 
    /// and the <see cref="InventoryUIManager"/>, allowing them to work together displaying and maintaining the information of the inventory
    /// </summary>
    private void InitializeNewInventories()
    {
        foreach (InventoryInitializer initializer in initializeInventory)
        {
            if (!prevInventoryTracker.Contains(initializer))
            {
                initializer.SetInitialized(true);
                GameObject tempinventoryUI = Instantiate(inventoryManagerObj, transform.position, Quaternion.identity, UI);
                RectTransform UIRect = UI.GetComponent<RectTransform>();
                tempinventoryUI.transform.position = new Vector3(Random.Range(0.0f, UIRect.sizeDelta.x), Random.Range(0.0f, UIRect.sizeDelta.y),0);
                tempinventoryUI.SetActive(true);
                tempinventoryUI.name = initializer.GetInventoryName();
                allInventoryUI.Add(tempinventoryUI);

                string inventoryName = initializer.GetInventoryName();
                int InventorySize = initializer.GetRow() * initializer.GetCol();
                Inventory curInventory = new Inventory(tempinventoryUI,inventoryName, InventorySize);

                inventoryManager.Add(inventoryName, curInventory);

                InventoryUIManager inventoryUI = tempinventoryUI.GetComponent<InventoryUIManager>();
                inventoryUI.SetVarsOnInit();
                inventoryUI.SetInventory(ref curInventory);
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
    /// <summary>
    /// Initializes any new inventories, giving the necessary information to the <see cref="Inventory"/> class 
    /// and the <see cref="InventoryUIManager"/>, allowing them to work together displaying and maintaining the information of the inventory
    /// </summary>
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
    /// <summary>
    /// Initializes the items into <see cref="itemManager"/>, allowing for the inventory to make deep copies of the items when needed
    /// </summary>
    private void InitializeItems()
    {
        itemManager.Clear();
        foreach (ItemInitializer item in items)
        {
            InventoryItem newItem = new InventoryItem(item);
            itemManager.Add(item.GetItemType(), newItem);
        }
    }
    /// <summary>
    /// Takes in input of two strings, one that is inputted into the <see cref="inventoryManager"/> and a reference to an item in <see cref="itemManager"/>, passing the item to the 
    /// expected inventory and adding a deep copy of the expected item and adding into the lowest unused possition in the <see cref="Inventory.GetList()"/>
    /// uses <see cref="Inventory.AddItem(InventoryItem)"/>.
    /// </summary>
    public void AddItemPos(string inventoryName, InventoryItem itemType, int position)
    {
        if (!(TestInventoryDict(inventoryName)))
        {
            return;
        }
        if (itemType.GetIsNull())
        {
            Debug.LogError("Cannot add null item");
            return;
        }
        Inventory inventory = inventoryManager[inventoryName];
        inventory.AddItemPos(position, itemType);
    }
    /// <summary>
    /// Addits a new item to a specified inventory, in a specified location. Uses <see cref="Inventory.AddItemLinearly(InventoryItem, int)/>
    /// </summary>
    public void AddItemPos(string inventoryName, string itemType, int position, int amount = 1)
    {
        if(!(TestInventoryDict(inventoryName) && TestItemDict(itemType)))
        {
            return;
        }
        Inventory inventory = inventoryManager[inventoryName];
        InventoryItem item = new InventoryItem(itemManager[itemType], amount);
        inventory.AddItemPos(position, item);
    }
    public void AddItemLinearly(string inventoryName, string itemType, int amount = 1)
    {
        if (!(TestInventoryDict(inventoryName) && TestItemDict(itemType)))
        {
            return;
        }
        Inventory inventory = inventoryManager[inventoryName];
        InventoryItem item = new InventoryItem(itemManager[itemType], amount);
        inventory.AddItemLinearly(item);
    }

    public void RemoveItemPos(string inventoryName, int position, int amount)
    {
        if (!(TestInventoryDict(inventoryName)))
        {
            return;
        }
        Inventory inventory = inventoryManager[inventoryName];
        inventory.RemoveItemInPosition(position, amount);

    }
    public void RemoveItem(string inventoryName, InventoryItem item, int amount = 1)
    {
        if (!(TestInventoryDict(inventoryName)))
        {
            return;
        }
        if (item.GetIsNull())
        {
            Debug.LogError("Cannot remove null item");
            return;
        }

        Inventory inventory = inventoryManager[inventoryName];
        inventory.RemoveItemInPosition(item, amount);
    }
    private bool TestInventoryDict(string inventoryName)
    {
        if(inventoryName == null)
        {
            Debug.LogError("Inventory name is null");
            return false;
        }
        if (inventoryManager.ContainsKey(inventoryName))
        {
            return true;
        }
        else
        {
            Debug.LogError("No existing inventory with name: " + inventoryName);
            return false;
        }
    }
    private bool TestItemDict(string itemType)
    {
        if (itemType == null)
        {
            Debug.LogError("Itemtype is null");
            return false;
        }
        if (itemManager.ContainsKey(itemType))
        {
            return true;
        }
        else
        {
            Debug.LogError("No existing item with name: " + itemType);
            return false;
        }
    }
    /// <summary>
    /// This is a debug function used to reset all lists. This will delete any all existing inventories in the scnene
    /// </summary>
    public void ResetInventory()
    {
        inventoryManager.Clear();
        itemManager.Clear();
        prevInventoryTracker.Clear();
        foreach (ItemInitializer item in items)
        {
            itemManager.Add(item.GetItemType(), new InventoryItem(item));
        }
        foreach (GameObject obj in allInventoryUI)
        {
            DestroyImmediate(obj);
        }
        allInventoryUI.Clear();
        inventoryUIDict.Clear();
        InventorySaveSystem.Reset();
    }
    /// <summary>
    /// This utilizes the serialized list allInventoryUI to setup the dictionaries <see cref="inventoryManager"/> and <see cref="EnableDisableDict"/>
    /// </summary>
    public void AllignDictionaries()
    {
        inventoryManager.Clear();
        foreach (GameObject InventoryUI in allInventoryUI)
        {
            InventoryUIManager inventoryInstance = InventoryUI.GetComponent<InventoryUIManager>();
            if(!inventoryUIDict.ContainsKey(inventoryInstance.GetInventoryName()))
            {
                inventoryUIDict.Add(inventoryInstance.GetInventoryName(), InventoryUI);

            }
            inventoryInstance.GetInventory().InitList();
            inventoryManager.Add(inventoryInstance.GetInventoryName(), inventoryInstance.GetInventory());
            foreach(char character in inventoryInstance.GetEnableDisable())
            {
                if (EnableDisableDict.ContainsKey(character.ToString().ToLower()))
                {
                    EnableDisableDict[character.ToString().ToLower()].Add(InventoryUI);
                }
                else
                {
                    EnableDisableDict.Add(character.ToString().ToLower(), new List<GameObject>());
                    EnableDisableDict[character.ToString().ToLower()].Add(InventoryUI);
                }
            }
        }
    }
    public int CountItems(string inventoryName, string itemType)
    {
        if (!(TestInventoryDict(inventoryName) && TestItemDict(itemType)))
        {
            return 0;
        }
        Inventory inventory = inventoryManager[inventoryName];
        return inventory.Count(itemType);
    }
    /// <summary>
    /// runs setup test sweet, returns false if there is a setup error.
    /// </summary>
    public void AddToggleKey(string InventoryName, char character)
    {
        if (EnableDisableDict.ContainsKey(character.ToString().ToLower()))
        {
            if (EnableDisableDict[character.ToString().ToLower()].Contains(inventoryUIDict[InventoryName]))
            {
                Debug.LogWarning("Dict Already Contains obj");
                return;
            }
            EnableDisableDict[character.ToString().ToLower()].Add(inventoryUIDict[InventoryName]);
        }
        else
        {
            EnableDisableDict.Add(character.ToString().ToLower(), new List<GameObject>());
            EnableDisableDict[character.ToString().ToLower()].Add(inventoryUIDict[InventoryName]);
        }
    }
    public void RemoveToggleKey(string InventoryName, char character)
    {
        if (EnableDisableDict.ContainsKey(character.ToString().ToLower()))
        {
            if (!EnableDisableDict[character.ToString().ToLower()].Contains(inventoryUIDict[InventoryName]))
            {
                Debug.LogWarning("Dict Does Not Contain Object attached to char");
                return;
            }
            EnableDisableDict[character.ToString().ToLower()].Remove(inventoryUIDict[InventoryName]);
        }
        else
        {
            Debug.LogWarning("Given character is not attached to a InventoryUIManager");
        }
    }
        /// <summary>
        /// called by <see cref="Update"/> to check if a user given keyinput has been pressed, and if so disable/enable the inventory. 
        /// </summary>
        private void ToggleOnKeyInput()
    {
        if (Input.anyKeyDown)
        {
            string input = Input.inputString;
            input = input.ToLower();
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
    /// <summary>
    /// Loads saved file on <see cref="Start"/> 
    /// </summary>
    private void LoadSave()
    {
        InventorySaveSystem.Create();
        if (InventorySaveSystem.LoadItem() != null)
        {
            InventoryData itemData = InventorySaveSystem.LoadItem();
            foreach (var pair in itemData.inventories)
            {
                Inventory inventory = inventoryManager[pair.Key];
                if (pair.Key == null)
                    continue;
                if (!inventory.GetSaveInventory())
                {
                    continue;
                }
                List<ItemData> items = pair.Value;
                foreach (ItemData item in items)
                {

                    if (item.name != null)
                    {
                        InventoryItem copyItem = itemManager[item.name];
                        InventoryItem newItem = new InventoryItem(copyItem, item.amount);
                        AddItemPos(pair.Key, newItem, item.position);
                    }
                }
            }
        }
    }
    /// <summary>
    /// Can be used to programatically create inventoryUI. This is a skeleton of what is likely needed. More customization is recommended for your situation.
    /// </summary>
    public void CreateInventory(Transform instantiaterPos, string inventoryName, int row, int col,
        bool highlightable = false, bool draggable = false, bool saveInventory = false, bool isActive = true)
    {
        if (!TestSetup()) return;
        GameObject tempinventoryUI = Instantiate(inventoryManagerObj, instantiaterPos.position, Quaternion.identity, UI);
        tempinventoryUI.SetActive(isActive);

        tempinventoryUI.transform.position = instantiaterPos.position;

        Inventory curInventory = new Inventory(tempinventoryUI, inventoryName, col * row);
        inventoryManager.Add(inventoryName, curInventory);

        InventoryUIManager inventoryUI = tempinventoryUI.GetComponent<InventoryUIManager>();
        inventoryUI.SetVarsOnInit();
        inventoryUI.SetSave(saveInventory);
        inventoryUI.SetInventory(ref curInventory);
        inventoryUI.SetHighlightable(highlightable);
        inventoryUI.SetDraggable(draggable);
        inventoryUI.SetRowCol(row, col);
        inventoryUI.SetInventoryName(inventoryName);
        inventoryUI.UpdateInventoryUI();
    }
    /// <summary>
    /// runs setup test sweet, returns false if there is a setup error.
    /// </summary>
    public bool TestSetup()
    {
        return TestinventoryManagerObjSetup()
            && TestInventoryUI()
            && TestInveInitializerListSetup()
            && TestUISetup();
            

    }
    /// <summary>
    /// Tests that the inventories in allInventoryUI are correctly instantiated
    /// </summary>
    private bool TestInventoryUI()
    {
        if(allInventoryUI.Count == 0 && Application.isPlaying)
        {
            Debug.LogWarning("No InventoryUIManagers Detected. Ensure to initialize all inventories in editor mode. If Unexpected Try Unpacking InventoryController");
            return false;
        }
        foreach (GameObject inventories in allInventoryUI)
        {
            if (inventories == null)
            {
                Debug.LogError("Inventories in allInventoryUI Are Null, Try hitting button \"Delete All Instantiated Objects\"");
                return false;
            }
        }
        return true;
    }
    /// <summary>
    /// Checks the InventoryManagerUI exists in context
    /// </summary
    public bool TestinventoryManagerObjSetup()
    {
        if (inventoryManagerObj == null)
        {
            Debug.LogError("Inventory manager object is null");
            return false;
        }
        return true;
    }
    /// <summary>
    /// Checks there are no duplicate named inventories
    /// </summary
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
                    Debug.LogError("You Can Only Have One Of Each Inventory Name");

                    return false;
                }
            }

        }
        return true;
    }
    /// <summary>
    /// Tests the UI object has been chosen
    /// </summary
    private bool TestUISetup()
    {
        if (UI == null)
        {
            Debug.LogError("UI Canvas Not Set Correctly");
            return false;
        }
        return true;
    }
    /// <summary>
    /// Test that an instance has been chosen
    /// </summary
    public bool TestInstance()
    {
        if (instance == null)
        {
            Debug.LogError("Read Instructions and Click The I Understand The Setup Bool. Inventory Controller Destroyed");
            return false;
        }
        return true;
    }
    /// <summary>
    /// Test whether or not the inventory controller rhas the correct child object
    /// </summary
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
    public bool checkUI(GameObject obj)
    {
        if (inventoryUIDict.ContainsValue(obj))
        {
            return true;
        }
        return false;
    }
    public Inventory GetInventory(string inventoryName)
    {
        return inventoryManager[inventoryName];
    }
    public InventoryItem GetItem(string inventoryName, int index)
    {
        return inventoryManager[inventoryName].InventoryGetItem(index);
    }
    public Transform GetUI()
    {
        return UI;
    }
    public List<ItemInitializer> GetItems()
    {
        return items;
    }
    private void OnDestroy()
    {
        InventorySaveSystem.SaveInventory(inventoryManager);
    }
}
