using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Threading.Tasks;

public class Inventory : MonoBehaviour
{
    public BagSlot[] bags = new BagSlot[5];
    public InventoryUI inventoryUI;
    public WeaponSlot mainHandWeapon;

    public string userName;
    public float weightMax = 100f;
    public float weightCurrent;
    public TestDatabaseConnector dbConnector;

    private Queue<Func<Task>> operationQueue = new Queue<Func<Task>>();
    private bool isQueueProcessing = false;

    #region Function Queue
    // Enqueue function tasks to queue; when you Dequeue, it returns a function that returns a task(async function)
    public void EnqueueOperation<T>(Func<Task<T>> operation, Action<T> callback)
    {
        // Add unnamed async Task that runs code below when Dequeued
        operationQueue.Enqueue(async () =>
        {
            // Get the return value of operation / task
            T result = await operation();
            // Pass the result to the callback; inventoryUI origin
            callback?.Invoke(result);
            // Always return true to indicate the task is completed
        });

        // Queue not processing; start the queue
        if (!isQueueProcessing)
        {
            ProcessQueue();
        }
    }

    // For tasks that do not return a value (eg. async Task)
    public void EnqueueOperation(Func<Task> operation)
    {
        operationQueue.Enqueue(async () =>
        {
            await operation();
        });

        if (!isQueueProcessing)
        {
            ProcessQueue();
        }
    }

    // Process the function task queue
    private async void ProcessQueue()
    {
        // Prevent duplicate processQueues
        isQueueProcessing = true;

        // Run function tasks until the queue is empty
        while (operationQueue.Count > 0)
        {
            Func<Task> operation = operationQueue.Dequeue();
            try
            {
                //await Task.Delay(2000); //Fake lag for testing
                await operation();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Operation failed: {ex.Message}");
            }
        }
        isQueueProcessing = false;
    }
    #endregion

    // Generate inventory and bags at startup
    public async Task SetupInventory(TestDatabaseConnector database)
    {
        // Set reference to database script
        dbConnector = database;

        // Check if player inventory is iniatilized
        if (await dbConnector.IsInventoryInitialize())
        {
            // Get player inventory and convert it to game inventory format
            MongoInventory mongoInv = await dbConnector.GetInventory();
            if(mongoInv == null) 
            {
                MessagePanel.Instance.DisplayMessage("Failed to retrieve player inventory data.");
                return;
            }
            ConvertDatabaseToInventory(mongoInv);
        }
        else 
        {
            // Convert starter inventory to database format and insert it to database
            StartUpInventory();
            if(!await dbConnector.InsertInventory(this)) 
            {
                MessagePanel.Instance.DisplayMessage("Failed to insert player inventory data.");
                return;
            }
        }
        
        // Calculate current weight
        CalculateWeight();

        // Setup inventory construction and give starter gear for new players
        void StartUpInventory()
        {
            // Setup mainBag
            bags[0] = new BagSlot(ItemDataManager.Instance.GetItemByID(-1) as BagData);

            // Add starting gear for player: weapon, money, extra bag and apples
            bags[0].bagInventory[0] = new InventorySlot(ItemDataManager.Instance.GetItemByID(5));
            bags[0].bagInventory[1] = new InventorySlot(5, ItemDataManager.Instance.GetItemByID(1));
            bags[0].bagInventory[2] = new InventorySlot(6, ItemDataManager.Instance.GetItemByID(4));
            bags[0].bagInventory[3] = new InventorySlot(ItemDataManager.Instance.GetItemByID(3));

            // Setting null bagSlots
            for (int i = 1; i < bags.Length; i++)
            {
                bags[i] = new BagSlot(ItemDataManager.Instance.GetNullData<BagData>());
            }
            // Setting null weaponSlot
            mainHandWeapon = new WeaponSlot(ItemDataManager.Instance.GetNullData<MeleeWeaponData>());
        }
    }

