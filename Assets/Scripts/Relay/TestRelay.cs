using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class TestRelay : MonoBehaviour
{
    [Header("--- Panels ---")]
    [SerializeField] GameObject m_InitPanel;
    [SerializeField] GameObject m_JoinPanel;
    [SerializeField] GameObject m_InGamePanel;

    [Header("--- Misc UI ---")]
    [SerializeField] TMP_InputField m_RelayInput;

    string m_PlayerName;

    void Start()
    {
        m_PlayerName = (char)UnityEngine.Random.Range(65, 90) + "-" + UnityEngine.Random.Range(1, 99).ToString();       
    }

    private void Awake()
    {
        m_InitPanel.SetActive(true);
        m_JoinPanel.SetActive(false);
        m_InGamePanel.SetActive(false);
    }
    public void OnClickInitUGS()
    {
        InitalizeAuthenticationAsync();
    }

    async void InitalizeAuthenticationAsync()
    {
        var options = new InitializationOptions();
        options.SetProfile(m_PlayerName);
        await UnityServices.InitializeAsync(options);

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log(m_PlayerName + " signed in. PlayerID: " + AuthenticationService.Instance.PlayerId);
            // monewpanel 1
            m_InitPanel.SetActive(false);
            m_JoinPanel.SetActive(true);
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public void OnClickCreateRelay()
    {
        CreateRelay();
    }

    private async void CreateRelay()
    {
        // 1) create allocation
        // 2) parameter is max number of connections (does not include host so 4 players would be maxConnections = 3)
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3); // create allocation on relay service
            // once you allocation the relay you need to make a connection using whatever transport we want
            // we can connect relay to netcode for game objects and unity transport to open that connect and make it all work thru the relay.
            // Change "Unity Transport" to "Relay Unity Transport"
            // If we'r esetting up Transport to use Relay it depends on version you're using
            // First version is Netcode for GameObjects version 1.0.2

            // got allocation, now generate a join code
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId); // get join code for that allocation
            Debug.Log("Created an allocation and got a joinCode: " + joinCode);

            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            NetworkManager.Singleton.StartHost();

            m_JoinPanel.SetActive(false);
            m_InGamePanel.SetActive(true);
            m_InGamePanel.transform.GetChild(0).GetComponent<TMP_Text>().text = "Join Code: " + joinCode;

        } catch (RelayServiceException e)
        {
            Debug.LogError("Can't create relay allocation: " + e.Message);
        }
    }

    //todo - diff between Singleton and Instance

    public void OnClickJoinRelay()
    {
        JoinRelay(m_RelayInput.text);
    }
    private async void JoinRelay(string joinCode)
    {
        try
        {
            Debug.Log("Join Relay with code: " + joinCode);

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartClient();
            m_JoinPanel.SetActive(false);
        } catch (RelayServiceException e)
        {
            Debug.LogError("Can't join relay with code: " + joinCode + ", error message: " + e.Message);
        }
    }
}
