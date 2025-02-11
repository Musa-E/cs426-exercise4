using Unity.Netcode;
using UnityEngine;

public class PartSubmission : NetworkBehaviour
{
    /// <summary>
    /// Called when first collision is detected.
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        // printing if collision is detected on the console
        Debug.Log("Turn In Collision Detected!");
    }
}