    #region Inventory Functions
    #region InventorySlot Related functions
    // Add Item to inventory; contains 2 local functions: AddItemEmptySlot and AddItemStackableSlot
    public async Task AddItem(List<InventorySlot> lootList)
    {
        // Create a list of all emptySlots in Inventory
        List<(int bagNum, int slotNum)> emptySlots = GetListEmptySlots();

        // Use ItemID as key and make a double int list which shows bag number and slot number for slots that have the specific item
        Dictionary<int, List<(int bagNum, int slotNum)>> stackableSlots = GetDictionaryStackSlots();

        // If no empty AND stackable slots OR no weight space; dont even bother running operation
        if (emptySlots.Count <= 0 && stackableSlots.Count <= 0) 
        {
            MessagePanel.Instance.DisplayMessage("No space available for anything!");
            return;
        }

        // If weight as already full; don't even bother running operation
        if (weightCurrent >= weightMax)
        {
            MessagePanel.Instance.DisplayMessage("Inventoty weight is full!");
            return;
        }

        // Create list to store original stack values of lootList; used for rollback if Database update fails
        List<int> rollbackLoot = new List<int>();
        foreach (InventorySlot slot in lootList)
            rollbackLoot.Add(slot.currentStack);

        // Create dictionary to store original values of InventorySlots; used for rollback if Database update + used to select which database slots to update
        Dictionary<(int bagNum, int slotNum), InventorySlot> rollbackInventory = new Dictionary<(int bagNum, int slotNum), InventorySlot>();

        // Create list to store all added items as STRINGS (used to show what got added in UI inspector panel)
        List<string> itemsAdded = new List<string>();

        // slotNum and bagNum makes code more bearable + weightSpace stores how many stacks of item can be added weight wise
        int slotNum, bagNum, weightSpace;
        
        // Try to add all items in the lootList to inventory
        for (int i = 0; i < lootList.Count; i++)
        {
            // If there's no carry capacity for even single stack; no need to bother trying to add that item; go to next loot item
            weightSpace = Mathf.FloorToInt((weightMax - weightCurrent) / lootList[i].item.Weight);
            if (weightSpace <= 0)
                continue;

            // If item is stackable, check first if there's same item that has stack space before looking for empty inventory slot
            if (lootList[i].item.Stackable)
            {
                // Check if loot item's ItemID is in stackableSlots dictionary, if not; try adding to empty slots 
                if (stackableSlots.ContainsKey(lootList[i].item.ItemID))
                {
                    // Adding stackable item to inventory, if true; all loot fitted into stackable slots and go to next lootList item iteration (continue)
                    if (AddItemStackableSlot(stackableSlots[lootList[i].item.ItemID], lootList[i]))
                    {
                        // If there are no more stackable slots for current loot ItemID; remove the Key-Value for that item from dictionary
                        if (stackableSlots[lootList[i].item.ItemID].Count == 0)
                            stackableSlots.Remove(lootList[i].item.ItemID);
                        continue;
                    }
                    // Out of stackable slots for item; remove the Key/ItemID from dictionary and move on to emptySlots
                    else
                        stackableSlots.Remove(lootList[i].item.ItemID);
                }
                // If empty slots available; add loot to those
                if (emptySlots.Count > 0)
                {
                    // Keep adding items until either; the loot runs of out stacks or inventory runs out of empty slots / carry weight
                    while (lootList[i].currentStack > 0 && emptySlots.Count > 0 && weightSpace > 0)
                    {
                        // Add loot to the first empty slot in the list. Keep track of where and how much items are added
                        AddItemEmptySlot(lootList[i]);

                        // Calculate new weightSpace after addition
                        weightSpace = Mathf.FloorToInt((weightMax - weightCurrent) / lootList[i].item.Weight);

                        // Remove current "empty slot", but if it's stacks don't become max; add it to the stackable dictionary before removal
                        if (!bags[bagNum].bagInventory[slotNum].IsSlotStackFull())
                        {
                            if (stackableSlots.ContainsKey(lootList[i].item.ItemID))
                                stackableSlots[lootList[i].item.ItemID].Add((bagNum, slotNum));
                            else
                            {
                                stackableSlots.Add(lootList[i].item.ItemID, new List<(int bagNum, int slotNum)>());
                                stackableSlots[lootList[i].item.ItemID].Add((bagNum, slotNum));
                            }
                        }
                        emptySlots.RemoveAt(0);
                    }
                }
            }
            // Not stackable item, check only for empty inventory slots
            else
            {
                // Add item to empty slot in list if there's any available
                if (emptySlots.Count > 0)
                {
                    AddItemEmptySlot(lootList[i]);
                    emptySlots.RemoveAt(0);
                }
            }
        }

        // No items got added; we're done here
        if (itemsAdded.Count <= 0)
            return;

        // Update database; rollback inventory+lootList if failure
        bool result = await dbConnector.AUpdateItemAdd(rollbackInventory, bags);
        if (!result)
        {
            RollBackOperation();
            MessagePanel.Instance.DisplayMessage("Error: Database failed to update!");
            return;
        }

        // Show message of added items
        MessagePanel.Instance.DisplayMessageLoot(itemsAdded);

        // Update UI after operation
        inventoryUI.UpdateUIAddItem();

        #region Local Functions
        // Local function to add item to empty item slot
        void AddItemEmptySlot(InventorySlot loot)
        {
            // Get emptySlot location
            bagNum = emptySlots[0].bagNum; slotNum = emptySlots[0].slotNum;

            // Select addAmount with Mathf.Min
            int addAmount = Mathf.Min(loot.currentStack, Mathf.Min(loot.item.MaxStack, weightSpace));

            // Add original data of the correct inventorySlot to rollbackDictionary before it gets changed first time
            if (!rollbackInventory.ContainsKey((bagNum, slotNum)))
                rollbackInventory.Add((bagNum, slotNum), new InventorySlot(bags[bagNum].bagInventory[slotNum]));

            // Add item to correct bag and slot
            bags[bagNum].bagInventory[slotNum] = new InventorySlot(addAmount, loot.item);

            // Add operation details as string to list
            if (loot.item.Stackable)
                itemsAdded.Add($"{addAmount}x {loot.item.ItemName} - Bag: {bagNum + 1} | Slot: {slotNum + 1}");
            else
                itemsAdded.Add($"{loot.item.ItemName} - Bag: {bagNum + 1} | Slot: {slotNum + 1}");

            // Reduce stacks from loot and calculate new weight
            if (loot.item.Stackable)
                loot.currentStack -= addAmount;
            else
                loot.currentStack = 0;
            CalculateWeight();
        }

        // Local function to add item to stackable slot; returns true if entire loot stack gets added
        bool AddItemStackableSlot(List<(int bagNum, int slotNum)> stackList, InventorySlot loot)
        {
            // Used to count how much stack space slot has
            int stackSpace;
            // Used to calculate how many stacks can be carried weíght wise
            int addAmount;

            // Keep adding item stacks until the loot runs out (exit with return true) or stackable slots for the item run out (exit with return false) 
            while (stackList.Count > 0)
            {
                // End loop if inventory runs out of weight capacity and go to next loot item iteration
                if (weightSpace <= 0)
                    return true;
                 
                // Update item location data
                bagNum = stackList[0].bagNum; slotNum = stackList[0].slotNum;

                // Calculate stack space from correct bag and slot
                stackSpace = bags[bagNum].bagInventory[slotNum].item.MaxStack - bags[bagNum].bagInventory[slotNum].currentStack;

                // Select addAmount with Mathf.Min
                addAmount = Mathf.Min(stackSpace, Mathf.Min(loot.currentStack, weightSpace));

                // Add operation details as string to list
                itemsAdded.Add($"{addAmount}x {loot.item.ItemName} - Bag: {bagNum + 1} | Slot: {slotNum + 1}");

                // Add original data of the correct inventorySlot to rollbackDictionary before it gets changed
                if (!rollbackInventory.ContainsKey((bagNum, slotNum)))
                    rollbackInventory.Add((bagNum, slotNum), new InventorySlot(bags[bagNum].bagInventory[slotNum]));

                // Add stacks to correct bag and slot
                bags[bagNum].bagInventory[slotNum].currentStack += addAmount;

                // Reduce addAmount from weightSpace and loot; calculate current weight
                weightSpace -= addAmount;
                loot.currentStack -= addAmount;
                CalculateWeight();

                // Remove slot from list if it's full now
                if (addAmount == stackSpace)
                    stackList.RemoveAt(0);

                // Return true and go to next iteration if; loot is out of stacks
                if (loot.currentStack == 0)
                    return true;
            }
            return false;
        }

        // Revert all changed slots and lootlist stacks to original values before AddItem
        void RollBackOperation() 
        {
            foreach(KeyValuePair<(int bagNum, int slotNum), InventorySlot> slot in rollbackInventory)
                bags[slot.Key.bagNum].bagInventory[slot.Key.slotNum] = slot.Value;
            for (int i = 0; i < lootList.Count; i++)
                lootList[i].currentStack = rollbackLoot[i];
        }
        #endregion
    }

