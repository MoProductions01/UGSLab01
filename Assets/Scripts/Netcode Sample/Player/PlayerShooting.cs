using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// This handled the players shooting soccer balls over the network via RPC's
/// ServerRpc - server code that can only run on the server but is triggered by clients
/// ClientRpc - Can only be called on the server but is executed on all clients.
/// </summary>
public class PlayerShooting : NetworkBehaviour
{
    [SerializeField] private Projectile Ball;   // soccer ball we're going to shoot
    [SerializeField] private AudioClip ShootingAudioClip; // sound when shooting
    [SerializeField] private float BallSpeed = 700; // speed of the balls
    [SerializeField] private float ShootingCooldown = 0.5f; // how long to wait in between shots
    [SerializeField] private Transform SpawnLocation;   // location to spawn the ball

    private float LastFired = float.MinValue; // time a ball was last fired
    private bool HasFired; // debug flag for showing a message on screen when a ball is shot

    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetMouseButton(0) && LastFired + ShootingCooldown < Time.time) // only shoot if we've waiting long enough
        {
            LastFired = Time.time; // keep track of last firing time
            Vector3 dir = transform.forward; // direction ball will shoot

            // Send off the request to be executed on all clients
            RequestFireServerRpc(dir);

            // Fire locally immediately
            ExecuteShoot(dir);
            StartCoroutine(ToggleLagIndicator()); // this is for debugging to see when you've fired
        }
    }

    /// <summary>
    /// This is called by the update when the Owner wants to fire off a ball.  Since we want
    /// ALL clients to shoot the ball as well we first call a ServerRpc since Servers are the
    /// only ones that can call ClientRpc's.  Then we call the ClientRpc which will shoot the ball
    /// on every client. 
    /// </summary>
    /// <param name="dir"></param>
    [ServerRpc]
    private void RequestFireServerRpc(Vector3 dir)
    {   // While this does work, it introduces lag because the server can fire immediately while the clients
        // have to wait around.  The solution...add an additional local ExecuteShoot() call so add a check 
        // in FireClientRpc to make sure you don't fire an additional ball
        FireClientRpc(dir);
    }

    /// <summary>
    /// Called from the server to execute the "ExecuteShoot" function on all clients
    /// </summary>
    /// <param name="dir"></param>
    [ClientRpc] 
    private void FireClientRpc(Vector3 dir)
    {
        // The reason we check to make sure we're not the owner is that we added a local ExecuteShoot() call in Update
        // on this object that gets called immediately.  The rest will have a slight delay
        if (!IsOwner) ExecuteShoot(dir);
    }

    /// <summary>
    /// Creates and shoots a ball.  When this is called by the ClientRpc it will go to every client
    /// </summary>
    /// <param name="dir"></param>
    private void ExecuteShoot(Vector3 dir)
    {   // Note that this isn't a network object but just a local object on everybody's client.
        // So there might be slight millisecond delay with this method but everyone is managing their own stuff.
        // Not everything needs to be a network object so doing it like this saves some bandwidth
        var projectile = Instantiate(Ball, SpawnLocation.position, Quaternion.identity);
        projectile.Init(dir * BallSpeed);
        AudioSource.PlayClipAtPoint(ShootingAudioClip, transform.position);
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        if (HasFired) GUILayout.Label("FIRED LOCALLY");

        GUILayout.EndArea();
    }

    /// <summary>
    /// If you want to test lag locally, go into the "NetworkButtons" script and uncomment the artificial lag
    /// </summary>
    /// <returns></returns>
    private IEnumerator ToggleLagIndicator()
    {
        HasFired = true;
        yield return new WaitForSeconds(0.2f);
        HasFired = false;
    }
}