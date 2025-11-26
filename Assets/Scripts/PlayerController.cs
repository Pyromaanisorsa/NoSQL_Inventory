using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Threading.Tasks;

public class PlayerController : MonoBehaviour
{
    [Header("Player movement")]
    public float maxSpeed = 20f;
    public float acceleration = 10f;
    public float deceleration = 5f;
    public float cameraSpeed = 5f;
    [Space(10)]

    [Header("Player References")]
    public Rigidbody rb;
    public GameObject hand;
    public Inventory inventory;
    public InventoryUI inventoryUI;

    private PlayerControls keybinds;
    private Vector2 moveDirection;
    [SerializeField] private Camera playerCamera;

    private void Awake()
    {
        // Set player camera active
        playerCamera.tag = "MainCamera";
        Camera.SetupCurrent(playerCamera);

        // Initialize player controls+enable them
        keybinds = new PlayerControls();
        keybinds.Enable();

        // Register the input action functions
        keybinds.Player.Inventory.performed += ctx => ToggleInventory();
        keybinds.Player.Movement.performed += ctx => moveDirection = ctx.ReadValue<Vector2>();
        keybinds.Player.Movement.canceled += ctx => moveDirection = Vector2.zero;
    }

    /*private void Start()
    {
        // Create reference to inventory and UI for both vice versa
        inventory.inventoryUI = inventoryUI;
        inventoryUI.inventory = inventory;

        // Inventory start setup
        inventory.SetupInventory();

        // Inventory UI start setup
        inventoryUI.SetupInventoryUI();
    }*/

    // Check camera movement during Update
    private void Update()
    {
        // Can't turn camera if searchBar is selected DUCT TAPE CONTROLS
        if (inventoryUI.IsSearchSelected())
            return;

        if (keybinds.Player.RotateCameraLeft.IsPressed())
            transform.Rotate(0, -cameraSpeed * Time.deltaTime, 0);
        else if(keybinds.Player.RotateCameraRight.IsPressed())
            transform.Rotate(0, cameraSpeed * Time.deltaTime, 0);
    }

    // Set references for both inventory and inventoryUI and start their setups
    public async Task SetupPlayer(TestDatabaseConnector database) 
    {
        // Create reference to inventory and UI for both vice versa
        inventory.inventoryUI = inventoryUI;
        inventoryUI.inventory = inventory;
        
        // Inventory start setup
        await inventory.SetupInventory(database);

        // Inventory UI start setup
        inventoryUI.SetupInventoryUI();
    }

    // Check movement during FixedUpdate
    private void FixedUpdate()
    {
        MovementCheck();
    }

    // Show/hide inventory view
    private void ToggleInventory()
    {
        // Can't toggle inventory if searchBar is selected DUCT TAPE CONTROLS
        if (inventoryUI.IsSearchSelected())
            return;

        // Get the canvasGroup from UI gameObject; flip it's BlockRayCast and Alpha values
        CanvasGroup group = inventoryUI.gameObject.GetComponent<CanvasGroup>();
        group.blocksRaycasts = !group.blocksRaycasts;

        // Set BlockRayCasts value to opposite
        if (group.blocksRaycasts)
            group.alpha = 1;
        else
            group.alpha = 0;
    }

    // Acceleration & deacceleration based movement
    private void MovementCheck()
    {
        // Can't move if searchBar is selected DUCT TAPE CONTROLS
        if (inventoryUI.IsSearchSelected())
            return;

        Vector3 velocity = new Vector3(moveDirection.x, 0, moveDirection.y) * maxSpeed;

        // If input; Apply acceleration force to player
        if (moveDirection != Vector2.zero)
        {
            
            rb.AddRelativeForce(velocity * acceleration, ForceMode.Acceleration);
        }
        // Deaccelerate player if no input
        else
        { 
            // Calculate deacceleration force
            Vector3 currentVelocity = rb.velocity;
            Vector3 deForce = currentVelocity.normalized * deceleration;

            // Calculate how much player slows down; if velocity smaller than the next deacceleration; stop player
            if (currentVelocity.magnitude < deceleration * Time.fixedDeltaTime)
                rb.velocity = Vector3.zero;
            // Not stopping; slow down player by deForce amount
            else
                rb.AddForce(deForce, ForceMode.Acceleration);
        }

        // Don't let speed go faster than maxSpeed
        if (rb.velocity.magnitude > maxSpeed)
            rb.velocity = rb.velocity.normalized * maxSpeed;
    }

    // OnTrigger; if player touches item Gameobject; try to add items inside it to player inventory
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Item")
        {
            Item script = other.GetComponent<Item>();
            AddItemAsync(script);
        }
    }


    // Async Task for item adding; need to wait for operation to complete before solving aftermath
    private async Task AddItemAsync(Item item)
    {
        if (item.IsActive())
        {
            inventory.EnqueueOperation(async () =>
            {
                await inventory.AddItem(item.lootList);
                item.UpdateItem();
            });
        }
    }
}