    // Remove/delete ítem from inventory
    public async Task<bool> ARemoveItem(int bagNum, int slotNum)
    {
        // Update database and locally if success
        if (await dbConnector.AUpdateItemRemove(bagNum, slotNum)) 
        {
            // Display message
            MessagePanel.Instance.DisplayMessage($"Removed item {bags[bagNum].bagInventory[slotNum].item.ItemName}!\nBag #{bagNum} | Slot #{slotNum}");

            // Set removed slot to null Instance and calculate new weight
            bags[bagNum].bagInventory[slotNum] = new InventorySlot(ItemDataManager.Instance.GetNullData<ItemData>());
            CalculateWeight();
            return true;
        }
        MessagePanel.Instance.DisplayMessage("Error: Database failed to update!");
        return false;
    }

    // Use consumable
    public async Task<bool> AConsumeItem(int bagNum, int slotNum)
    {
        // Update database and update inventory locally if succesful
        if (await dbConnector.AUpdateItemConsume(bags[bagNum].bagInventory[slotNum].currentStack, bagNum, slotNum))
        {
            // Convert consumable item to it's subclass to access the "effect string"
            ConsumableData convert = bags[bagNum].bagInventory[slotNum].item as ConsumableData;
            MessagePanel.Instance.DisplayMessage($"Using consumable {bags[bagNum].bagInventory[slotNum].item.ItemName}\nFrom Bag #{bagNum} | Slot #{slotNum}\n{convert.Effect}");

            // Update inventorySlot; if currentStack becomes 0; instance slot as null instance
            bags[bagNum].bagInventory[slotNum].currentStack -= 1;
            if (bags[bagNum].bagInventory[slotNum].currentStack == 0)
                bags[bagNum].bagInventory[slotNum] = new InventorySlot(ItemDataManager.Instance.GetNullData<ItemData>());
            CalculateWeight();
            return true;
        }
        MessagePanel.Instance.DisplayMessage("Error: Database failed to update!");
        return false;
    }

