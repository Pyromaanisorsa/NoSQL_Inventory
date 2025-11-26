using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading.Tasks;
using System;

public class TestDatabaseConnector : MonoBehaviour
{
    private MongoClient client;
    private IMongoDatabase database;
    private IMongoCollection<MongoPlayer> playerCollection;
    private IMongoCollection<MongoInventory> inventoryCollection;
    private FilterDefinition<MongoInventory> filter;
    //private int maxRetries = 3;
    private ObjectId inventoryID;

    // Setup connection to the database and collection
    void Awake()
    {
        // Connection string to the MongoDB Atlas 
        string connectionString = "your-connection-string-to-your-mongoDB-cluster";

        // Initialize Mongoclient
        client = new MongoClient(connectionString);

        // Connect to the database
        database = client.GetDatabase("noSQL_Inventory");

        // Access players collection
        playerCollection = database.GetCollection<MongoPlayer>("players");

        // Access inventories collection
        inventoryCollection = database.GetCollection<MongoInventory>("inventories");

        // Add indexation to playerCollection "username" fields.
        // This needs to be only run once and the indexation stays in the collection for all documents
        //CreateUsernameIndex();

        // Create indexation for username field in MongoPlayer documents for faster searching
        void CreateUsernameIndex() 
        {
            // Create ascending keydefination for usernames
            var indexKeysDefinition = Builders<MongoPlayer>.IndexKeys.Ascending(doc => doc.username);

            // Ensure unique usernames
            var indexOptions = new CreateIndexOptions { Unique = true };

            // Define the index model for the username field
            var indexModel = new CreateIndexModel<MongoPlayer>(indexKeysDefinition, indexOptions);

            // Create the index
            playerCollection.Indexes.CreateOne(indexModel);
            Debug.Log("USERNAME INDEX CREATING COMPLETE");
        }
    }

    // Create and insert NEW player+inventory documents to the database
    public async Task<bool> RegisterPlayer(string username, string password)
    {
        // Create filter to find document for inputted username
        FilterDefinition<MongoPlayer> playerFilter = Builders<MongoPlayer>.Filter.Eq(doc => doc.username, username);

        // Check if user with inputted name already exists; don't want duplicates
        if (await playerCollection.Find(playerFilter).AnyAsync()) 
        {
            MessagePanel.Instance.DisplayMessage("Username is unavailable!");
            return false;
        }

        // Create new MongoInventory
        MongoInventory mongoInventory = new MongoInventory(ObjectId.GenerateNewId());

        // Insert the mongoInventory document into the inventory collection
        await inventoryCollection.InsertOneAsync(mongoInventory);

        // Store the new inventory _id received from database
        inventoryID = mongoInventory.Id;

        // Insert new player document to collection
        await playerCollection.InsertOneAsync(new MongoPlayer(username, password, inventoryID));

        // Create filter to find player's inventory document with ínventory document _id for future use
        filter = Builders<MongoInventory>.Filter.Eq("_id", inventoryID);
        MessagePanel.Instance.DisplayMessage("Registration successful.");
        return true;
    }

    // Retrive player inventory document _id from the database
    public async Task<bool> LoginPlayer(string username, string password)
    {
        // Create filter and find document for inputted username
        FilterDefinition<MongoPlayer> playerFilter = Builders<MongoPlayer>.Filter.Eq(doc => doc.username, username);
        MongoPlayer playerDoc = await playerCollection.Find(playerFilter).FirstOrDefaultAsync();

        // Check if player document for inputted username exists
        if (playerDoc == null) 
        {
            MessagePanel.Instance.DisplayMessage("Username does not exist.");
            return false;
        }

        // Check if inputed password is correct
        if (playerDoc.password != password)
        {
            MessagePanel.Instance.DisplayMessage("Password is invalid.");
            return false;
        }

        // Store inventory _id and create filter to find player's inventory document with ínventory document _id for future use
        inventoryID = playerDoc.inventoryID;
        filter = Builders<MongoInventory>.Filter.Eq("_id", inventoryID);
        MessagePanel.Instance.DisplayMessage("Login succesful.");
        return true;
    }

    // Check if current player's inventory has been initialized, ready for use (if mainBag is still NULL, it's good enough of a check)
    public async Task<bool> IsInventoryInitialize() 
    {
        // Filter that finds current player's inventory document and checks if the bags[] object value is null
        FilterDefinition<MongoInventory> invFilter = Builders<MongoInventory>.Filter.And(
            Builders<MongoInventory>.Filter.Eq("_id", inventoryID),
            Builders<MongoInventory>.Filter.Eq("bags", (object)null)
        );

        // Return opposite boolean if document was found (eg. document found => true, meaning not initialized, so return false)
        return !await inventoryCollection.Find(invFilter).AnyAsync();
    }

