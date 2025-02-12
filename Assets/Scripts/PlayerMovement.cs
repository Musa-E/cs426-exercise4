using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// adding namespaces
using Unity.Netcode;
using Unity.VisualScripting;
using System.Linq;
using System;

//using TMPro; // For the inventory display



#if UNITY_EDITOR
using UnityEditor.Callbacks;
#endif
// because we are using the NetworkBehaviour class
// NewtorkBehaviour class is a part of the Unity.Netcode namespace
// extension of MonoBehaviour that has functions related to multiplayer

//https://www.youtube.com/watch?v=6FitlbrpjlQ
public class PlayerMovement : NetworkBehaviour
{
    // Camera Rotation
    public float mouseSensitivity = 2f;
    private float verticalRotation = 0f;
    private Transform cameraTransform;

    // Ground Movement
    private Rigidbody rb;
    public float MoveSpeed = 5f;
    private float moveHorizontal;
    private float moveForward;

    // Jumping
    public float jumpForce = 10f;
    public float fallMultiplier = 2.5f; // Multiplies gravity when falling down
    public float ascendMultiplier = 2f; // Multiplies gravity for ascending to peak of jump
    private bool isGrounded = true;
    public LayerMask groundLayer;
    private float groundCheckTimer = 0f;
    private float groundCheckDelay = 0.3f;
    private float playerHeight;
    private float raycastDistance;


    /// <summary>
    /// Holds a list of the current bullets in the scene for easy management
    /// </summary>
    private List<GameObject> activeBullets = new List<GameObject>();

    /// <summary>
    /// The speed of the bullets
    /// </summary>
    public float bulletSpeed = 3000f;

    /// <summary>
    /// The max number of bullets the player can have stored at any given time.
    /// </summary>
    public int maxBullets = 15; // DEBUG: Set to 15 to easily test part conversions, normally at 5

    /// <summary>
    /// The maximum number of bullets that can exist/be rendered at any one time
    /// </summary>
    public int maxRenderedBullets = 5;

    /// <summary>
    /// How many bullets the player currently has access to/stored
    /// </summary>
    public int currentBulletCount = 0;

    /// <summary>
    /// How many part(s) the player currently has access to/stored
    /// </summary>
    public int currentPartCount = 0;


    // create a list of colors
    public List<Color> colors = new List<Color>();

    // getting the reference to the prefab
    // [SerializeField]
    // private GameObject spawnedPrefab;
    // save the instantiated prefab
    // private GameObject instantiatedPrefab;

    public GameObject cannon;
    public GameObject bullet;

    /// <summary>
    /// Holds a list of the current items/parts in the player's inventory for easy management
    /// </summary>
    public List<Part> inventory = new List<Part>();


    /// <summary>
    /// Holds the names of every part
    /// </summary>
    public List<String> partNames = new List<String>();
    public List<Part> parts = new List<Part>();

    public int totalPartCount = 11; // Based off the project diagram, there are 11 parts.

    /// <summary>
    /// Max number of parts a player can hold
    /// </summary>
    public int maxParts = 3; 

    /// <summary>
    /// How many parts have been turned in so far
    /// </summary>
    public int turnedInPartCount = 0;


    // Manage the inventory UI
    // private GameObject inventoryManagerObject;
    [SerializeField] public InventoryManager inventoryManager;
    
    /// </summary>
    /// Can the player convert a part to bullets
    /// </summary>
    public bool canConvertPartsToBullets = true;

    /// <summary>
    /// For every converted part, the player gets x bullets (up to the maxBullet count)
    /// </summary>
    public int partToBulletConversion = 3; 

    // Which part is going to be placed in the inventory next.  This is a temp feature just for debugging purposes.
    // This will either be determined randomly, or by some other method at a later date.
    private int elementNumber = 0;


    // reference to the camera audio listener
    [SerializeField] private AudioListener audioListener;
    // reference to the camera
    [SerializeField] private Camera playerCamera;
    // reference to the canvas
    [SerializeField] private Canvas inventoryUiCanvas;

    /// <summary>
    /// Displays the inventory on screen
    /// </summary>
    //[SerializeField] private TMP_Text inventoryText;
    // [SerializeField] public TextMeshProUGUI inventoryText;
    //private readonly InventoryManagerScript InventoryManager;