    // Swap two inventorySlots
    public async Task<bool> ASwapInventorySlots(int originBagNum, int originSlotNum, int targetBagNum, int targetSlotNum)
    {
        // Try to swap inventory slots in database, update locally if successful
        if (await dbConnector.AUpdateSwapInventorySlots(bags[originBagNum].bagInventory[originSlotNum], originBagNum, originSlotNum, bags[targetBagNum].bagInventory[targetSlotNum], targetBagNum, targetSlotNum))
        {
            MessagePanel.Instance.DisplayMessage($"Slots #{originSlotNum} and #{targetSlotNum} swapped.");
            InventorySlot temp = new InventorySlot(bags[originBagNum].bagInventory[originSlotNum]);
            bags[originBagNum].bagInventory[originSlotNum] = bags[targetBagNum].bagInventory[targetSlotNum];
            bags[targetBagNum].bagInventory[targetSlotNum] = temp;
            CalculateWeight();
            return true;
        }
        MessagePanel.Instance.DisplayMessage("Error: Database failed to update!");
        return false;
    }

    // Insert item to bag from another bag
    public async Task<bool> AInsertItem(int originSlotNum, int originBagNum, int targetBagNum)
    {
        // Check if target bag contains emptySlot
        if (!bags[targetBagNum].GetEmptySlot(out int targetSlotNum))
        {
            MessagePanel.Instance.DisplayMessage("No space available in the bag!");
            return false;
        }

        // Try to swap bag location in database; update locally if successful
        if (await dbConnector.AUpdateInsertItem(originBagNum, originSlotNum, targetBagNum, targetSlotNum, bags[originBagNum].bagInventory[originSlotNum]))
        {
            MessagePanel.Instance.DisplayMessage($"Moved {bags[originBagNum].bagInventory[originSlotNum].item.ItemName} \nFrom Bag #{originBagNum} to Bag #{targetBagNum}");
            bags[targetBagNum].bagInventory[targetSlotNum] = bags[originBagNum].bagInventory[originSlotNum];
            bags[originBagNum].bagInventory[originSlotNum] = new InventorySlot(ItemDataManager.Instance.GetNullData<BagData>());
            return true;
        }
        MessagePanel.Instance.DisplayMessage("Error: Database failed to update!");
        return false;
    }

    // Combine stacks of two same items
    public async Task<bool> ACombineItemStacks(int originBagNum, int originSlotNum, int targetBagNum, int targetSlotNum)
    {
        // Make code less bloated
        int maxStackCount = bags[originBagNum].bagInventory[originSlotNum].item.MaxStack;
        int originStackCount = bags[originBagNum].bagInventory[originSlotNum].currentStack;
        int targetStackCount = bags[targetBagNum].bagInventory[targetSlotNum].currentStack;

        // Count how many stacks you can move to target slot
        int stackCount = maxStackCount - targetStackCount;

        // Does originSlot have enough stacks
        if (stackCount > originStackCount)
            stackCount = originStackCount;

        Debug.Log("STACK COUNT: " + stackCount);
        // Try to combine stacks in database, if successful; update locally
        if (await dbConnector.AUpdateCombineItemStacks(originBagNum, originSlotNum, targetBagNum, targetSlotNum, originStackCount, targetStackCount, stackCount))
        {
            MessagePanel.Instance.DisplayMessage($"Moved {stackCount}x {bags[originBagNum].bagInventory[originSlotNum].item.ItemName}\n" +
                $"From Bag #{originBagNum} | Slot #{originSlotNum}\n to Bag #{targetBagNum} | Slot #{targetSlotNum}");
            bags[targetBagNum].bagInventory[targetSlotNum].currentStack += stackCount;

            // If originStack goes to zero; turn originSlot to emptySlot
            if (stackCount - originStackCount == 0)
                bags[originBagNum].bagInventory[originSlotNum] = new InventorySlot(ItemDataManager.Instance.GetNullData<ItemData>());
            else
                bags[originBagNum].bagInventory[originSlotNum].currentStack -= stackCount;
            return true;
        }
        MessagePanel.Instance.DisplayMessage("Error: Database failed to update!");
        return false;
    }
    #endregion

