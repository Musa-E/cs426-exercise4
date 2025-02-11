using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// adding namespaces
using Unity.Netcode;

#if UNITY_EDITOR
using UnityEditor.Callbacks;
#endif

public class Bullet : NetworkBehaviour
{

    //this method is called whenever a collision is detected
    private void OnCollisionEnter(Collision collision)
    {

        // printing if collision is detected on the console
        Debug.Log("Bullet Collision Detected!");

        // if the collision is detected destroy the bullet
        DestroyBulletServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    // method name must end with ServerRPC
    private void DestroyBulletServerRpc()
    {
        //despawn
        NetworkObject networkObject = GetComponent<NetworkObject>();
        if (networkObject != null && networkObject.IsSpawned)
        {
            networkObject.Despawn(true);
        }
        Destroy(gameObject);
    }

}