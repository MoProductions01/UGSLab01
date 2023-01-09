using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// This script 1) Handles synchronizing our own data instead of using ClientNetworkTransform and 
/// 2) lets us toggle between server and client authority
/// </summary>
public class PlayerNetwork : NetworkBehaviour
{        
    [SerializeField] private bool UsingServerAuthority; //  A toggle to test the difference between owner and server auth.
    [SerializeField] private float KlugeInterpolationTime = 0.1f; // value for helping out the hacky smoothing
    
    private NetworkVariable<PlayerNetworkState> PlayerState; // Current state of our player optimized with serialization   

    private void Awake()
    {        
        // determine whether we have server or client authority based on the bool above
        NetworkVariableWritePermission permission = UsingServerAuthority ? NetworkVariableWritePermission.Server : NetworkVariableWritePermission.Owner;
        PlayerState = new NetworkVariable<PlayerNetworkState>(writePerm: permission);
    }    

    // this was moved from PlayerController
    public override void OnNetworkSpawn()
    {         
        string auth = (IsHost ? "Host - " : "Client - ");       
        this.name = auth + GetInstanceID().ToString();
        //Debug.Log("PlayerNetwork.OnNetworkSpawn() name: " + this.name + " --NCCB--");        
    }
    // Update is called once per frame
    void Update()
    {
        // if owner 
        if (IsOwner == true) // if we're the owner then transmit our state to everyone
        {          
            TransmitState();            
        }
        else
        {           
            ConsumeState(); // if we're not the owner then take the data from this game object and apply it
        }
    }

    /// <summary>
    /// Gets our latest transform data and sends it out over the network via Rpc calls
    /// </summary>
    private void TransmitState()
    {
        PlayerNetworkState state = new PlayerNetworkState // create new state based on our data
        {                       
            Position = transform.position,
            Rotation = transform.rotation.eulerAngles
        };
        
        // if we are NOT server authority then we can just write our value.
        // Even if serverauth is turned on if the client running this code is the server we don't need RPC we can just write to it ourselves
        if (IsServer == true || UsingServerAuthority == false) 
        {
            PlayerState.Value = state;
        }
        else
        {
            // If server authority has been turned on that means this client can't actually write this
            // (it'll throw an exception saying client is not allowed to write to this variable).
            // We actually have to tell the server that "this is my new state please propogate to the rest of the clients".
            // This is done by a Server RPC call
            TransmitStateServerRpc(state); 
        }
    }

    /// <summary>
    /// This is called by clients but executed by a server if we don't have client authority for our state
    /// </summary>
    /// <param name="state"></param>
    [ServerRpc] 
    private void TransmitStateServerRpc(PlayerNetworkState state)
    {
        PlayerState.Value = state;
    }

    private Vector3 _posVel;
    private float _rotVelY;

    /// <summary>
    /// Takes the state and applies it to our transform
    /// </summary>
    private void ConsumeState()
    {
        // these two lines are very klugy in the way they handle smoothing. The ClientNetworkTransform has all sorts of interpolation
        // that we don't have doing things with our own serilazation so just use smooth damping for onw        
        transform.position = Vector3.SmoothDamp(transform.position, PlayerState.Value.Position, 
            ref _posVel, KlugeInterpolationTime);
        transform.rotation = Quaternion.Euler(0, Mathf.SmoothDampAngle(transform.rotation.eulerAngles.y, PlayerState.Value.Rotation.y, 
            ref _rotVelY, KlugeInterpolationTime), 0);
    }

    /// <summary>
    /// Struct holding our player data using the UGS network serialization.  Much more
    /// efficient than ClientNetworkTransform.  Do this by implementing INetworkSerializable
    /// </summary>
    struct PlayerNetworkState : INetworkSerializable
    {
        private float X, Z;        
        private short YRot; // using a short instead of a float loses the fractional part of our rotation but 0-360 is efficient enough

        internal Vector3 Position
        {
            get => new Vector3(X, 0, Z);
            set
            {
                X = value.x;
                Z = value.z;
            }
        }

        internal Vector3 Rotation
        {
            get => new Vector3(0, YRot, 0);
            set => YRot = (short)value.y;
        }

        /// <summary>
        /// The function you have to implement to use INetworkSerializable. It tells
        /// Unity how to serializae our data
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serializer"></param>
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref X);
            serializer.SerializeValue(ref Z);
            serializer.SerializeValue(ref YRot);
        }
    }
}