    #region Bagslot Related functions
    // Equip bag to bagSlot from inventory
    public async Task<(bool success, int emptyBag)> AEquipBag(int bagNum, int slotNum, int? targetBagSlot = null)
    {
        // Declare emptyBag variable
            int emptyBag;

        // Dragged inventorySlot to bag slot; use the passed bagSlot as location to set bag, if it's empty
        if (targetBagSlot.HasValue) 
        {
            if (!bags[targetBagSlot.Value].IsBagNull())
                return (false, 0);
            emptyBag = targetBagSlot.Value;
        }
        // Check for empty BagSlots and get index for empty slot
        else if (!GetEmptyBagSlot(out emptyBag)) 
        {
            MessagePanel.Instance.DisplayMessage($"No empty bag slots available!");
            return (false, 0);
        }
        Debug.Log("EmptyBag: " + emptyBag);

        // Update database and update inventory locally if succesful
        if (await dbConnector.AUpdateBagEquip(bags[bagNum].bagInventory[slotNum].item as BagData, emptyBag, bagNum, slotNum))
        {
            // Display message
            MessagePanel.Instance.DisplayMessage($"Equipped {bags[bagNum].bagInventory[slotNum].item.ItemName}!\nBag #{bagNum} | Slot #{slotNum}\nEquipped to bagSlot #{emptyBag}");

            // Move bagItem to bagSlot, instantiate null inventorySlot and update weight
            bags[emptyBag] = new BagSlot(bags[bagNum].bagInventory[slotNum].item as BagData);
            bags[bagNum].bagInventory[slotNum] = new InventorySlot(ItemDataManager.Instance.GetNullData<ItemData>());
            CalculateWeight();
            return (true, emptyBag);
        }
        MessagePanel.Instance.SendMessage("Error: Database failed to update!");
        return (false, 0);
    }

    // Unequip bag from bagSlot
    public async Task<(bool success, int bagIndex, int slotIndex)> AUnequipBag(int bagSlot, int? targetBagNum = null, int? targetSlotNum = null)
    {
        // Panic lock for main bag
        if (bagSlot == 0)
        {
            MessagePanel.Instance.DisplayMessage("Error: Can't remove main bag!");
            return (false, 0, 0);
        }

        // Is there enough weight space to carry the bag
        if (bags[bagSlot].bagItem.Weight + weightCurrent > weightMax)
        {
            MessagePanel.Instance.DisplayMessage("Not enough weight space to carry the bag!");
            return (false, 0, 0);
        }
            
        // Check if bag is empty
        if (!bags[bagSlot].IsBagEmpty())
        {
            MessagePanel.Instance.DisplayMessage("Bag is not empty! Can't be unequipped!");
            return (false, 0, 0);
        }

        // Declare bagIndex and slotIndex values
        int bagIndex, slotIndex;

        // Dragged bagSlot to inventory slot; use the passed inventory location as empty slot
        if(targetBagNum.HasValue && targetSlotNum.HasValue)
        {
            // If the target slot is not empty; return
            if (bags[targetBagNum.Value].bagInventory[targetSlotNum.Value].item.ItemID != 0)
                return (false, 0, 0);
                    
            bagIndex = targetBagNum.Value;
            slotIndex = targetSlotNum.Value;
        }
        // Find empty inventorySlot if possible
        else if (!GetEmptySlot(out bagIndex, out slotIndex))
        {
            MessagePanel.Instance.DisplayMessage("No inventory space available!");
            return (false, 0, 0);
        }

        // Update database and update inventory locally if succesful
        if (await dbConnector.AUpdateBagRemove(bags[bagSlot].bagItem.ItemID, bagSlot, bagIndex, slotIndex))
        {
            // Display message
            MessagePanel.Instance.DisplayMessage($"Unequipped {bags[bagSlot].bagItem.ItemName}!\nBag Slot #{bagSlot}\nBag moved to Bag #{bagIndex} | Slot #{slotIndex}");

            // Move bagItem to inventorySlot, instantiate null bagSlot
            bags[bagIndex].bagInventory[slotIndex] = new InventorySlot(bags[bagSlot].bagItem);
            bags[bagSlot] = new BagSlot(ItemDataManager.Instance.GetNullData<BagData>());
            CalculateWeight();
            return (true, bagIndex, slotIndex);
        }
        MessagePanel.Instance.DisplayMessage("Error: Database failed to update!");
        return (false, 0, 0);
    }

