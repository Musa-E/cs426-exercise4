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

    /// <summary>
    /// Called when first collision is detected.
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {

        // printing if collision is detected on the console
        Debug.Log("Bullet Collision Detected!");

        // if the collision is detected destroy the bullet
        DestroyBulletServerRpc();
    }

    /// <summary>
    /// Despawns bullet in sync with client and server.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
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