using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

/// <summary>
/// This is just a helper class to show buttons on screen and set some artificial network 
/// lag if necessary
/// </summary>
public class NetworkButtons : MonoBehaviour {
    private void OnGUI() {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer) {
            if (GUILayout.Button("Host")) NetworkManager.Singleton.StartHost();
            if (GUILayout.Button("Server")) NetworkManager.Singleton.StartServer();
            if (GUILayout.Button("Client")) NetworkManager.Singleton.StartClient();
        }

        GUILayout.EndArea();
    }

    // use this to set up networking tests
    /* private void Awake() {
         GetComponent<UnityTransport>().SetDebugSimulatorParameters(
             packetDelay: 120,
             packetJitter: 5,
             dropRate: 3);
     }*/
}