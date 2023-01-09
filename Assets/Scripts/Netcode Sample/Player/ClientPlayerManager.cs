using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// This class handles management of the various components that can cause issues 
/// with using a Character Controller and Netcode together.  It can work, but you 
/// have to make sure that certain components are turned off if you're not the owner
/// </summary>
public class ClientPlayerManager : NetworkBehaviour
{    
    // these are all references to the various components on the Player that need
    // to be managed when using a Character Controller with Netcode
    [SerializeField] CharacterController CharacterController;
    [SerializeField] PlayerInput PlayerInput;
    [SerializeField] CapsuleCollider CapsuleCollider;
    [SerializeField] PlayerController PlayerController;       
    [SerializeField] PlayerShooting PlayerShooting;    

    private void Awake()
    {
        // By default shut off components that can cause issues with network sync.  
        // See comments in OnNetworkSpawn for better explanation
        CharacterController.enabled = false;
        PlayerInput.enabled = false;
        CapsuleCollider.enabled = false;
        PlayerController.enabled = false;        
        PlayerShooting.enabled = false;        
    }   

    public override void OnNetworkSpawn()
    {
        //Debug.Log("ClientPlayerManager.OnNetworkSpawn() ID: " + GetInstanceID() + " --NCCB--");        

        // set position to the spawn point based on client id
        transform.position = GameObject.Find("SpawnPoints").transform.GetChild((int)OwnerClientId).transform.position;

        // CharacterControllers are only enabled on owning clients.  Non owned player objects
        // (ghost) have this disabled, so in order to make sure that the physics still works
        // when that is disabled you turn on a CapsuleCollider that is left disabled on owning clients
        if (!IsOwner)
        {
            enabled = false;    // we don't need this if we're not the owner            
            CapsuleCollider.enabled = true; // leave the CharacterController off and turn on the Capsule collider
            return;
        }
        
        // since we're the owner go ahead and re-enable all of these components.  
        CharacterController.enabled = true;
        PlayerInput.enabled = true;
        PlayerController.enabled = true;
        PlayerShooting.enabled = true;                        
    }
}