    // Replace placeholder inventory with starting inventory for new player inventory
    public async Task<bool> InsertInventory(Inventory inventory) 
    {
        // Convert starter inventory to MongoInventory format and use it to update everything except _id for the inventory document
        MongoInventory mongoInventory = new MongoInventory(inventory);
        var update = Builders<MongoInventory>.Update
            .Set("bags", mongoInventory.bags)
            .Set("mainHandWeapon", mongoInventory.mainHandWeapon);

        // Update the document and check if the update was successful
        var result = await inventoryCollection.UpdateOneAsync(filter, update);
        if (result.MatchedCount > 0)
            return true;
        return false;
    }

    // Get the player inventory to load player inventory
    public async Task<MongoInventory> GetInventory()
    {
        return await inventoryCollection.Find(filter).FirstOrDefaultAsync();
    }

    // Update inventorySlot in the player inventory document, UNUSED LEFTOVER
    public async Task<bool> AUpdateItemSlot(InventorySlot slot, int bagNum, int slotNum)
    {
        // Set location to what to update in Document
        var update = Builders<MongoInventory>.Update
            .Set($"bags.{bagNum}.bagInventory.{slotNum}.itemID", slot.item.ItemID)
            .Set($"bags.{bagNum}.bagInventory.{slotNum}.currentStack", slot.currentStack);

        // Update the document
        var result = await inventoryCollection.UpdateOneAsync(filter, update);

        // Return boolean if operation was succesful or not
        if (result.ModifiedCount > 0)
            return true;
        return false;
    }

    // Update inventorySlot in the player inventory document
    public async Task<bool> AUpdateItemConsume(int currentStack, int bagNum, int slotNum)
    {
        // Initialize UpdateDefination; makes code more bearable with if-else
        UpdateDefinition<MongoInventory> update;

        // Set location to what to update in Document; if slot runs out of stacks; null that item. Else reduce stack by 1
        if (currentStack - 1 <= 0)
        {
            update = Builders<MongoInventory>.Update
           .Set($"bags.{bagNum}.bagInventory.{slotNum}.itemID", 0)
           .Set($"bags.{bagNum}.bagInventory.{slotNum}.currentStack", 0);
        }
        else
        {
            update = Builders<MongoInventory>.Update
           .Set($"bags.{bagNum}.bagInventory.{slotNum}.currentStack", currentStack - 1);
        }

        // Update the document
        var result = await inventoryCollection.UpdateOneAsync(filter, update);

        // Return boolean if operation was succesful or not
        if (result.ModifiedCount > 0)
            return true;
        return false;
    }

    // Update inventorySlot in the player inventory document
    public async Task<bool> AUpdateItemRemove(int bagNum, int slotNum)
    {
        // Set location to what to update in Document; if slot runs out of stacks; null that item. Else reduce stack by 1
        var update = Builders<MongoInventory>.Update.Set($"bags.{bagNum}.bagInventory.{slotNum}", new MongoInventorySlot());

        // Update the document
        var result = await inventoryCollection.UpdateOneAsync(filter, update);

        // Return boolean if operation was succesful or not
        if (result.ModifiedCount > 0)
            return true;
        return false;
    }

    // Update multiple inventorySlots (inventory.addItem)
    public async Task<bool> AUpdateItemAdd(Dictionary<(int bagNum, int slotNum), InventorySlot> slots, BagSlot[] bags)
    {
        var bulkUpdate = new List<WriteModel<MongoInventory>>();
        int bagIndex, slotIndex;

        // Loop through all added slots and create update for document for each
        foreach (KeyValuePair<(int bagNum, int slotNum), InventorySlot> slot in slots)
        {
            bagIndex = slot.Key.bagNum; slotIndex = slot.Key.slotNum;

            // Set location to what to update in Document
            var update = Builders<MongoInventory>.Update
                .Set($"bags.{bagIndex}.bagInventory.{slotIndex}.itemID", bags[bagIndex].bagInventory[slotIndex].item.ItemID)
                .Set($"bags.{bagIndex}.bagInventory.{slotIndex}.currentStack", bags[bagIndex].bagInventory[slotIndex].currentStack);

            bulkUpdate.Add(new UpdateOneModel<MongoInventory>(filter, update));
        }

        //Update the document
        var result = await inventoryCollection.BulkWriteAsync(bulkUpdate);

        // Return boolean if operation was succesful or not
        if (result.ModifiedCount == bulkUpdate.Count)
            return true;
        return false;
    }