    // Replace equipped bag with one from inventory
    public async Task<bool> AReplaceBag(int bagNum, int slotNum, int targetBagNum)
    {
        // Is there enough weight space to carry the swapped bag
        if (weightCurrent + bags[targetBagNum].bagItem.Weight - bags[bagNum].bagInventory[slotNum].item.Weight > weightMax)
        {
            MessagePanel.Instance.DisplayMessage("Not enough weight space to replace the bag!");
            return false;
        }

        // Check if bag can carry the contents of the equipped bag
        if (!IsBagReplaceable(bags[targetBagNum], bags[bagNum].bagInventory[slotNum].item as BagData))
        {
            MessagePanel.Instance.DisplayMessage("Bag can't carry all items inside the equipped bag!");
            return false;
        }

        // Try to swap two bags location in database; update locally if successful
        if (await dbConnector.AUpdateReplaceBag(bagNum, slotNum, targetBagNum, bags[targetBagNum].bagItem.ItemID,
            new MongoBagSlot(bags[targetBagNum].bagInventory, bags[bagNum].bagInventory[slotNum].item as BagData)))
        {
            MessagePanel.Instance.DisplayMessage($"Replaced Bag #{targetBagNum} with {bags[bagNum].bagInventory[slotNum].item.ItemName}\nFrom Bag #{bagNum} | Slot #{slotNum}");
            BagSlot temp = new BagSlot(bags[targetBagNum].bagInventory, bags[bagNum].bagInventory[slotNum].item as BagData);
            bags[bagNum].bagInventory[slotNum] = new InventorySlot(bags[targetBagNum].bagItem);
            bags[targetBagNum] = temp;
            CalculateWeight();
            return true;
        }
        MessagePanel.Instance.DisplayMessage("Error: Database failed to update!");
        return false;
    }

    // Try to swap bags; either two equipped bags or equipped bag to bag from inventory
    public async Task<bool> ASwapBags(int originBagNum, int targetBagNum)
    {
        // Try to swap two bags location in database; update locally if successful
        if (await dbConnector.AUpdateSwapBags(originBagNum, targetBagNum, new MongoBagSlot(bags[originBagNum]), new MongoBagSlot(bags[targetBagNum])))
        {
            MessagePanel.Instance.DisplayMessage($"Swapped location of bags #{originBagNum} and #{targetBagNum}");
            BagSlot temp = bags[originBagNum];
            bags[originBagNum] = bags[targetBagNum];
            bags[targetBagNum] = temp;
            return true;
        }
        MessagePanel.Instance.DisplayMessage("Error: Database failed to update!");
        return false;
    }

    // Swap bag to different empty slot
    public async Task<bool> ASwapBagLocation(int originBagNum, int targetBagNum)
    {
        // Try to swap bag location in database; update locally if successful
        if (await dbConnector.AUpdateSwapBagLocation(originBagNum, targetBagNum, new MongoBagSlot(bags[originBagNum])))
        {
            MessagePanel.Instance.DisplayMessage($"Moved BagSlot #{originBagNum} to BagSlot #{targetBagNum}");
            bags[targetBagNum] = bags[originBagNum];
            bags[originBagNum] = new BagSlot(ItemDataManager.Instance.GetNullData<BagData>());
            return true;
        }
        MessagePanel.Instance.DisplayMessage("Error: Database failed to update!");
        return false;
    }
    #endregion

    #region WeaponSlot Related functions
    // Equip weapon from inventory to weaponSlot
    public async Task<bool> AEquipWeapon(int bagNum, int slotNum)
    {
        // Try to equipWeapon in database, update locally if successful
        if (await dbConnector.AUpdateWeaponEquip(bags[bagNum].bagInventory[slotNum], mainHandWeapon.weapon.ItemID, bagNum, slotNum))
        {
            // Get playerController script to access player hand
            PlayerController player = GetComponentInParent<PlayerController>();

            // Empty weaponSlot; move weapon from inventory to weaponSlot and instantiate equipped weapon
            if (mainHandWeapon.IsWeaponNull())
            {
                MessagePanel.Instance.DisplayMessage($"Equipped weapon {bags[bagNum].bagInventory[slotNum].item.ItemName}!\nFrom Bag #{bagNum} | Slot #{slotNum}");
                mainHandWeapon = new WeaponSlot(bags[bagNum].bagInventory[slotNum].item as WeaponData);
                bags[bagNum].bagInventory[slotNum] = new InventorySlot(ItemDataManager.Instance.GetNullData<ItemData>());
                Instantiate(mainHandWeapon.weapon.WeaponModel, player.hand.transform);
            }
            // Already weapon equipped; swap weapons between inventory slot and weapon slot, destroy current weapon and instantiate new weapon
            else
            {
                // Is there enough weight space to swap weapons
                if(weightCurrent - mainHandWeapon.weapon.Weight + bags[bagNum].bagInventory[slotNum].item.Weight > weightMax) 
                {
                    MessagePanel.Instance.DisplayMessage("Not enough weight space to swap weapons!");
                    return false;
                }

                MessagePanel.Instance.DisplayMessage($"Equipped weapon {bags[bagNum].bagInventory[slotNum].item.ItemName}!\n" +
                    $"Unequipped weapon {mainHandWeapon.weapon.ItemName}!\n To Bag #{bagNum} | Slot #{slotNum}");
                    
                // Create temporary copy of current weapon to swap weapons
                ItemData tempItem = mainHandWeapon.weapon;
                mainHandWeapon = new WeaponSlot(bags[bagNum].bagInventory[slotNum].item as WeaponData);
                bags[bagNum].bagInventory[slotNum] = new InventorySlot(tempItem);
                   
                // Destroy current weapon and instantiate the new weapon
                foreach (Transform child in player.hand.transform)
                    Destroy(child.gameObject);
                Instantiate(mainHandWeapon.weapon.WeaponModel, player.hand.transform);
            }
            CalculateWeight();
            return true;
        }
        MessagePanel.Instance.DisplayMessage("Error: Database failed to update!");
        return false;
    }

