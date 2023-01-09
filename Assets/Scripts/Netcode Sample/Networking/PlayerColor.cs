using Unity.Netcode;
using UnityEngine;

/// <summary>
/// A server-authoritative example with reactive checks instead of per-frame checks
/// to change the color of the players as they enter to make them look a little different
/// Also shows reactive checks instead of per-frame checks
/// If you have questions, pop into discord and have a chat https://discord.gg/tarodev
/// </summary>
public class PlayerColor : NetworkBehaviour 
{
    // By default network variables are only changable by the server so we'll have to use an RPC call
    private readonly NetworkVariable<Color> NetColor = new();
    private readonly Color[] Colors = { Color.white, Color.blue, Color.green, Color.yellow, Color.black, Color.red, Color.magenta, Color.gray };
    private int Index; // current color index

    //[SerializeField] private MeshRenderer Renderer; //our renderer
    [SerializeField] private SkinnedMeshRenderer Renderer; // renderer for the mohawk so we can change it's color

    private void Awake() {
        // Subscribing to a change event. This is how the owner will change its color.
        // Could also be used for future color changes
        NetColor.OnValueChanged += OnValueChanged;
    }

    /// <summary>
    /// Removes the change event when we're getting destroyed
    /// </summary>
    public override void OnDestroy() {
        NetColor.OnValueChanged -= OnValueChanged;
    }

    /// <summary>
    /// The change event that's called when the value of the color changes.  
    /// </summary>
    /// <param name="prev"></param>
    /// <param name="next"></param>
    private void OnValueChanged(Color prev, Color next) 
    {
        Renderer.material.color = next;
    }

    public override void OnNetworkSpawn() {
        // Take note, RPCs are queued up to run.
        // If we tried to immediately set our color locally after calling this RPC it wouldn't have propagated
        if (IsOwner) 
        {
            Index = (int)OwnerClientId; // update the color index based on the client ID, which is 0 for host then goes upwards
            CommitNetworkColorServerRpc(GetNextColor()); // Call the RPC to let all the clients know about the change
        }
        else 
        {
            Renderer.material.color = NetColor.Value; // we're not the owner so just set the color to the NetworkVariable value
        }
    }

    /// <summary>
    /// ServerRpc called to let everyone know about the color changes that happened
    /// in OnNetworkSpawn
    /// </summary>
    /// <param name="color"></param>
    [ServerRpc]
    private void CommitNetworkColorServerRpc(Color color) 
    {
        NetColor.Value = color; // this triggers the OnValueChanged events
    }   

    /// <summary>
    /// Helper to get the colors
    /// </summary>
    /// <returns></returns>
    private Color GetNextColor() 
    {
        return Colors[Index++ % Colors.Length];
    }
}