    //private InventoryManagerScript inventoryManager;
    //InventoryManagerScript IM = GameObject.AddComponent<InventoryManagerScript>;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        cameraTransform = Camera.main.transform;

        // Set the raycast to be slightly beneath the player's feet
        playerHeight = GetComponent<CapsuleCollider>().height * transform.localScale.y;
        raycastDistance = (playerHeight / 2) + 0.2f;

        // Hides the mouse
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Fill the partNames list
        initParts();

        if (inventoryManager != null)
        {
            // inventoryManager = FindFirstObjectByType<InventoryManager>();
            inventoryManager.bulletCount.text = "Ammo: (0/" + maxBullets + ")";
            inventoryManager.partCount.text = "(0/" + totalPartCount + ")";
        }
        else
        {
            Debug.LogError("InventoryManager GameObject is not assigned!");
        }

        // Check if this is the local player
        if (IsOwner) // Replace this with your networking library's local player check
        {
            // Enable the Canvas for the local player
            if (inventoryUiCanvas != null)
            {
                inventoryUiCanvas.enabled = true;
            }
        }
        else
        {
            // Disable the Canvas for remote players
            if (inventoryUiCanvas != null)
            {
                inventoryUiCanvas.enabled = false;
            }
        }
    }



    // Update is called once per frame
    void Update()
    {
        // check if the player is the owner of the object
        // makes sure the script is only executed on the owners 
        // not on the other prefabs 
        if (!IsOwner) return;

        moveHorizontal = Input.GetAxisRaw("Horizontal");
        moveForward = Input.GetAxisRaw("Vertical");

        RotateCamera();

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            Jump();
        }

        // Checking when we're on the ground and keeping track of our ground check delay
        if (!isGrounded && groundCheckTimer <= 0f)
        {
            Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
            isGrounded = Physics.Raycast(rayOrigin, Vector3.down, raycastDistance, groundLayer);
        }
        else
        {
            groundCheckTimer -= Time.deltaTime;
        }
        // transform.position += moveDirection * speed * Time.deltaTime;


        // Allows the player to convert their first part into x bullets
        if (Input.GetKeyDown(KeyCode.C) && canConvertPartsToBullets) {
            convertPartToBullet();
        }

        /* 
            If 'I' is pressed spawn the object [DEBUG]

            For debugging, when you want to get bullets, give yourself a part to convert first.
            This uses a new object type called a "Part", as defined in the "Part.cs" file.  It's
            a super simple type, with a name, count, and a boolean keeping track if it's been turned
            in yet.  It's highly recommended that when the turn in area is being implemented, there
            be a way to store the names of each of these objects.  That said, make sure that it can
            do that for multiple different clients giving the same objects (aka, differentiate between
            them).
        */
        if (Input.GetKeyDown(KeyCode.I))
        {
            // AddToInventory("", 1, false, "Misc"); // Create and add an item to the inventory (Default name is "TempObj")

            if (currentPartCount < maxParts) { // Don't add a part if the max number of parts has been reached
                
                // Keep going through the part list
                if (elementNumber >= 0 && elementNumber < parts.Count) {
                    AddToInventory(parts.ElementAt(elementNumber));
                    // Debug.Log("Element number is currently: " + elementNumber);
                    elementNumber++; // increment
                }
            } else {
                Debug.Log("Cannot add item: Cannot exceed max part count. You have " + currentPartCount + " parts, max is " + maxParts);
            }
            /// Visually update the inventory to show that it at least updates [temp]
            // inventoryText.text = "Game Started!";
        }

        // Allows the player to convert their first part into x bullets
        if (Input.GetKeyDown(KeyCode.R)) {
            resetPartTurnedInStatus();
        }


        // Allows the player to turn in their first, not-turned in part so it can't be stolen
        if (Input.GetKeyDown(KeyCode.T)) {
            
            // If this function returns true, a part was turned in.  Otherwise, all parts (11 total) were unable to be turned in.
            if(!turnInFirstEligibleItem() && turnedInPartCount >= 11) {
                Debug.Log("No parts could be turned in.  This is a win condition.");
            }

        }


        // When the user shoots a bullet
        if (Input.GetButtonDown("Fire1"))
        {
            // call the BulletSpawningServerRpc method
            // as client can not spawn objects
            // BulletSpawningServerRpc(cannon.transform.position, cannon.transform.rotation);
            FireBullet();
        }
    }

    void FixedUpdate()
    {
        MovePlayer();
        ApplyJumpPhysics();
    }


    
    void MovePlayer()
    {

        Vector3 movement = (transform.right * moveHorizontal + transform.forward * moveForward).normalized;
        Vector3 targetVelocity = movement * MoveSpeed;

        if (rb.isKinematic) rb.isKinematic = false; // Ensure Rigidbody is NOT kinematic (gets rid of warning spammed in console)

        // Apply movement to the Rigidbody
        Vector3 velocity = rb.linearVelocity;
        velocity.x = targetVelocity.x;
        velocity.z = targetVelocity.z;
        rb.linearVelocity = velocity;

        // If we aren't moving and are on the ground, stop velocity so we don't slide
        if (isGrounded && moveHorizontal == 0 && moveForward == 0)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }

    void RotateCamera()
    {
        float horizontalRotation = Input.GetAxis("Mouse X") * mouseSensitivity;
        transform.Rotate(0, horizontalRotation, 0);

        verticalRotation -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
    }

    void Jump()
    {
        isGrounded = false;
        groundCheckTimer = groundCheckDelay;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z); // Initial burst for the jump
    }

    void ApplyJumpPhysics()
    {
        if (rb.linearVelocity.y < 0)
        {
            // Falling: Apply fall multiplier to make descent faster
            rb.linearVelocity += Vector3.up * Physics.gravity.y * fallMultiplier * Time.fixedDeltaTime;
        } // Rising
        else if (rb.linearVelocity.y > 0)
        {
            // Rising: Change multiplier to make player reach peak of jump faster
            rb.linearVelocity += Vector3.up * Physics.gravity.y * ascendMultiplier * Time.fixedDeltaTime;
        }
    }


    /// <summary>
    /// Add an item to the inventory list using the given parameters.  [DO NOT USE]
    /// </summary>
    /// <param name="partName"> provides the part with a name </param> 
    /// <param name="partCount"> provides how many of that part are being stored </param> 
    /// <param name="hasBeenTurnedIn"> has this item been turned in before? </param> 
    // void AddToInventory(String partName, int partCount, bool hasBeenTurnedIn, string category) {
        
    //     // Create the new object and store it in the inventory
    //     String objName;
    //     string catName; // What category does this item belong to

    //     // Handles the partName
    //     if (partName == null || partName == "") {
    //         objName = "TempObj " + inventory.Count;
    //     } else {
    //         objName = partName;
    //     }

    //     // Handles the category
    //     if (category == null || category == "") {
    //         catName = "Misc";
    //     } else {
    //         catName = category;
    //     }
        
    //     Part newPart = new(objName, 1, false);
    //     newPart.SetMemberOf(catName); // Update the category

    //     // Add to inventory
    //     inventory.Add(newPart);

    //     currentPartCount++; // Increment the current number of parts for easy viewing in the inspector
    //     Debug.Log("Added " + newPart.Name + " to inventory!");
    // }


    /// <summary>
    /// Add an item to the inventory list using the given part parameter.
    /// </summary>
    /// <param name="acquiredItem"> provides the part to add to the inventory (with various conditions) </param> 
    void AddToInventory(Part acquiredItem) {
        
        /* ************************************************************************************************************
            NOTE:

            I wanted to make things a bit easier, so I made the inventory always use the actual elements in the
            'parts' list declared at the top of this file.  That way, whenever you use the part for some reason,
            it's the same object, not a local copy of it.  So if you need to use a part, please refer to it as 
                parts.ElementAt(i) 
            instead of assigning a local variable to hold a copy of it, like 
                Part temp = parts.ElementAt(i)
            
            It might not be the most convenient, but it's late and since this works, it's good enough for me.
        ************************************************************************************************************ */

        // Confirm non-null
        if (acquiredItem == null) {
            Debug.Log("Cannot add part: Item is null");
            return;
        }

        // The index of the part in the parts list
        int partIdx = FindPartIndexByName(acquiredItem.Name);

        // Assuming the item did exist in the parts list, continue
        if (partIdx != -1) {

            // Prevents duplicates; if name is already in inventory, does not add
            if (!isPartAlreadyInInventory(acquiredItem.Name)) {

                if (!parts.ElementAt(partIdx).WasTurnedIn) { // Part has not been turned in yet
                            
                    // Update turned in status for part from part list and add it to the player's inventory
                    inventory.Add(parts.ElementAt(partIdx));
                    parts.ElementAt(partIdx).WasTurnedIn = false;

                    currentPartCount++; // Increment the current number of parts for easy viewing in the inspector
                    Debug.Log("The '" + parts.ElementAt(partIdx).Name + "' part has been acquired.");
                    inventoryManager.updateInventoryTextPartAcquired(parts.ElementAt(partIdx)); 

                } else { // Element found, but was already turned in; does not allow for duplicate items in inventory
                    Debug.Log("The '" + parts.ElementAt(partIdx).Name + "' part has already been turned in, and cannot be reacquired.");
                }
                
            } else {
                Debug.Log("Item not added: Cannot have duplicate item names in inventory");
            }

        } else { // Item was not found in the parts list
            Debug.Log("This part does not exist in the part list and cannot be acquired.");
        }

    }


    /// <summary>
    /// Converts parts held in the player's inventory into bullets, with various conditions being checked
    /// </summary>
    private void convertPartToBullet() {

        if (inventory != null && inventory.Any()) { // If the inventory list isn't empty...

            // Enforce the rule of needing at least one part in order to convert into bullets
            if (currentPartCount > 0) {

                // Add [partToBulletConversion] number of bullets to count as long as it doesn't exceed maxBullets
                if (currentBulletCount + partToBulletConversion <= maxBullets) {
                    
                    if (inventory[0] == null) { // Ensure not null
                        Debug.Log("Part does not exist");
                    
                    } else {
                        Debug.Log("Removing " + inventory.ElementAt(0).Name + "... Converting to " + partToBulletConversion + " bullets!");
                        
                        // Not sure which one to use
                        // inventory.RemoveAt(0); // Remove first element of inventory list
                        // inventory.RemoveAt(turnedInPartCount); // Remove element of inventory list that matches which part has been turned in
                        loseInventoryItem();

                        // Update the current bullet/part count (visually too)
                        currentBulletCount += partToBulletConversion;
                        inventoryManager.updateVisualBulletCounter(currentBulletCount, maxBullets);
                        // inventoryManager.updateVisualPartCounter(turnedInPartCount, totalPartCount);
                        // Debug.Log("Current bullet count is now: " + currentBulletCount + "= " + (currentBulletCount-partToBulletConversion) + " + " + partToBulletConversion);
                    }

                } else {
                    Debug.Log("Could not convert part to bullets: Bullet count (" + currentBulletCount + ") cannot exceed maximum bullet count (" + maxBullets + ").");
                }
            
            } else {
                Debug.Log("Cannot convert: No parts to convert.");
            }

        }   else {
            Debug.Log("Cannot convert: Inventory is empty!");
        }
    }


    /// <summary>
    /// Converts the first part in the inventory into bullets </param> 
    /// </summary>
    // void convertPartToBullet() {

    //     if (inventory != null && inventory.Count > 0) { // If the inventory list isn't empty...

    //             // Add [partToBulletConversion] number of bullets to count as long as it doesn't exceed maxBullets
    //             if (currentBulletCount + partToBulletConversion <= maxBullets) {
                    
    //                 if (inventory[0] == null) {
    //                     Debug.Log("Part does not exist");
                    
    //                 } else {
    //                     Debug.Log("Removing " + inventory.ElementAt(0).Name + "... Converting to " + partToBulletConversion + " bullets!");
    //                     inventory.RemoveAt(0); // Remove first element of inventory list
    //                     currentPartCount--; // Decrement the current number of parts for easy viewing in the inspector

    //                     // Update the current bullet count
    //                     currentBulletCount += partToBulletConversion; // Does this still need: activeBullets.Count + ???
    //                     Debug.Log("Current bullet count is now: " + currentBulletCount + "= " + (currentBulletCount-partToBulletConversion) + " + " + partToBulletConversion);
    //                 }

    //             } else {
    //                 Debug.Log("Could not convert part to bullets: Bullet count (" + activeBullets.Count + ") cannot exceed maximum bullet count (" + maxBullets + ").");
    //             }

    //         }   else {
    //             Debug.Log("Cannot convert: Inventory is empty!");
    //         }
    // }


    /// <summary>
    /// Fills the part list with all parts possible
    /// </summary>
    void initParts() {

        // Input Devices
        Part keyboard = new("Keyboard", 1, false, "Input Devices");
        Part mouse = new("Mouse", 1, false, "Input Devices");
        Part scanner = new("Scanner", 1, false, "Input Devices");
        Part joystick = new("Joystick", 1, false, "Input Devices");

        // Output Devices
        Part controlUnit = new("ControlUnit", 1, false, "CPU");
        Part ALU = new("ALU", 1, false, "CPU");
        Part memory = new("Memory", 1, false, "CPU");

        // CPU Devices
        Part monitor = new("Monitor", 1, false, "Output Devices");
        Part printer = new("Printer", 1, false, "Output Devices");
        Part speaker = new("Speaker", 1, false, "Output Devices");
        Part headphones = new("Headphones", 1, false, "Output Devices");

        // Add to part list
        parts.Add(keyboard);
        parts.Add(mouse);
        parts.Add(scanner);
        parts.Add(joystick);

        parts.Add(controlUnit);
        parts.Add(ALU);
        parts.Add(memory);

        parts.Add(monitor);
        parts.Add(printer);
        parts.Add(speaker);
        parts.Add(headphones);
  
    }


    /// <summary>
    /// Allows you to reset the "Turned in" status of all parts in the parts list
    /// </summary>
    void resetPartTurnedInStatus() {
        for (int i = 0; i < parts.Count; i++) {
            parts.ElementAt(i).WasTurnedIn = false;
        }
        
        // Reset the counter for each item being added to the inventory
        elementNumber = 0;

        Debug.Log("Part 'Turned In' Status has been reset for all items.");
    }


    /// <summary>
    /// Given a part name, this function will return the index of the part with the matching name in the parts list. -1 if it doesn't exist
    /// </summary>
    /// <param name="partName"> Name of the part you're searching for </param>
    /// <returns> index of the part with the matching name in the parts list. -1 if it doesn't exist </returns>
    public int FindPartIndexByName(string partName) {

        if (partName == null || partName == "") {
            return -1;
        }

        // Verify the item exists in the part list
        for (int i = 0; i < parts.Count ; i++) { // Not too many items, so efficiency isn't a big deal.
            
            if (parts.ElementAt(i).Name == partName) { // Part name matches   
                // Debug.Log("The '" + parts.ElementAt(i).Name + "' part has been found.");
                
                // Return the matching part
                return i;
            }
            else { // Element not found
                // Debug.Log("The '" + parts.ElementAt(i).Name + "' part not found in part list, and cannot be acquired.");
            }
        }

        return -1;
    }


    public bool isPartAlreadyInInventory(string partName) {

        // Ensure given part exists
        if (partName == null || partName == "") {
            return false;
        }

        // Verify the item exists in the part list
        for (int i = 0; i < inventory.Count ; i++) { // Not too many items, so efficiency isn't a big deal.
            
            if (inventory.ElementAt(i).Name == partName) { // Part name matches   
                // Debug.Log("The '" + parts.ElementAt(i).Name + "' part has been found.");
                
                // Return the matching part
                return true;
            }
            else { // Element not found
                // Debug.Log("The '" + parts.ElementAt(i).Name + "' part not found in part list, and cannot be acquired.");
            }
        }

        return false;
    }


    /// <summary>
    /// Allows the player to turn in the first item in their inventory
    /// </summary>
    /// <returns> True: A part was turned in.  False: No part could be turned in, this is a win condition. </returns>
    bool turnInFirstEligibleItem() {

        Debug.Log("Checking turn-in status of all items in inventory.");

        // Iterate through list until the first element that hasn't been turned in is found
        for (int i = 0 ; i < inventory.Count; i++) {
            
            int myPartIdx = FindPartIndexByName(inventory.ElementAt(i).Name);
            Debug.Log("Checking " + inventory.ElementAt(i).Name + " turn-in status: " + parts.ElementAt(myPartIdx).WasTurnedIn + ".");
            
            // If the item in the inventory has not already been turned in, turn it in.
            // Finds the first not turned in item, updates it, then returns
            if (!parts.ElementAt(myPartIdx).WasTurnedIn) {
                // Marked as turned in
                parts.ElementAt(myPartIdx).WasTurnedIn = true;

                // Decrement part count
                currentPartCount--;
                turnedInPartCount++;  // Increment how many parts were turned in

                Debug.Log("Your '" + inventory.ElementAt(i).Name + "' part was turned in and can no longer be lost!");

                // Update the UI
                if (inventoryManager != null)
                {
                    inventoryManager.turnedIn(inventory.ElementAt(i));
                    inventoryManager.updateVisualPartCounter(turnedInPartCount, totalPartCount);
                }

                // Remove from inventory
                inventory.RemoveAt(i);

                return true;
            }

        }



        return false;

    }

    private void loseInventoryItem(){
        int myPartIdx = FindPartIndexByName(inventory.ElementAt(0).Name);
        if(!parts.ElementAt(myPartIdx).WasTurnedIn){
            inventoryManager.updateInventoryTextPartLost(inventory.ElementAt(0));
        }
        inventory.RemoveAt(0);
        currentPartCount--;
    }

    // private void turnInInventory() {

    //     // Iterate through list to turn in valid parts.
    //     for (int i = 0 ; i < inventory.Count; i++) {
    //         // If the item in the inventory has not already been turned in, turn it in.
    //         int partIndex = parts.IndexOf(inventory.ElementAt(i));
    //         Debug.Log($"Part index: {partIndex}");
    //         if (!parts.ElementAt(partIndex).WasTurnedIn) {
    //             parts.ElementAt(partIndex).WasTurnedIn = true;
    //             Debug.Log("Your '" + inventory.ElementAt(i).Name + "' part was turned in and can no longer be lost!");
    //             inventory.RemoveAt(i);
    //         } else {
    //             Debug.Log("This part was already submitted. Consider converting it to bullets.");
    //         }
    //     }
    // }


    // this method is called when the object is spawned
    // we will change the color of the objects
    public override void OnNetworkSpawn()
    {
        GetComponent<MeshRenderer>().material.color = colors[(int)OwnerClientId];

        // check if the player is the owner of the object
        if (!IsOwner) return;
        // if the player is the owner of the object
        // enable the camera and the audio listener
        audioListener.enabled = true;
        playerCamera.enabled = true;
    }


    /// <summary>
    /// Spawns a bullet in sync with server and client.
    /// </summary>
    [ServerRpc]
    private void BulletSpawningServerRpc(Vector3 position, Quaternion rotation)
    {
        // Changed spawn calls to work on server rpc instead. Works better for moving objects like bullets.
        GameObject newBullet = Instantiate(bullet, position, rotation);
        NetworkObject networkObject = newBullet.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            networkObject.Spawn(true);
            BulletSetupClientRpc(newBullet);
        }
    }

    /// <summary>
    /// Applies velocity and movement for bullets in each client.
    /// </summary>
    [ClientRpc]
    private void BulletSetupClientRpc(NetworkObjectReference bulletRef)
    {
        // Apply velocity and movement for each client
        if (bulletRef.TryGet(out NetworkObject bullet))
        {
            // bullet.GetComponent<Rigidbody>().linearVelocity += Vector3.up * 2;
            bullet.GetComponent<Rigidbody>().AddForce(bullet.transform.forward * bulletSpeed);
        }
    }

    // [ClientRpc]
    // private void BulletSpawningClientRpc(Vector3 position, Quaternion rotation, ulong playerId)
    // {

    //     // Prevent the local shooter from duplicating bullets (already spawned by the server)
    //     if (IsOwner) return;

    //     // Ensure only maxRenderedBullets exist (for performance)
    //     if (activeBullets.Count >= maxRenderedBullets)
    //     {
    //         Destroy(activeBullets[0]);
    //         activeBullets.RemoveAt(0);
    //     }

    //     GameObject newBullet = Instantiate(bullet, position, rotation);
    //     NetworkBullet bulletScript = newBullet.GetComponent<NetworkBullet>();

    //     if (bulletScript != null)
    //     {
    //         Vector3 initialVelocity = newBullet.transform.forward * (bulletSpead * 0.5f) + Vector3.up * 1.5f; // Reduced speed
    //         bulletScript.SetVelocity(initialVelocity);
    //     }

    //     // Add to active bullets list
    //     activeBullets.Add(newBullet);


    //     // // If this client is the shooter, don't spawn another bullet (since it already did locally)
    //     // if (NetworkManager.Singleton.LocalClientId == playerId) return;

    //     // // Ensure only maxRenderedBullets exist (for performance)
    //     // if (activeBullets.Count >= maxRenderedBullets)
    //     // {
    //     //     Destroy(activeBullets[0]);
    //     //     activeBullets.RemoveAt(0);
    //     // }

    //     // GameObject newBullet = Instantiate(bullet, position, rotation);
    //     // NetworkBullet bulletScript = newBullet.GetComponent<NetworkBullet>();

    //     // if (bulletScript != null)
    //     // {
    //     //     Vector3 initialVelocity = newBullet.transform.forward * (bulletSpead * 0.5f) + Vector3.up * 1.5f; // Reduced speed
    //     //     bulletScript.SetVelocity(initialVelocity);
    //     // }

    //     // // Add to active bullets list
    //     // activeBullets.Add(newBullet);
    // }


    /// <summary>
    /// Local spawning of a bullet so client doesn't need to wait for the server for everything.
    /// </summary>
    private void FireBullet()
    {

        // Prevent shooting if out of bullets
        if (currentBulletCount <= 0) {
            Debug.Log("No bullets left!");
            return;
        }
        currentBulletCount--;

        // Update the visual counter accordingly
        inventoryManager.updateVisualBulletCounter(currentBulletCount, maxBullets);

        // Tell the server to officially create the bullet for synchronization
        BulletSpawningServerRpc(cannon.transform.position, cannon.transform.rotation);
    }


    /* 
        NOTE:

            I don't think these two functions are needed since each client can locally hold their own inventory
            without needing everyone else's.  That said, I included them just in case.  I'm not too familiar with
            these Server/Client RPC functions yet, so feel free to get rid of them if they aren't needed.
    
        - Musa
    */

    // AddToInventory functions [Read above note]
    // need to add the [ServerRPC] attribute
    // [ServerRpc]
    // private void AddToInventoryServerRpc(Part part) {

    //     // call the AddToInventoryClientRpc method to locally add the item to the inventory all clients
    //     AddToInventoryClientRpc(part);
    // }

    // need to add the [ClientRpc] attribute
    // [ClientRpc]
    // private void AddToInventoryClientRpc(Part part) {
    //     inventory.Add(part);
    //     Debug.Log("Added part to inventory. Total parts: " + inventory.Count);
    // }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("hit detected");
        if (other.gameObject.CompareTag("building"))
        {
            int myPartIdx = FindPartIndexByName(other.name);
            AddToInventory(parts.ElementAt(myPartIdx));
            Debug.Log($"Items in inventory {inventory.Count()}");
            //IM.SetText(inventory);
        }
    }

    // Function is called on first collision
    private void OnCollisionEnter(Collision collision)
    {   
        // Debug.Log("Entered Collision");

        // Checks if player collided with bullet and if their inventory is not empty. As a result, the first item in their inventory will be removed.
        if(collision.gameObject.CompareTag("Bullet") && inventory.Any()) {
            Debug.Log($"Bullet x Player Collision Detected! Player loses {inventory.ElementAt(0).Name}");
            loseInventoryItem();
            
        }

        if(collision.gameObject.tag == "Submit" && inventory.Any()) {
            Debug.Log("Turn In Station x Player Collision Detected!");
            if(!turnInFirstEligibleItem() && turnedInPartCount >= 11) {
                Debug.Log("No parts could be turned in.  This is a win condition.");
            }
        }

    }
}