    // Unequip weapon from weaponSlot to inventory (if space)
    public async Task<bool> AUnequipWeapon(int? targetBagNum = null, int? targetSlotNum = null)
    {
        // Is there enough weight space to carry the weapon
        if (mainHandWeapon.weapon.Weight + weightCurrent > weightMax) 
        {
            MessagePanel.Instance.DisplayMessage("Not enough weight space to carry the weapon!");
            return false;
        }

        // Declare bagIndex and slotIndex values
        int bagIndex, slotIndex;

        // Dragged bagSlot to inventory slot; use the passed inventory location as empty slot
        if (targetBagNum.HasValue && targetSlotNum.HasValue)
        {
            // If the target slot is not empty; return
            if (bags[targetBagNum.Value].bagInventory[targetSlotNum.Value].item.ItemID != 0)
                return false;

            bagIndex = targetBagNum.Value;
            slotIndex = targetSlotNum.Value;
        }
        // Find empty inventorySlot if possible
        else if (!GetEmptySlot(out bagIndex, out slotIndex))
        {
            MessagePanel.Instance.DisplayMessage("Inventory is full! Can't unequip weapon!");
            return false;
        }

        // Try to unequip weapon in database, update locally if successful
        if (await dbConnector.AUpdateWeaponUnequip(mainHandWeapon.weapon.ItemID, bagIndex, slotIndex))
        {
            MessagePanel.Instance.DisplayMessage($"Unequipped {mainHandWeapon.weapon.ItemName}!\nTo Bag #{bagIndex} | Slot #{slotIndex}");
            bags[bagIndex].bagInventory[slotIndex] = new InventorySlot(mainHandWeapon.weapon);
            mainHandWeapon = new WeaponSlot(ItemDataManager.Instance.GetNullData<MeleeWeaponData>());
            CalculateWeight();

            // Destroy removed weapon in player
            PlayerController player = GetComponentInParent<PlayerController>();
            foreach (Transform child in player.hand.transform)
                Destroy(child.gameObject);
            return true;
        }
        MessagePanel.Instance.DisplayMessage("Error: Database failed to update!");
        return false;
    }

    // Destroy equipped weapon instantly without adding it to inventory
    public async Task<bool> ADestroyWeapon()
    {
        // Try to destroy weapon in database, update locally if successful
        if (await dbConnector.AUpdateWeaponDestroy())
        {
            MessagePanel.Instance.DisplayMessage($"Equipped weapon destroyed!");
            mainHandWeapon = new WeaponSlot(ItemDataManager.Instance.GetNullData<MeleeWeaponData>());
            CalculateWeight();

            // Destroy removed weapon in player
            PlayerController player = GetComponentInParent<PlayerController>();
            foreach (Transform child in player.hand.transform)
                Destroy(child.gameObject);
            return true;
        }
        MessagePanel.Instance.DisplayMessage("Error: Database failed to update!");
        return false;
    }
    #endregion
    #endregion

    #region Private Helper Functions
    // Convert MongoInventory data to Inventory format
    private void ConvertDatabaseToInventory(MongoInventory mongoInv)
    {
        // Loop through all bags and inventorySlots
        for(int b = 0; b < mongoInv.bags.Length; b++) 
        {
            // Empty bag; just instantiate nullBag
            if (mongoInv.bags[b].bagItemID == 0)
                bags[b] = new BagSlot(ItemDataManager.Instance.GetNullData<BagData>());

            // Not empty bag; copy contents from document to bag
            else
            {
                bags[b] = new BagSlot(ItemDataManager.Instance.GetItemByID(mongoInv.bags[b].bagItemID) as BagData);
                for(int i = 0; i < bags[b].bagInventory.Length; i++) 
                {
                    // Empty itemSlot; instantiate nullItem
                    if (mongoInv.bags[b].bagInventory[i].itemID == 0)
                        bags[b].bagInventory[i] = new InventorySlot(ItemDataManager.Instance.GetNullData<ItemData>());

                    // Not empty itemSlot; copy contents from document to slot
                    else
                    {
                        bags[b].bagInventory[i].currentStack = mongoInv.bags[b].bagInventory[i].currentStack;
                        bags[b].bagInventory[i].item = ItemDataManager.Instance.GetItemByID(mongoInv.bags[b].bagInventory[i].itemID);
                    }
                }
            }
        }

        // Copy weapon from document and instantiate if necessary (either nullInstance or copy data)
        if(mongoInv.mainHandWeapon.weaponID == 0)
            mainHandWeapon = new WeaponSlot(ItemDataManager.Instance.GetNullData<MeleeWeaponData>());
        else 
        {
            mainHandWeapon = new WeaponSlot(ItemDataManager.Instance.GetItemByID(mongoInv.mainHandWeapon.weaponID) as WeaponData);
            PlayerController player = GetComponentInParent<PlayerController>();
            Instantiate(mainHandWeapon.weapon.WeaponModel, player.hand.transform);
        }
    }

