using Unity.Netcode;
using UnityEngine;

public class NetworkBullet : NetworkBehaviour
{
    private Rigidbody rb;

    // Syncs velocity from server to clients
    private NetworkVariable<Vector3> networkVelocity = new NetworkVariable<Vector3>(
        Vector3.zero, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer) // Clients receive the velocity from the server
        {
            ApplyVelocity(networkVelocity.Value);
        }
    }

    public void SetVelocity(Vector3 velocity)
    {
        if (IsServer)
        {
            networkVelocity.Value = velocity; // Server updates the velocity for all clients
        }

        ApplyVelocity(velocity);
    }

    private void ApplyVelocity(Vector3 velocity)
    {
        if (!rb.isKinematic)
        {
            rb.linearVelocity = velocity;
        }
    }
}
