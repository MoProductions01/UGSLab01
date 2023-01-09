using UnityEngine;

/// <summary>
/// Simple script to take care of shooting our ball objects.
/// </summary>
public class Projectile : MonoBehaviour
{
    [SerializeField] private AudioClip DestroyAudioClip; // audio clip to play when ball goes away    

    private Vector3 Direction; // direction we're going

    /// <summary>
    /// Gets the ball going
    /// </summary>
    /// <param name="dir"></param>
    public void Init(Vector3 dir)
    {
        GetComponent<Rigidbody>().AddForce(dir); // add force for the physics system
        Invoke(nameof(DestroyBall), 3); // destroy the ball after 3 seconds
    }

    /// <summary>
    /// Destroy the ball
    /// </summary>
    private void DestroyBall()
    {
        // play a sound, start the particles and destroy ourselves
        AudioSource.PlayClipAtPoint(DestroyAudioClip, transform.position);        
        Destroy(gameObject);
    }
}