    // Get a list off all empty inventorySlots from entire inventory
    private List<(int, int)> GetListEmptySlots()
    {
        List<(int bagNum, int slotNum)> result = new List<(int bagNum, int slotNum)>();

        // Loop through all bags
        for(int b = 0; b < bags.Length; b++) 
        {
            // Only check bags that are not NULL
            if(!bags[b].IsBagNull()) 
            {
                for (int i = 0; i < bags[b].bagInventory.Length; i++)
                {
                    if (bags[b].bagInventory[i].IsSlotNull())
                        result.Add((b, i));
                }
            }
        }
        return result;
    }

    // Get dictionary of lists that store location of inventorySlots(Key = ItemID, Value = bagNum+slotNum list)
    private Dictionary<int, List<(int, int)>> GetDictionaryStackSlots()
    {
        Dictionary<int, List<(int bagNum, int slotNum)>> result = new Dictionary<int, List<(int bagNum, int slotNum)>>();
        
        // Loop through all bags
        for (int b = 0; b < bags.Length; b++)
        {
            // Only check bags that are not NULL
            if (!bags[b].IsBagNull())
            {
                for (int i = 0; i < bags[b].bagInventory.Length; i++)
                {
                    // Check if slot item is empty and has max stack, no need to add either of those to dictionary
                    if (!bags[b].bagInventory[i].IsSlotNull() && !bags[b].bagInventory[i].IsSlotStackFull())
                    {
                        // Check if key already exists; Add new value to Key's value list and create new Key-Value if false and add to list
                        if (result.ContainsKey(bags[b].bagInventory[i].item.ItemID))
                            result[bags[b].bagInventory[i].item.ItemID].Add((b, i));
                        else 
                        {
                            result.Add(bags[b].bagInventory[i].item.ItemID, new List<(int bagNum, int slotNum)>());
                            result[bags[b].bagInventory[i].item.ItemID].Add((b, i));
                        }
                    }
                }
            }
        }
        return result;
    }

    // Find first empty slot in entire inventory
    private bool GetEmptySlot(out int bagNum, out int slotNum)
    {
        bagNum = 0; slotNum = 0;
        for (int b = 0; b < bags.Length; b++)
        {
            if (!bags[b].IsBagNull())
            {
                for (int i = 0; i < bags[b].bagInventory.Length; i++)
                {
                    if (bags[b].bagInventory[i].IsSlotNull())
                    {
                        slotNum = i;
                        bagNum = b;
                        return true;
                    }
                }
            }
        }
        return false;
    }

    // Check if there's empty bagSlots and take reference of empty bag index
    private bool GetEmptyBagSlot(out int bagNum)
    {
        bagNum = 0;
        for (int i = 0; i < bags.Length; i++)
        {
            if (bags[i].IsBagNull())
            {
                bagNum = i;
                return true;
            }
        }
        return false;
    }

    // Check if bag has enough space to replace
    private bool IsBagReplaceable(BagSlot equippedBag, BagData bagItem) 
    {
        // If equipped bag is empty; instant success
        if (equippedBag.IsBagEmpty())
            return true;

        // If bag you're trying to equip is bigger than current equipped bag; instant success
        if (bagItem.BagSize >= equippedBag.bagItem.BagSize)
            return true;

        // If bag you're trying to equip can carry all the items currently in equipped bag; success
        if (equippedBag.bagItem.BagSize - equippedBag.GetBagSpaceCount() <= bagItem.BagSize)
            return true;

        // Can't fit the items inside bag to new bag; failure
        return false;
    }

    // Calculate current total weight of inventory
    private void CalculateWeight()
    {
        float weight = 0;
        
        // Count inventory weight
        for (int b = 0; b < bags.Length; b++)
        {
            if (!bags[b].IsBagNull())
            {
                for (int i = 0; i < bags[b].bagInventory.Length; i++)
                {
                    if (!bags[b].bagInventory[i].IsSlotNull()) 
                    {
                        if (bags[b].bagInventory[i].item.Stackable)
                            weight += bags[b].bagInventory[i].item.Weight * bags[b].bagInventory[i].currentStack;
                        else
                            weight += bags[b].bagInventory[i].item.Weight;
                    }
                }
            }
        }
        weightCurrent = (float)Math.Round(weight, 3);
    }
    #endregion
}