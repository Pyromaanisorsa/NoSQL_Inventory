using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

public class InventoryUI : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject itemGrid;
    public GameObject bagGrid;
    public TextMeshProUGUI weightCounter;
    public TMP_InputField searchBar;

    [Header("UI Slots")] //These are public for testing purposes
    public List<InventoryUISlot> slotList;
    public List<BagSlotUI> bagList;
    public WeaponSlotUI weaponSlot;

    [Header("Prefabs+Refs")]
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private GameObject bagSlotPrefab;
    [SerializeField] private GameObject weaponSlotPrefab;
    [SerializeField] private RightClickPanel panelScript;
    public Inventory inventory;

    [Header("Generic Variables")]
    [SerializeField] private int currentActiveBag = 0;
    [SerializeField] private bool searchActive = false;
    [SerializeField] private bool inspectActive;
    [SerializeField] private bool searchSelected = false;
    private Color hiddenColor = new Color(1, 1, 1, 0);

    // Hides right context panel whenever you click something
    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            panelScript.Hide();
        }
        //else if (Input.GetMouseButtonDown(0))
        //{
        //    if (!EventSystem.current.IsPointerOverGameObject())
        //        panelScript.Hide();
        //}
    }

    // Setup UI first time / on start of the game
    public void SetupInventoryUI()
    {
        // Make inventory view visible on Setup / when player logins
        CanvasGroup group = gameObject.GetComponent<CanvasGroup>();
        group.blocksRaycasts = true;
        group.alpha = 1;

        // Setup main bagSlot separately; Disable removeButton since main bag can't be removed!
        GameObject uiSlot = Instantiate(bagSlotPrefab, bagGrid.transform);
        BagSlotUI slotScript = uiSlot.GetComponent<BagSlotUI>();
        slotScript.SetSlotLocation(0, 0, this);
        bagList.Add(uiSlot.GetComponent<BagSlotUI>());

        // Set bag image and onClick functions for slotButtons
        slotScript.itemButton.image.sprite = IconManager.Instance.GetIconByID(inventory.bags[0].bagItem.IconID);
        slotScript.itemButton.onClick.AddListener(() => LeftClickBagSlot(0));
        slotScript.itemButton.OnRightClick += async () => await RightClickBagSlot(0);

        // Setting up bag slots
        GenerateBagSlots();

        // Setting up inventory slots, at setup; main bag is generated
        GenerateItemSlots();

        // Setup WeaponSlot
        weaponSlot.itemButton.OnRightClick += async () => await RightClickWeaponSlot();
        weaponSlot.SetSlotLocation(this);
        UpdateWeaponSlot();

        // Setup weight counter+bag space counters
        UpdateSecondaryElements();

        // Setup searchBar function
        searchBar.onValueChanged.AddListener(delegate { SearchItem(searchBar.text); });
        searchBar.onSelect.AddListener(delegate { ToggleSearchActive(true); });
        searchBar.onDeselect.AddListener(delegate { ToggleSearchActive(false); });
    }

    #region UpdateUI functions
    // Update InventorySlot UI
    private void UpdateSlot(int invSlot)
    {
        // Makes code more bearable
        int bagNum = slotList[invSlot].bagNum, slotNum = slotList[invSlot].slotNum;

        // Update item icon to slot
        slotList[invSlot].itemButton.image.sprite = IconManager.Instance.GetIconByID(inventory.bags[bagNum].bagInventory[slotNum].item.IconID);

        // Empty item; hide stackCount, set itemIcon to empty and disable slot buttons
        if (inventory.bags[bagNum].bagInventory[slotNum].IsSlotNull()) 
        {
            SetInventorySlotButtonInactive(invSlot);
            return;
        }

        // Activate slot buttons
        SetInventorySlotButtonActive(invSlot);

        // Stackable item; show and update stackCount
        if (inventory.bags[bagNum].bagInventory[slotNum].item.Stackable)
        {
            TextMeshProUGUI text = slotList[invSlot].stackBG.GetComponentInChildren<TextMeshProUGUI>();
            text.text = inventory.bags[bagNum].bagInventory[slotNum].currentStack.ToString();
            slotList[invSlot].stackBG.gameObject.SetActive(true);
        }
        // Not stackable item; hide stackCount
        else
            slotList[invSlot].stackBG.gameObject.SetActive(false);
    }

    // Update BagSlot UI
    private void UpdateBagSlot(int bagSlot)
    {
        // Update bagSlot image
        bagList[bagSlot].itemButton.image.sprite = IconManager.Instance.GetIconByID(inventory.bags[bagSlot].bagItem.IconID);

        // Bag is empty; Disable empty bagSlot buttons and set currentActiveBag to mainBag if current bag is empty
        if (inventory.bags[bagSlot].IsBagNull())
            SetBagSlotButtonInactive(bagSlot);

        // Bag is not empty; Enable bagSlot buttons and toggle UI to the just equipped bag
        else
            SetBagSlotButtonActive(bagSlot);
    }

    // Update WeaponSlot UI and interactability
    private void UpdateWeaponSlot()
    {
        // Update icon and set removeButton active/inactive if empty weaponSlot
        weaponSlot.itemButton.image.sprite = IconManager.Instance.GetIconByID(inventory.mainHandWeapon.weapon.IconID);

        if (inventory.mainHandWeapon.IsWeaponNull()) 
        {
            weaponSlot.itemButton.interactable = false;
            weaponSlot.itemButton.image.color = hiddenColor;
            weaponSlot.dragReference.enabled = false;
        }
           
        else 
        {
            weaponSlot.itemButton.interactable = true;
            weaponSlot.itemButton.image.color = Color.white;
            weaponSlot.dragReference.enabled = true;
        }
    }

    // Update all current itemSlots (not bagSlots)
    private void UpdateSlotList()
    {
        for(int i = 0; i < slotList.Count; i++)
            UpdateSlot(i);
    }

    // Update all inventory UI elements
    private void UpdateInventoryUI()
    {
        // Update current itemSlots
        UpdateSlotList();

        // Update all bagSlots
        for (int slot = 0; slot < bagList.Count; slot++)
            UpdateBagSlot(slot);

        // Update rest of UI elements
        UpdateWeaponSlot();
        UpdateSecondaryElements();
    }

    // Update entire UI after adding items
    public void UpdateUIAddItem()
    {
        // Update slots and elements (ADD single slot update?)
        UpdateSlotList();
        UpdateSecondaryElements();
    }

    // Update weight counter+bag space counters
    private void UpdateSecondaryElements()
    {
        UpdateWeightCounterUI();
        UpdateBagSpaceCounters();
    }

    // Update weight counter
    private void UpdateWeightCounterUI()
    {
        weightCounter.text = "Weight: " + inventory.weightCurrent + " / " + inventory.weightMax;
    }

    // Update bag space counters
    private void UpdateBagSpaceCounters()
    {
        // Calculate empty bag space for each non-empty bag set it as text
        for (int i = 0; i < bagList.Count; i++)
        {
            if (!inventory.bags[i].IsBagNull())
            {
                TextMeshProUGUI text = bagList[i].stackBG.GetComponentInChildren<TextMeshProUGUI>();
                text.text = inventory.bags[i].GetBagSpaceCount().ToString();
            }
        }
    }
    #endregion

    #region Universal Container Functions
    // Universal leftClick inventorySlot function, but doesn't do anything atm
    private async Task LeftClickInventorySlot(int invSlot)
    {
        // Hide right click panel
        panelScript.Hide();
    }

    // Universal rightClick inventorySlot function
    private async Task RightClickInventorySlot(int invSlot)
    {
        // Hide right click panel
        panelScript.Hide();

        // Makes code less bloated
        int bagNum = slotList[invSlot].bagNum, slotNum = slotList[invSlot].slotNum;

        // Clear old actions from panel dictionary
        panelScript.ClearActions();

        // Add functions to RightClick context panels action dictionary, with key as the button's text based on the item type
        switch (inventory.bags[bagNum].bagInventory[slotNum].item.ItemType)
        {
            case ItemType.Bag:
                panelScript.actions.Add("Equip", () => EquipBag(invSlot, bagNum, slotNum));
                break;

            case ItemType.Consumable:
                panelScript.actions.Add("User", () => UseConsumable(invSlot, bagNum, slotNum));
                break;

            case ItemType.Weapon:
                panelScript.actions.Add("Equip", () => EquipWeapon(invSlot, bagNum, slotNum));
                break;
        }

        // Add Inspect and Destroy option to every ItemType
        panelScript.actions.Add("Inspect", () => InspectInventorySlot(invSlot));
        panelScript.actions.Add("Destroy", () => RemoveItem(invSlot));

        // Set up and show the context menu
        panelScript.SetupMenu();

        // Position the menu pivot at the mouse position
        Vector2 mousePosition = Input.mousePosition;
        panelScript.Show(mousePosition);
    }

    // Universal doubleClick inventorySlot function
    private async Task DoubleClickInventorySlot(int invSlot)
    {
        // Makes code less bloated
        int bagNum = slotList[invSlot].bagNum, slotNum = slotList[invSlot].slotNum;

        // Activate a function based off slot's item's itemtype.
        switch (inventory.bags[bagNum].bagInventory[slotNum].item.ItemType)
        {
            case ItemType.Bag:
                EquipBag(invSlot, bagNum, slotNum);
                break;

            case ItemType.Consumable:
                UseConsumable(invSlot, bagNum, slotNum);
                break;

            case ItemType.Weapon:
                EquipWeapon(invSlot, bagNum, slotNum);
                break;

            case ItemType.Null:
                MessagePanel.Instance.DisplayMessage("Empty slot, how did you manage to even trigger this LOL");
                break;

            default:
                break;
        }
    }

    // Set pressed non-active bag active and generate inventorySlots from current active bag
    private void LeftClickBagSlot(int bagSlot)
    {
        // Hide right click panel
        panelScript.Hide();

        // Don't toggle current bag for no reason
        if (bagSlot != currentActiveBag)
        {
            // If currently searching; remove search before bagToggle
            CancelSearch();

            // Generate slots from clicked bagSlot
            currentActiveBag = bagSlot;
            GenerateItemSlots();
        }
    }

    // Universal rightClick bagSlot function
    private async Task RightClickBagSlot(int bagSlot)
    {
        // Hide right click panel
        panelScript.Hide();

        // Don't do anything if bagSlot is null
        if (inventory.bags[bagSlot].IsBagNull())
            return;

        // Clear old actions from panel dictionary
        panelScript.ClearActions();

        // Add Bag function options to RightPanel actions dictionary
        panelScript.actions.Add("Inspect", () => InspectBagContents(bagSlot));
        
        // Prevent "trying" to unequip main bag
        if(bagSlot != 0)
            panelScript.actions.Add("Unequip", () => RemoveBag(bagSlot));

        // Set up and show the context menu
        panelScript.SetupMenu();

        // Position the menu pivot at the mouse position
        Vector2 mousePosition = Input.mousePosition;
        panelScript.Show(mousePosition);
    }

    // Universal rightClick weaponSlot function
    private async Task RightClickWeaponSlot()
    {
        // Hide right click panel
        panelScript.Hide();

        // Don't do anything if bagSlot is null
        if (inventory.mainHandWeapon.IsWeaponNull())
            return;

        // Clear old actions from panel dictionary
        panelScript.ClearActions();

        // Add Bag function options to RightPanel actions dictionary
        panelScript.actions.Add("Unequip", () => RemoveWeapon());
        panelScript.actions.Add("Destroy", () => DestroyWeapon());

        // Set up and show the context menu
        panelScript.SetupMenu();

        // Position the menu pivot at the mouse position
        Vector2 mousePosition = Input.mousePosition;
        panelScript.Show(mousePosition);
    }

    // Universal EndDrag function when target is InventorySlot
    public async Task DraggingDropInventorySlot(int targetInvSlot, UISlotBase originReference) 
    {
        // Switch based off of origin/dragged slot's type/class
        switch (originReference)
        {
            // InventorySlot dropped on InventorySlot; swap location of items
            case InventoryUISlot invSlot:
                if (invSlot.slot == targetInvSlot)
                    return;
                await ItemDroppedOnInventorySlot(invSlot.slot, targetInvSlot);
                break;

            // BagSlot dropped on InventorySlot; try to unequip a bag to target slot
            case BagSlotUI bagSlot:
                await BagDroppedOnInventorySlot(bagSlot.slot, targetInvSlot);
                break;

            // WeaponSlot dropped on InventorySlot; try to unequip a weapon to target slot
            case WeaponSlotUI:
                await WeaponDroppedOnInventorySlot(targetInvSlot);
                break;
        }

        // Decide what to do if you drop item slot on inventorySlot
        async Task ItemDroppedOnInventorySlot(int originInvSlot, int targetInvSlot)
        {
            // Makes code less bloated
            int originBagNum = slotList[originInvSlot].bagNum, originSlotNum = slotList[originInvSlot].slotNum;
            int targetBagNum = slotList[targetInvSlot].bagNum, targetSlotNum = slotList[targetInvSlot].slotNum;

            // If origin inventorySlot is stackable and both slots contain same item
            if (IsSlotStackableAndSameItem())
            {
                // If both slots stacks are not full
                if(IsBothSlotsNotFull()) 
                {
                    await CombineItemStacks(originInvSlot, targetInvSlot);
                }
                // Else swap items between inventory slots
                else
                {
                    await SwapItems(originInvSlot, targetInvSlot);
                }
            }
            // Else swap items between inventory slots
            else
            {
                await SwapItems(originInvSlot, targetInvSlot);
            }

            // Check if slot is stackable and slots are same item
            bool IsSlotStackableAndSameItem() 
            {
                if (inventory.bags[originBagNum].bagInventory[originSlotNum].IsSlotStackable())
                    if (inventory.bags[originBagNum].bagInventory[originSlotNum].item.ItemID == inventory.bags[targetBagNum].bagInventory[targetSlotNum].item.ItemID)
                        return true;
                return false;
            }

            // Check if either of the slots is full;
            bool IsBothSlotsNotFull() 
            {
                if (!inventory.bags[originBagNum].bagInventory[originSlotNum].IsSlotStackFull() && !inventory.bags[targetBagNum].bagInventory[targetSlotNum].IsSlotStackFull())
                    return true;
                return false;
            }
        }

        // Decide what to do if you drop bag slot on inventorySlot
        async Task BagDroppedOnInventorySlot(int originBagSlot, int targetInvSlot)
        {
            // Makes code less bloated
            int bagNum = slotList[targetInvSlot].bagNum, slotNum = slotList[targetInvSlot].slotNum;

            // Trying to move to bag to it's own bagInventory; don't bother
            if (bagNum == originBagSlot)
                return;

            // If target inventory slot is empty; unequip bag there
            if (inventory.bags[bagNum].bagInventory[slotNum].IsSlotNull())
            {
                await RemoveBag(originBagSlot, bagNum, slotNum);
            }
            // Else try to replace equipped bag with target inventory slot bag item
            else 
            {
                // No bag item in target inventory slot; don't bother
                if (inventory.bags[bagNum].bagInventory[slotNum].item.ItemType != ItemType.Bag)
                    return;
                await ReplaceBag(targetInvSlot, bagNum, slotNum, originBagSlot);
            }
        }

        // Decide what to do if you drop weapon slot on inventorySlot
        async Task WeaponDroppedOnInventorySlot(int targetInvSlot)
        {
            // Makes code less bloated
            int bagNum = slotList[targetInvSlot].bagNum, slotNum = slotList[targetInvSlot].slotNum;

            // If target inventorySlot is null; unequip the weapon to that slot
            if (inventory.bags[bagNum].bagInventory[slotNum].IsSlotNull())
            {
                await RemoveWeapon(targetInvSlot, bagNum, slotNum);
            }
            // Else try to swap equipped weapon with weapon from inventory 
            else
            {
                // Target slot not weapon; don't bother
                if (inventory.bags[bagNum].bagInventory[slotNum].item.ItemType != ItemType.Weapon)
                    return;
                await EquipWeapon(targetInvSlot, bagNum, slotNum);
            }
        }
    }

    // Universal EndDrag function when target is BagSlot
    public async Task DraggingDropBagSlot(int targetBagSlot, UISlotBase originReference)
    {
        // Switch based off of origin/dragged slot's type/class
        switch (originReference)
        {
            // InventorySlot dropped on BagSlot; either equip bag to empty bag slot / swap bags OR insert item to target bag
            case InventoryUISlot invSlot:
                await ItemDroppedOnBagSlot(invSlot.slot, targetBagSlot);
                break;

            // BagSlot dropped on BagSlot; try to swap bags
            case BagSlotUI bagSlot:
                await SwapBags(bagSlot.slot, targetBagSlot);
                break;
        }

        // Decide what to do if you drop bag item on bagSlot
        async Task ItemDroppedOnBagSlot(int originInvSlot, int targetBagSlot)
        {
            // Makes code less bloated
            int bagNum = slotList[originInvSlot].bagNum, slotNum = slotList[originInvSlot].slotNum;

            // If dropped item is a bag
            if (inventory.bags[bagNum].bagInventory[slotNum].item.ItemType == ItemType.Bag) 
            {
                // If dropped on empty bagSlot; just try to equip bag normal way
                if (inventory.bags[targetBagSlot].IsBagNull())
                {
                    await EquipBag(originInvSlot, bagNum, slotNum, targetBagSlot);
                }
                // Else try to swap equipped bag with one from inventory
                else
                {
                    await ReplaceBag(originInvSlot, bagNum, slotNum, targetBagSlot);
                }
            }
            // Non-bag item; insert to target inventory
            else 
            {
                await InsertItemToBag(originInvSlot, targetBagSlot);
            }
        }
    }

    // Universal EndDrag function when target is WeaponSlot
    public async Task DraggingDropWeaponSlot(UISlotBase originReference)
    {
        // Switch based off of origin/dragged slot's type/class
        switch (originReference)
        {
            // InventorySlot dropped on WeaponSlot; swap location of items
            case InventoryUISlot invSlot:
                ItemDroppedOnWeaponSlot(invSlot.slot);
                break;

            // WeaponSlot dropped on WeaponSlot; not used in since there's only one weaponSlot
            //case WeaponSlotUI wepSlot:
            //    break;
        }

        async Task ItemDroppedOnWeaponSlot(int originInvSlot)
        {
            // Makes code less bloated
            int bagNum = slotList[originInvSlot].bagNum, slotNum = slotList[originInvSlot].slotNum;
            await EquipWeapon(originInvSlot, bagNum, slotNum);
        }
    }
    #endregion

    #region Button Functions
    #region ItemSlot Related Functions
    // Remove item from clicked inventorySlot
    private async Task RemoveItem(int invSlot)
    {
        // Hide right click panel
        panelScript.Hide();

        // Makes code more bearable
        int bagNum = slotList[invSlot].bagNum; int slotNum = slotList[invSlot].slotNum;

        inventory.EnqueueOperation(() => inventory.ARemoveItem(bagNum, slotNum), success =>
        {
            if (success)
            {
                UpdateSlot(invSlot);
                UpdateSecondaryElements();

                if(searchActive)
                    SearchItem(searchBar.text);
            }
        });
    }

    // Swap itemSlots positions from drag and drop
    private async Task SwapItems(int originInvSlot, int targetInvSlot)
    {
        // Makes code less bloated
        int originBagNum = slotList[originInvSlot].bagNum, originSlotNum = slotList[originInvSlot].slotNum;
        int targetBagNum = slotList[targetInvSlot].bagNum, targetSlotNum = slotList[targetInvSlot].slotNum;

        inventory.EnqueueOperation(() => inventory.ASwapInventorySlots(originBagNum, originSlotNum, targetBagNum, targetSlotNum), success =>
        {
            if (success)
            {
                UpdateSlot(originInvSlot);
                UpdateSlot(targetInvSlot);
                UpdateSecondaryElements();
            }
        });
    }

    // Combine stacks of same items from inventorySlots
    private async Task CombineItemStacks(int originInvSlot, int targetInvSlot)
    {
        // Makes code less bloated
        int originBagNum = slotList[originInvSlot].bagNum, originSlotNum = slotList[originInvSlot].slotNum;
        int targetBagNum = slotList[targetInvSlot].bagNum, targetSlotNum = slotList[targetInvSlot].slotNum;

        inventory.EnqueueOperation(() => inventory.ACombineItemStacks(originBagNum, originSlotNum, targetBagNum, targetSlotNum), success =>
        {
            if (success)
            {
                UpdateSlot(originInvSlot);
                UpdateSlot(targetInvSlot);
                UpdateSecondaryElements();
            }
        });
    }

    // Try to use consumable item from inventory
    private async Task UseConsumable(int invSlot, int invBagNum, int invSlotNum)
    {
        // Hide right click panel
        panelScript.Hide();

        inventory.EnqueueOperation(() => inventory.AConsumeItem(invBagNum, invSlotNum), success =>
        {
            if (success)
            {
                UpdateSecondaryElements();

                // Remove just emptied slot from search OR update slot normally
                if (searchActive && inventory.bags[invBagNum].bagInventory[invSlotNum].IsSlotNull())
                    SearchItem(searchBar.text);
                else
                    UpdateSlot(invSlot);
            }
        });
    }

    // Move Item to another Bag
    private async Task InsertItemToBag(int originInvSlot, int targetBagSlot)
    {
        // Makes code less bloated
        int bagNum = slotList[originInvSlot].bagNum, slotNum = slotList[originInvSlot].slotNum;

        // If trying to insert item to same bag; don't bother
        if (bagNum == targetBagSlot)
            return;

        inventory.EnqueueOperation(() => inventory.AInsertItem(slotNum, bagNum, targetBagSlot), success =>
        {
            if (success)
            {
                UpdateSlot(originInvSlot);
                UpdateSecondaryElements();

                if(searchActive)
                    SearchItem(searchBar.text);
            }
        });
    }

    // Replace equipped bag with one from inventory (Started from both invSlot and bagSlot originSlots)
    private async Task ReplaceBag(int originInvSlot, int originBagNum, int originSlotNum, int targetBagSlot)
    {
        // Can't replace bag with a bag that is contained in the bag you are trying to replace!
        if (originBagNum == targetBagSlot)
            return;

        inventory.EnqueueOperation(() => inventory.AReplaceBag(originBagNum, originSlotNum, targetBagSlot), success =>
        {
            if (success)
            {
                UpdateSlot(originInvSlot);
                UpdateBagSlot(targetBagSlot);
                UpdateSecondaryElements();
            }
        });
    }

    // Inspect bag contents in MessagePanel
    private void InspectInventorySlot(int invSlot)
    {
        // Hide right click panel
        panelScript.Hide();

        // Makes code less bloated
        int bagNum = slotList[invSlot].bagNum, slotNum = slotList[invSlot].slotNum;

        string message = $"{inventory.bags[bagNum].bagInventory[slotNum].item.ItemName}\n" +
            $"{inventory.bags[bagNum].bagInventory[slotNum].item.ItemDescription}\n{inventory.bags[bagNum].bagInventory[slotNum].item.ItemType}";

        // Display bag contents in MessagePanel
        MessagePanel.Instance.DisplayMessage(message);
    }
    #endregion

    #region BagSlot Related Functions
    // Try to equip bag from inventory
    private async Task EquipBag(int invSlot, int bagNum, int slotNum, int? targetBagSlot = null)
    {
        // Hide right click panel
        panelScript.Hide();

        inventory.EnqueueOperation(() => inventory.AEquipBag(bagNum, slotNum, targetBagSlot), result =>
        {
            if (result.success)
            {
                // Update UI elements
                UpdateSlot(invSlot);
                UpdateBagSlot(result.emptyBag);
                UpdateSecondaryElements();

                // If currently searching; remove search before generating bag contents
                CancelSearch();

                // Generate / open equipped bag
                currentActiveBag = result.emptyBag;
                GenerateItemSlots();
            }
        });
    }

    // Unequip bag from clicked bagSlot
    private async Task RemoveBag(int bagSlot, int? targetBagNum = null, int? targetSlotNum = null)
    {
        // Hide right click panel
        panelScript.Hide();

        inventory.EnqueueOperation(() => inventory.AUnequipBag(bagSlot, targetBagNum, targetSlotNum), result =>
        {
            if (result.success)
            {
                // Update UI elements
                UpdateBagSlot(bagSlot);
                UpdateSecondaryElements();

                // Currently searching; update all slots with new search (might find the removed bag)
                if (searchActive)
                {
                    // Current bag got removed; set mainBag as activeBag for when search ends
                    if (currentActiveBag == bagSlot)
                        currentActiveBag = 0;
                    SearchItem(searchBar.text);
                    return;
                }

                // Current bag got removed; generate mainBag and return
                if (currentActiveBag == bagSlot)
                {
                    currentActiveBag = 0;
                    GenerateItemSlots();
                    return;
                }

                // Removed bag got added to current bag; update the slot it got added to
                if (currentActiveBag == result.bagIndex)
                    UpdateSlot(result.slotIndex);
            }
        });
    }

    // Swap bag location or try to swap two bags based off of if targetSlot is nullBag
    private async Task SwapBags(int originBagSlot, int targetBagSlot)
    {
        // If trying to move mainBag; don't bother
        if (originBagSlot == 0 || targetBagSlot == 0)
            return;

        // If target bagSlot is null; just move the originBagSlot to that slot
        if (inventory.bags[targetBagSlot].IsBagNull())
        {
            inventory.EnqueueOperation(() => inventory.ASwapBagLocation(originBagSlot, targetBagSlot), success =>
            {
                if (success)
                {
                    // Update UI elements
                    UpdateBagSlot(originBagSlot);
                    UpdateBagSlot(targetBagSlot);
                    UpdateSecondaryElements();

                    // Currently searching; update all slots with new search (might have different item order)
                    if (searchActive) 
                    {
                        if (currentActiveBag == originBagSlot)
                            currentActiveBag = targetBagSlot;
                        SearchItem(searchBar.text);
                        return;
                    }

                    // If currentActiveBag was just moved; set active bag to targetBag
                    if (currentActiveBag == originBagSlot) 
                    {
                        currentActiveBag = targetBagSlot;
                        GenerateItemSlots();
                        return;
                    } 
                }
            });
        }
        // Else try to swap two equipped bags locations
        else
        {
            // Trying to swap same bag; don't bother
            if (originBagSlot == targetBagSlot)
                return;

            inventory.EnqueueOperation(() => inventory.ASwapBags(originBagSlot, targetBagSlot), success =>
            {
                if (success)
                {
                    // Update UI elements
                    UpdateBagSlot(originBagSlot);
                    UpdateBagSlot(targetBagSlot);
                    UpdateSecondaryElements();

                    // If either of the moved bags was currentActiveBag; set it's new index as currentActiveBag
                    if (currentActiveBag == originBagSlot)
                        currentActiveBag = targetBagSlot;
                    else if (currentActiveBag == targetBagSlot)
                        currentActiveBag = originBagSlot;

                    // Refresh search if active (might swap item order)
                    if (searchActive)
                    {
                        SearchItem(searchBar.text);
                        return;
                    }

                    // Generate / open new currentActiveBag if search not active
                    GenerateItemSlots();
                }
            });
        }
    }

    // Inspect bag contents in MessagePanel
    private void InspectBagContents(int bagSlot)
    {
        // Hide right click panel
        panelScript.Hide();

        // Makes code more bearable
        BagSlot temp = inventory.bags[bagSlot];

        string message = $"Bag #{bagSlot + 1}: {temp.bagItem.ItemName}";
        int i = 1;

        // Print each item and slot # in bag; show stacks if item is stackable as well
        foreach (InventorySlot item in temp.bagInventory)
        {
            if (item.IsSlotNull())
            {
                message += $"\nSlot #{i}: NULL";
            }
            else
            {
                message += $"\nSlot #{i}: {item.item.ItemName}";
                if (item.item.Stackable)
                    message += $" | Stack: {item.currentStack} / {item.item.MaxStack}";
            }
            i++;
        }

        // Display bag contents in MessagePanel
        MessagePanel.Instance.DisplayMessage(message, 15);
    }
    #endregion

    #region WeaponSlot Related Functions
    // Try to equip / swap weapon from inventory
    private async Task EquipWeapon(int invSlot, int bagNum, int slotNum)
    {
        // Hide right click panel
        panelScript.Hide();

        inventory.EnqueueOperation(() => inventory.AEquipWeapon(bagNum, slotNum), success =>
        {
            if (success)
            {
                UpdateWeaponSlot();
                UpdateSlot(invSlot);
                UpdateSecondaryElements();
            }
        });
    }

    // Uneqip weapon from weaponSlot
    private async Task RemoveWeapon(int? targetInvSlot = null, int? bagNum = null, int? slotNum = null)
    {
        // Hide right click panel
        panelScript.Hide();

        inventory.EnqueueOperation(() => inventory.AUnequipWeapon(bagNum, slotNum), success =>
        {
            UpdateWeaponSlot();
            UpdateSecondaryElements();

            // Moved to specific slot; update only that slot
            if (targetInvSlot.HasValue)
                UpdateSlot(targetInvSlot.Value);
            // Moved possibly anywhere in inventory; update all inventorySlots
            else
                UpdateSlotList();
        });
    }

    // Destroy equipped weapon
    private async Task DestroyWeapon()
    {
        // Hide right click panel
        panelScript.Hide();

        inventory.EnqueueOperation(() => inventory.ADestroyWeapon(), success =>
        {
            UpdateWeaponSlot();
            UpdateSecondaryElements();
        });
    }
    #endregion

    // Search for items that contain the text typed in searchBar (mininum length of 2)
    private void SearchItem(string search)
    {
        // Generate slots for each item in the inventory that contains searched item name 
        if(searchBar.text.Length >= 2)
        {
            searchActive = true;
            ClearInventorySlots();

            // Search entire inventory
            for (int b = 0; b < inventory.bags.Length; b++)
            {
                if(!inventory.bags[b].IsBagNull())
                {
                    for (int i = 0; i < inventory.bags[b].bagInventory.Length; i++)
                    {
                        // Stop searching if there's already max amount of slots my poor UI can handle
                        if (slotList.Count >= 20)
                            return;
                        if (inventory.bags[b].bagInventory[i].item.ItemName.Contains(search, System.StringComparison.OrdinalIgnoreCase))
                            GenerateItemSlot(b, i);
                    }
                }
            }
        }
        // Canceled search; restore current bag's slots
        else if (searchBar.text.Length <= 0)
        {
            // Search canceled from toggleBag function; don't generate itemSlots twice (toggleBag sets searchActive to FALSE)
            if (searchActive) 
            {
                GenerateItemSlots();
                searchActive = false;
            }
        }
    }

    // Cancel current search; used when currentActiveBag changes
    private void CancelSearch()
    {
        if (searchActive)
        {
            searchActive = false;
            searchBar.text = "";
        }
    }
    #endregion

    #region SlotGenerators
    // Generate single slot (used especially during search)
    private void GenerateItemSlot(int bagNum, int slotNum)
    {
        // Instantiate slotPrefab to ItemGrid
        GameObject uiSlot = Instantiate(slotPrefab, itemGrid.transform);
        InventoryUISlot slotScript = uiSlot.GetComponent<InventoryUISlot>();

        // Store item location to inventorySlot script and save index of slot in list for button functions
        int slot = slotList.Count;
        slotScript.SetSlotLocation(bagNum, slotNum, slot, this);
        slotList.Add(slotScript);

        // Add listeners/subscriabe events to buttons and update slot UI
        slotScript.itemButton.onClick.AddListener(async() => await LeftClickInventorySlot(slot));
        slotScript.itemButton.OnRightClick += async () => await RightClickInventorySlot(slot);
        slotScript.itemButton.OnDoubleClick += async () => await DoubleClickInventorySlot(slot);

        // Use updateSlotUI for the rest (icon, empty item check, stackable)
        UpdateSlot(slot);
    }

    // Generate inventorySlots from active bag (setup+bag button clicks)
    private void GenerateItemSlots()
    {
        // Destroy all inventoryUISlots from ItemGrid and empty the slotList
        ClearInventorySlots();

        // Generate all the slots from current active bag
        for (int i = 0; i < inventory.bags[currentActiveBag].bagInventory.Length; i++)
            GenerateItemSlot(currentActiveBag, i);
    }

    // Generate bagSlots (used during setup ONLY!)
    private void GenerateBagSlots()
    {
        for (int b = 1; b < inventory.bags.Length; b++)
        {
            // Required because buttonListener takes the reference of variable not the value for some reason
            int bagNum = b;

            // Instantiate bagPrefab to BagGrid
            GameObject uiSlot = Instantiate(bagSlotPrefab, bagGrid.transform);
            BagSlotUI slotScript = uiSlot.GetComponent<BagSlotUI>();

            // Store bag location in inventory to BagSlot script
            slotScript.SetSlotLocation(b, b, this);
            bagList.Add(slotScript);

            // Setup item icon and add listeners to buttons
            slotScript.itemButton.image.sprite = IconManager.Instance.GetIconByID(inventory.bags[bagNum].bagItem.IconID);
            slotScript.itemButton.onClick.AddListener(() => LeftClickBagSlot(bagNum));
            slotScript.itemButton.OnRightClick += async () => await RightClickBagSlot(bagNum);

            // Disable itemButton and hide removeButton+spaceCount if null item
            if (inventory.bags[bagNum].IsBagNull())
                SetBagSlotButtonInactive(bagNum);
        }
    }

    // Clear slot list and destroy all UI InventorySlots from ItemGrid
    private void ClearInventorySlots()
    {
        slotList.Clear();
        foreach (Transform child in itemGrid.transform) Destroy(child.gameObject);
    }

    // Enable buttons / interactability on inventory slot
    private void SetInventorySlotButtonActive(int slot)
    {
        slotList[slot].itemButton.interactable = true;
        slotList[slot].itemButton.image.color = Color.white;
        slotList[slot].dragReference.enabled = true;
    }

    // Disable buttons / interactability on inventory slot
    private void SetInventorySlotButtonInactive(int slot)
    {
        slotList[slot].itemButton.interactable = false;
        slotList[slot].stackBG.gameObject.SetActive(false);
        slotList[slot].itemButton.image.color = hiddenColor;
        slotList[slot].dragReference.enabled = false;
    }

    // Enable buttons / interactability on bag slot
    private void SetBagSlotButtonActive(int slot)
    {
        bagList[slot].itemButton.interactable = true;
        bagList[slot].stackBG.gameObject.SetActive(true);
        bagList[slot].itemButton.image.color = Color.white;
        bagList[slot].dragReference.enabled = true;
    }

    // Disable buttons / interactability on bag slot
    private void SetBagSlotButtonInactive(int slot)
    {
        bagList[slot].itemButton.interactable = false;
        bagList[slot].stackBG.gameObject.SetActive(false);
        bagList[slot].itemButton.image.color = hiddenColor;
        bagList[slot].dragReference.enabled = false;
    }

    // Toggle if SearchBar is selected
    private void ToggleSearchActive(bool active)
    {
        searchSelected = active;
    }

    // Duct tape solution to prevent player movement while typing on search =)
    // Proper way to do it would be to have UI keybinds mode for player as well
    public bool IsSearchSelected()
    {
        return searchSelected;
    }
    #endregion
}