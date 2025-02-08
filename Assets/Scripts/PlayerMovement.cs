using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// adding namespaces
using Unity.Netcode;
using Unity.VisualScripting;
using System.Linq;
using System;





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
    public float bulletSpead = 1000f;

    /// <summary>
    /// The max number of bullets the player can have stored at any given time.
    /// </summary>
    public int maxBullets = 5;

    /// <summary>
    /// The maximum number of bullets that can exist/be rendered at any one time
    /// </summary>
    public int maxRenderedBullets = 5;

    /// <summary>
    /// How many bullets the player currently has access to/stored
    /// </summary>
    public int currentBulletCount = 0;


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
    /// Max number of parts a player can hold
    /// </summary>
    public int maxParts = 3; 
    
    /// </summary>
    /// Can the player convert a part to bullets
    /// </summary>
    public bool canConvertPartsToBullets = true;

    /// <summary>
    /// For every converted part, the player gets x bullets (up to the maxBullet count)
    /// </summary>
    public int partToBulletConversion = 3; 


    // reference to the camera audio listener
    [SerializeField] private AudioListener audioListener;
    // reference to the camera
    [SerializeField] private Camera playerCamera;


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

            if (inventory != null && inventory.Count > 0) { // If the inventory list isn't empty...

                // Add [partToBulletConversion] number of bullets to count as long as it doesn't exceed maxBullets
                if (currentBulletCount + partToBulletConversion <= maxBullets) {
                    
                    if (inventory[0] == null) {
                        Debug.Log("Part does not exist");
                    
                    } else {
                        Debug.Log("Removing " + inventory.ElementAt(0).Name + "... Converting to " + partToBulletConversion + " bullets!");
                        inventory.RemoveAt(0); // Remove first element of inventory list

                        // Update the current bullet count
                        currentBulletCount += partToBulletConversion; // Does this still need: activeBullets.Count + ???
                        Debug.Log("Current bullet count is now: " + currentBulletCount + "= " + activeBullets.Count + " + " + partToBulletConversion);
                    }

                } else {
                    Debug.Log("Could not convert part to bullets: Bullet count (" + activeBullets.Count + ") cannot exceed maximum bullet count (" + maxBullets + ").");
                }

            }   else {
                Debug.Log("Cannot convert: Inventory is empty!");
            }
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
            AddToInventory("", 1, false); // Create and add an item to the inventory (Default name is "TempObj")
        }


        // When the user shoots a bullet
        if (Input.GetButtonDown("Fire1"))
        {
            // call the BulletSpawningServerRpc method
            // as client can not spawn objects
            BulletSpawningServerRpc(cannon.transform.position, cannon.transform.rotation);
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
    /// Add an item to the inventory list using the given parameters.
    /// </summary>
    /// <param name="partName"> provides the part with a name </param> 
    /// <param name="partCount"> provides how many of that part are being stored </param> 
    /// <param name="hasBeenTurnedIn"> has this item been turned in before? </param> 
    void AddToInventory(String partName, int partCount, bool hasBeenTurnedIn) {
        // Create the new object and store it in the inventory
        String objName;

        if (partName == null || partName == "") {
            objName = "TempObj " + inventory.Count;
        } else {
            objName = partName;
        }
        
        Part newPart = new(objName, 1, false);
        inventory.Add(newPart);

        Debug.Log("Added " + newPart.Name + " to inventory!");
    }

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

    // need to add the [ServerRPC] attribute
    [ServerRpc]
    // method name must end with ServerRPC
    private void BulletSpawningServerRpc(Vector3 position, Quaternion rotation)
    {
        // call the BulletSpawningClientRpc method to locally create the bullet on all clients
        BulletSpawningClientRpc(position, rotation);
    }

    [ClientRpc]
    private void BulletSpawningClientRpc(Vector3 position, Quaternion rotation)
    {
        
        if (currentBulletCount > 0) {

            // Ensure only maxRenderedBullets exist (for performance, and to make it look better; avoids old balls all over the scene)
            if (activeBullets.Count >= maxRenderedBullets)
            {
                // Destroy the oldest bullet and remove it from the list
                Destroy(activeBullets[0]);
                activeBullets.RemoveAt(0);
                // Debug.Log("Max number reached; removed first ball in bullet list");
            }

            GameObject newBullet = Instantiate(bullet, position, rotation);
            newBullet.GetComponent<Rigidbody>().linearVelocity += Vector3.up * 2;
            newBullet.GetComponent<Rigidbody>().AddForce(newBullet.transform.forward * bulletSpead);
            // newBullet.GetComponent<NetworkObject>().Spawn(true);

            // Add the new bullet to the bullet list
            activeBullets.Add(newBullet);
            
            // Decrease the number of remaining bullets available to the player
            currentBulletCount--;

            // Debug.Log("Bullet Fired!");
        }
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

}