    // Update swapping two inventorySlots
    public async Task<bool> AUpdateSwapInventorySlots(InventorySlot originSlot, int originBagNum, int originSlotNum, InventorySlot targetSlot, int targetBagNum, int targetSlotNum)
    {
        // Set location to what to update in Document
        var update = Builders<MongoInventory>.Update
            .Set($"bags.{originBagNum}.bagInventory.{originSlotNum}.itemID", targetSlot.item.ItemID)
            .Set($"bags.{originBagNum}.bagInventory.{originSlotNum}.currentStack", targetSlot.currentStack)
            .Set($"bags.{targetBagNum}.bagInventory.{targetSlotNum}.itemID", originSlot.item.ItemID)
            .Set($"bags.{targetBagNum}.bagInventory.{targetSlotNum}.currentStack", originSlot.currentStack);

        // Update the document
        var result = await inventoryCollection.UpdateOneAsync(filter, update);

        // Return boolean if operation was succesful or not
        if (result.ModifiedCount > 0)
            return true;
        return false;
    }

    // Update inserting item to bag
    public async Task<bool> AUpdateInsertItem(int originBagNum, int originSlotNum, int targetBagNum, int targetSlotNum, InventorySlot originSlot)
    {
        // Set location to what to update in Document
        var update = Builders<MongoInventory>.Update
            .Set($"bags.{originBagNum}.bagInventory.{originSlotNum}.itemID", 0)
            .Set($"bags.{originBagNum}.bagInventory.{originSlotNum}.currentStack", 0)
            .Set($"bags.{targetBagNum}.bagInventory.{targetSlotNum}.itemID", originSlot.item.ItemID)
            .Set($"bags.{targetBagNum}.bagInventory.{targetSlotNum}.currentStack", originSlot.currentStack);

        // Update the document
        var result = await inventoryCollection.UpdateOneAsync(filter, update);

        // Return boolean if operation was succesful or not
        if (result.ModifiedCount > 0)
            return true;
        return false;
    }

    // Update combining stacks of two same items
    public async Task<bool> AUpdateCombineItemStacks(int originBagNum, int originSlotNum, int targetBagNum, int targetSlotNum, int originStackCount, int targetStackCount, int movedStackCount)
    {
        UpdateDefinition<MongoInventory> update;

        // Set location to what to update in Document; if originSlot loses all stacks, make it null/emptySlot
        if (originStackCount - movedStackCount == 0)
        {
            update = Builders<MongoInventory>.Update
            .Set($"bags.{originBagNum}.bagInventory.{originSlotNum}", new MongoInventorySlot())
            .Set($"bags.{targetBagNum}.bagInventory.{targetSlotNum}.currentStack", targetStackCount + movedStackCount);
        }
        else
        {
            update = Builders<MongoInventory>.Update
            .Set($"bags.{originBagNum}.bagInventory.{originSlotNum}.currentStack", originStackCount - movedStackCount)
            .Set($"bags.{targetBagNum}.bagInventory.{targetSlotNum}.currentStack", targetStackCount + movedStackCount);
        }

        // Update the document
        var result = await inventoryCollection.UpdateOneAsync(filter, update);

        // Return boolean if operation was succesful or not
        if (result.ModifiedCount > 0)
            return true;
        return false;
    }

    // Update bagSlot that got equipped and null the item slot that bag originated from in player database document
    public async Task<bool> AUpdateBagEquip(BagData bag, int emptyBag, int bagNum, int slotNum)
    {
        // Initialize the equipped bag in mongoDB format
        MongoInventorySlot[] mongoBag = new MongoInventorySlot[bag.BagSize];
        for (int i = 0; i < mongoBag.Length; i++)
            mongoBag[i] = new MongoInventorySlot();
        
        // Set location to what to update in Document
        var update = Builders<MongoInventory>.Update
            .Set($"bags.{emptyBag}.bagItemID", bag.ItemID)
            .Set($"bags.{emptyBag}.bagInventory", mongoBag)
            .Set($"bags.{bagNum}.bagInventory.{slotNum}.itemID", 0)
            .Set($"bags.{bagNum}.bagInventory.{slotNum}.currentStack", 0);

        // Await for the document to update
        var result = await inventoryCollection.UpdateOneAsync(filter, update);

        // Return boolean if operation was succesful or not
        if (result.ModifiedCount > 0)
            return true;
        return false;
    }

