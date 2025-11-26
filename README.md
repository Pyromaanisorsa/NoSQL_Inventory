README WORK IN PROGRESS

# NoSQL_Inventory
Unity inventory system where inventory data is stored and updated in MongoDB NoSQL database. All inventory updates are first made to the database before applying them in-game locally. Features item database and editor tools to add/remove/modify in-game items. Invenory UI supports different types of item slots (item, bag, weapon) and features like double clicking, right click context menu per item type / slot, search bar and dragging itemSlots in the UI. The slots react to these actions differently based off of the dragged/clicked item's type. 

When adding items to inventory the game takes into calculation: weight, empty slots and stackable items stacks.
This hasn't been translated yet, but here's the flowchart of AddItem function in Assets/Scripts/Inventory/Inventory in finnish.
![AddItemFunction in Finnish(AddItem_FlowChart.png).