    // Update bagSlot that got equipped and null the item slot that bag originated from in player database document
    public async Task<bool> AUpdateBagRemove(int bagItemID, int emptyBag, int bagNum, int slotNum)
    {
        // Set location to what to update in Document
        var update = Builders<MongoInventory>.Update
            .Set($"bags.{emptyBag}.bagItemID", 0)
            .Set($"bags.{emptyBag}.bagInventory", new MongoInventorySlot[0])
            .Set($"bags.{bagNum}.bagInventory.{slotNum}.itemID", bagItemID)
            .Set($"bags.{bagNum}.bagInventory.{slotNum}.currentStack", 1);

        // Await for the document to update
        var result = await inventoryCollection.UpdateOneAsync(filter, update);

        // Return boolean if operation was succesful or not
        if (result.ModifiedCount > 0)
            return true;
        return false;
    }

    // Update swapping two bags location
    public async Task<bool> AUpdateSwapBags(int originBagSlot, int targetBagSlot, MongoBagSlot originBag, MongoBagSlot targetBag)
    {
        // Set location to what to update in Document
        var update = Builders<MongoInventory>.Update
            .Set($"bags.{targetBagSlot}", originBag)
            .Set($"bags.{originBagSlot}", targetBag);

        // Update the document
        var result = await inventoryCollection.UpdateOneAsync(filter, update);

        // Return boolean if operation was succesful or not
        if (result.ModifiedCount > 0)
            return true;
        return false;
    }

    // Update swapping bag location to emptySlot
    public async Task<bool> AUpdateSwapBagLocation(int originBagSlot, int targetBagSlot, MongoBagSlot bag)
    {
        // Set location to what to update in Document
        var update = Builders<MongoInventory>.Update
            .Set($"bags.{targetBagSlot}", bag)
            .Set($"bags.{originBagSlot}", new MongoBagSlot());

        // Update the document
        var result = await inventoryCollection.UpdateOneAsync(filter, update);

        // Return boolean if operation was succesful or not
        if (result.ModifiedCount > 0)
            return true;
        return false;
    }

    // Update replacing equipped bag from inventory
    public async Task<bool> AUpdateReplaceBag(int bagNum, int slotNum, int targetBagID, int itemID, MongoBagSlot newBag)
    {
        // Set location to what to update in Document
        var update = Builders<MongoInventory>.Update
            .Set($"bags.{targetBagID}", newBag)
            .Set($"bags.{bagNum}.bagInventory.{slotNum}.itemID", itemID)
            .Set($"bags.{bagNum}.bagInventory.{slotNum}.currentStack", 1);

        // Update the document
        var result = await inventoryCollection.UpdateOneAsync(filter, update);

        // Return boolean if operation was succesful or not
        if (result.ModifiedCount > 0)
            return true;
        return false;
    }

    // Update weaponSlot in the player inventory document
    public async Task<bool> AUpdateWeaponEquip(InventorySlot invSlot, int weaponID, int bagNum, int slotNum)
    {
        // Initialize UpdateDefination; makes code more bearable with if-else
        UpdateDefinition<MongoInventory> update;

        // Set location to what to update in Document; if slot runs out of stacks; null that item. Else reduce stack by 1
        if (weaponID == 0)
        {
            update = Builders<MongoInventory>.Update
            .Set($"mainHandWeapon.weaponID", invSlot.item.ItemID)
            .Set($"bags.{bagNum}.bagInventory.{slotNum}", new MongoInventorySlot());
        }
        else 
        {
            update = Builders<MongoInventory>.Update
            .Set($"mainHandWeapon.weaponID", invSlot.item.ItemID)
            .Set($"bags.{bagNum}.bagInventory.{slotNum}", new MongoInventorySlot(weaponID));
        }

        // Update the document
        var result = await inventoryCollection.UpdateOneAsync(filter, update);

        // Return boolean if operation was succesful or not
        if (result.ModifiedCount > 0)
            return true;
        return false;
    }

    // Update weaponSlot in the player inventory document
    public async Task<bool> AUpdateWeaponUnequip(int weaponID, int bagNum, int slotNum)
    {
        // Initialize UpdateDefination; makes code more bearable with if-else
        var update = Builders<MongoInventory>.Update
            .Set($"mainHandWeapon.weaponID", 0)
            .Set($"bags.{bagNum}.bagInventory.{slotNum}", new MongoInventorySlot(weaponID));

        // Update the document
        var result = await inventoryCollection.UpdateOneAsync(filter, update);

        // Return boolean if operation was succesful or not
        if (result.ModifiedCount > 0)
            return true;
        return false;
    }

    // Update weaponSlot to become null
    public async Task<bool> AUpdateWeaponDestroy()
    {
        // Initialize UpdateDefination; makes code more bearable with if-else
        var update = Builders<MongoInventory>.Update.Set($"mainHandWeapon.weaponID", 0);

        // Update the document
        var result = await inventoryCollection.UpdateOneAsync(filter, update);

        // Return boolean if operation was succesful or not
        if (result.ModifiedCount > 0)
            return true;
        return false;
    }
}
