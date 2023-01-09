using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using Unity.Tutorials.Core.Editor;
using Unity.VisualScripting;

public class StreamlineLobby : MonoBehaviour
{
    [Header("--- Panels ---")]
    [SerializeField] GameObject m_InitUGSPanel;
    [SerializeField] GameObject m_NotJoinedPanel;
    [SerializeField] GameObject m_JoinedPanel;

    [Header("--- Create Lobby UI ---")]      
    [SerializeField] TMP_InputField m_LobbyNameInput;
    [SerializeField] Toggle m_PrivacyToggle;
    [SerializeField] TMP_InputField m_MaxPlayersInput;
    [SerializeField] TMP_Dropdown m_GameModeDropdown;

    [Header("--- Lobby Info---")]
    [SerializeField] Transform m_LobbiesParent;
    [SerializeField] GameObject m_LobbyInfoPrefab;

    [Header("--- Misc UI ---")]
    [SerializeField] TMP_InputField m_LobbyCodeField;
    [SerializeField] TMP_Dropdown m_UpdateGameModeDropdown;

    // todo - fix join button
    string m_PlayerName;
    List<Lobby> m_LobbyList = new List<Lobby>();
    Lobby m_JoinedLobby;

    static int MAX_LOBBIES_SHOWN = 10;

    public class OnLobbyListChangedEventArgs : EventArgs
    {
        public List<Lobby> lobbyList;
    }
    public event EventHandler<OnLobbyListChangedEventArgs> OnLobbyListChanged;

   // public enum eLobbyState { PRE_INIT, LOBBY_SEARCH, IN_LOBBY }    
   // public eLobbyState m_LobbyState = eLobbyState.PRE_INIT;    
    public void OnClickInitUGS()
    {
        if(UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            InitalizeAuthenticationAsync();
        }        
    }

    //RefreshLobbyList("OnGUI() button");
    //CreateLobby(nameWanted, maxPlayersWanted, privacyWanted, gameModeWanted);


    private void Awake()
    {
        m_InitUGSPanel.SetActive(true);
        m_NotJoinedPanel.SetActive(false);
        m_JoinedPanel.SetActive(false);
    }
    // Start is called before the first frame update
    void Start()
    {        
        m_PlayerName = (char)UnityEngine.Random.Range(65, 90) + "-" + UnityEngine.Random.Range(1, 99).ToString();
        OnLobbyListChanged += LobbyListChanged;        
    }

    

    async void InitalizeAuthenticationAsync()
    {
        var options = new InitializationOptions();
        options.SetProfile(m_PlayerName);

        await UnityServices.InitializeAsync(options);

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in. PlayerID: " + AuthenticationService.Instance.PlayerId);
            m_InitUGSPanel.SetActive(false);
            m_NotJoinedPanel.SetActive(true);
            RefreshLobbyList("UGS Sign in callback");
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();

    }

    // notes from CM project
    // refresh lobbies on (QueryLobbiesAsync - to discover lobbies)
    // 1) button click
    // 2) On Authenticate
    // 3) On a timer (I'm thinking I might go with the Update() version 
    // update lobby on (UpdateLobbyAsync - The lobby host is the only player that can update the lobby’s data.):
    // updating game mode (reassign m_Joined)
    async void UpdateLobbyInfo()
    {
        if (m_JoinedLobby == null) { Debug.LogWarning("Not joined in a lobby you can refresh."); return; }

        m_JoinedLobby = await LobbyService.Instance.UpdateLobbyAsync(m_JoinedLobby.Id, new UpdateLobbyOptions());
    }

    private void LobbyListChanged(object sender, OnLobbyListChangedEventArgs e)
    {
        Debug.Log("LobbyListChanged() sender: " + sender.ToString());
        DestroyLobbiesList();
        m_LobbyList = e.lobbyList;
        if(m_LobbyList.Count > 0)
        {
            Debug.Log("fill in list shithead");            
            foreach(Lobby lobby in m_LobbyList)
            {
                Transform lobbyItem = Instantiate(m_LobbyInfoPrefab.transform, m_LobbiesParent);
                lobbyItem.gameObject.SetActive(true);
                lobbyItem.GetComponent<LobbyItem>().Init(lobby);                
            }
        }
    }

    private void DestroyLobbiesList()
    {
        foreach (Transform child in m_LobbiesParent)
        {
            Destroy(child.gameObject);
        }
    }

    /*
     [Header("--- Lobby Info---")]
    [SerializeField] Transform m_LobbiesParent;
    [SerializeField] GameObject m_LobbyInfoPrefab;
     */

    public void OnClickRefreshLobbies()
    {
        RefreshLobbyList("Button");
    }
    public async void RefreshLobbyList(string caller)
    {
        Debug.Log("RefreshLobbyList() from: " + caller);
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions();
            options.Count = MAX_LOBBIES_SHOWN;

            // Filters are for open lobbies only
            options.Filters = new List<QueryFilter>
            {
                new QueryFilter(
                    field: QueryFilter.FieldOptions.AvailableSlots,
                    value: "0",
                    op: QueryFilter.OpOptions.GT )
                // todo - add game type
            };
            options.Order = new List<QueryOrder>
            {
                new QueryOrder(
                    asc: false,
                    field: QueryOrder.FieldOptions.Created )                
            };

            QueryResponse response = await Lobbies.Instance.QueryLobbiesAsync(options);

            //Debug.Log("Just queried lobbies");
            OnLobbyListChanged?.Invoke(this, new OnLobbyListChangedEventArgs { lobbyList = response.Results });
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }        
    }

    public void OnClickUpdateGameMode()
    {
        string newGameMode = ((GameMode)m_UpdateGameModeDropdown.value).ToString();
        UpdateLobbyGameMode(newGameMode);
    }

    async void UpdateLobbyGameMode(string gameMode)
    {
        try
        {
            Debug.Log("UpdateLobbyGameMode() new mode: " + gameMode);
            UpdateLobbyOptions options = new UpdateLobbyOptions();
            options.Data = new Dictionary<string, DataObject>
            {
                {KEY_GAME_MODE, new DataObject(DataObject.VisibilityOptions.Public, gameMode) }
            };
            m_JoinedLobby = await LobbyService.Instance.UpdateLobbyAsync(m_JoinedLobby.Id, options);
            RefreshJoinedScreen();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Can't UpdateLobbyGameMode(): " + gameMode);
        }
    }

    public enum GameMode
    {
        CaptureTheFlag,
        Deathmatch
    }

    public const string KEY_PLAYER_NAME = "PlayerName";    
    public const string KEY_GAME_MODE = "GameMode";
    private Player GetPlayer()
    {
        return new Player(AuthenticationService.Instance.PlayerId, null, new Dictionary<string, PlayerDataObject> {
            { KEY_PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, m_PlayerName) },
           // { KEY_PLAYER_CHARACTER, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, PlayerCharacter.Marine.ToString()) } todo - add character
        });
    }

    public void OnClickJoinLobbyByCode()
    {
        string lobbyCode = m_LobbyCodeField.text;
        JoinLobbyByCode(lobbyCode);
    }

    async void JoinLobbyByCode(string lobbyCode)
    {
        try
        {
            Debug.Log("JoinLobbyByCode(): " + lobbyCode);
            Player player = GetPlayer();
            m_JoinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, new JoinLobbyByCodeOptions
            {
                Player = player
            });
            // monewlobby 3 - todo - fix this
            m_NotJoinedPanel.SetActive(false);
            m_JoinedPanel.SetActive(true);
            RefreshJoinedScreen();
        } catch(LobbyServiceException e)
        {
            Debug.LogError("Can't join lobby by code: " + e.Message);
        }
    }   
    
    public async void JoinLobby(Lobby lobby)
    {
        try
        {
            Player player = GetPlayer();
            m_JoinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, new JoinLobbyByIdOptions
            {
                Player = player
            });
            Debug.Log("Just joined lobby: " + m_JoinedLobby.Name);
            // monewlobby 2 - todo - fix this
            m_NotJoinedPanel.SetActive(false);
            m_JoinedPanel.SetActive(true);
            RefreshJoinedScreen();
        } catch(LobbyServiceException e)
        {
            Debug.LogError("Failed to join lobby: " + e.Message);
        }
    }
    async void QuickJoinLobby()
    {
        try
        {
            //Debug.Log("QuickJoinLobby()");

            QuickJoinLobbyOptions options = new QuickJoinLobbyOptions
            {
                Player = GetPlayer(),
            };
            m_JoinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);
            Debug.Log("quick joined lobby with code: " + m_JoinedLobby.LobbyCode);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to quick join lobby: " + e.Message);
        }
    }
   
    public void OnClickCreateLobby()
    {                
        if (m_LobbyNameInput.text.IsNullOrEmpty()) { Debug.LogError("Need valid name to create lobby: " + m_LobbyNameInput.text); return; }
        int maxPlayers;
        int.TryParse(m_MaxPlayersInput.text, out maxPlayers);
        if (maxPlayers <= 0 || maxPlayers > 5) { Debug.LogError("Invalid number of players to create lobby: " + maxPlayers); return; }

        string s = "Want to create lobby named: " + m_LobbyNameInput.text + ", Private? " + m_PrivacyToggle.isOn;
        s += ", max players: " + maxPlayers.ToString() + ", game mode: " + ((GameMode)m_GameModeDropdown.value).ToString();
        Debug.Log(s);

        CreateLobby(m_LobbyNameInput.text, maxPlayers, m_PrivacyToggle.isOn, (GameMode)m_GameModeDropdown.value);
    }   

    async void CreateLobby(string lobbyName, int maxPlayers, bool isPrivate, GameMode gameMode)
    {        
        try
        {            
            Debug.Log("CreateLobby() name: " + lobbyName + ", max players: " + maxPlayers + ", private? " + isPrivate + ", mode: " + gameMode);

            Player player = GetPlayer();
            Dictionary<string, DataObject> data = new Dictionary<string, DataObject>
            {
                { KEY_GAME_MODE, new DataObject(DataObject.VisibilityOptions.Public, gameMode.ToString()) }
            };

            CreateLobbyOptions options = new CreateLobbyOptions
            {
                Player = player,
                IsPrivate = isPrivate,
                Data = data
            };

            m_JoinedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);            
            StartCoroutine(LobbyHeartbeat());

            Debug.Log("Just created a lobby: " + m_JoinedLobby.Name + ", maxPlayers: " + m_JoinedLobby.MaxPlayers + ", privacy?: " + m_JoinedLobby.IsPrivate + ", Id: " + m_JoinedLobby.Id + ", Code: " + m_JoinedLobby.LobbyCode);
            // monewlobby 1
            m_NotJoinedPanel.SetActive(false);
            m_JoinedPanel.SetActive(true);
            RefreshJoinedScreen();

        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to create lobby: " + e.Message);
        }        
    }

    public void OnClickRefreshJoinedScreen()
    {
        RefreshJoinedScreen();
    }

    async void RefreshJoinedScreen()
    {
        Debug.Log("refresh joined screen");
        if(m_JoinedLobby == null) { Debug.LogError("Null joined lobby can't refresh screen."); return; }

        m_JoinedLobby = await LobbyService.Instance.GetLobbyAsync(m_JoinedLobby.Id);

        TMP_Text lobbyText = m_JoinedPanel.transform.GetChild(1).GetChild(0).GetComponent<TMP_Text>();
        lobbyText.text = m_JoinedLobby.Name + ": " + m_JoinedLobby.Players.Count + "/" + m_JoinedLobby.MaxPlayers + " - " + m_JoinedLobby.Data[KEY_GAME_MODE].Value + " - " + m_JoinedLobby.LobbyCode;

        Debug.Log("num players: " + m_JoinedLobby.Players.Count);

        TMP_Text playersText = m_JoinedPanel.transform.GetChild(1).GetChild(1).GetComponent<TMP_Text>();
        playersText.text = "";
        foreach(Player player in m_JoinedLobby.Players)
        {
            if (IsPlayerLobbyHost(player)) playersText.text += "* ";
            playersText.text += player.Data[KEY_PLAYER_NAME].Value + "\n";
        }
    }

    public bool IsPlayerLobbyHost(Player player)
    {
        if (m_JoinedLobby == null) { Debug.LogError("No joined lobby."); return false; }

        return m_JoinedLobby.HostId == player.Id;
    }

    public bool IsLobbyHost()
    {
        if (m_JoinedLobby == null) { Debug.LogError("No joined lobby."); return false; }

        return m_JoinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }


    IEnumerator LobbyHeartbeat()
    {
        while(true)
        {
            yield return new WaitForSeconds(15);
            LobbyService.Instance.SendHeartbeatPingAsync(m_JoinedLobby.Id);
        }
    }

    

   // [Header("---Create Lobby Info:---")]
    /*public string nameWanted = "My Lobby";
    public int maxPlayersWanted = 2;
    public bool privacyWanted = false;
    public GameMode gameModeWanted = GameMode.CaptureTheFlag;
    string lobbyCode = "Lobby Code";*/

    /*
     if (GUI.Button(new Rect(buttonX, 0, buttonW, buttonH), "Refresh Lobby List"))
            {
                RefreshLobbyList("OnGUI() button");
            }
            if (GUI.Button(new Rect(buttonX, buttonH, buttonW, buttonH), "Create Lobby"))
            {
                CreateLobby(nameWanted, maxPlayersWanted, privacyWanted, gameModeWanted);
            }
     */

    private void Update()
    {
        
    }
#if false
    [Header("---GUI---")]
    public Texture joinButton;
    public Texture lobbyBG;
    public GUIStyle lobbyStyle;
   // public GUIStyle textFieldStyle;
    
    private void OnGUI()
    {        
        int buttonW = 150;
        int buttonH = 50;
        int buttonX = Screen.width - buttonW;
        int buttonY = 0;

        if(UnityServices.State != ServicesInitializationState.Initialized)
        {
            if (GUI.Button(new Rect(buttonX, 0, buttonW, buttonH), "UGS: " + UnityServices.State.ToString()))
            {
                switch (UnityServices.State)
                {
                    case ServicesInitializationState.Uninitialized:
                        InitalizeAuthenticationAsync();
                        break; 
                    case ServicesInitializationState.Initializing:
                        Debug.Log("Unity Services is initializing...");
                        break;                    
                }
            }
            return;
        }
       
        if(m_JoinedLobby != null)
        {
            string role = (IsLobbyHost() == true ? "Host" : "Client");
            string clientInfo = "You are " + role + ".\n";
            clientInfo += GetJoinedLobbyInfo();
            GUILayout.Box(clientInfo, lobbyStyle);
            if(IsLobbyHost() == true)
            {
                if (GUI.Button(new Rect(buttonX, 0, buttonW, buttonH), "Refresh Lobby Info"))
                {
                    UpdateLobbyInfo();
                }
            }            
        }
        else
        {   // not in a lobby
            if (GUI.Button(new Rect(buttonX, 0, buttonW, buttonH), "Refresh Lobby List"))
            {
                RefreshLobbyList("OnGUI() button");
            }
            if (GUI.Button(new Rect(buttonX, buttonH, buttonW, buttonH), "Create Lobby"))
            {
                CreateLobby(nameWanted, maxPlayersWanted, privacyWanted, gameModeWanted);
            }            

            int lobbiesW = 400;
            int lobbiesH = lobbyStyle.fontSize + 20;
            int lobbiesX = Screen.width / 2 - lobbiesW / 2;
            int lobbiesY = 200;
            int joinW = 100;
            int joinH = lobbiesH - 10;
          
            int y = lobbiesY - lobbiesH - 10;
            lobbyCode = GUI.TextField(new Rect(lobbiesX, lobbiesY - lobbiesH - 10, lobbiesW, lobbiesH), lobbyCode, lobbyStyle);
            if (lobbyCode.Length == 6)
            {
                if (GUI.Button(new Rect(lobbiesX + lobbiesW - joinW - 5, y + (lobbiesH - joinH) / 2, joinW, joinH), joinButton))
                {
                    Debug.Log("lskdf");
                    JoinLobbyByCode(lobbyCode);
                }
            }

            if (m_LobbyList.Count > 0)
            {                
                if (GUI.Button(new Rect(buttonX, buttonH*2, buttonW, buttonH), "Quick Join Lobby"))
                {
                    QuickJoinLobby();
                }

                int lobbyIndex = 0;
                foreach (Lobby lobby in m_LobbyList)
                {
                    string label = lobby.Name + ": " + lobby.Players.Count + "/" + lobby.MaxPlayers;
                    y = lobbiesY + (lobbiesH * lobbyIndex);                    
                    Rect labelRect = new Rect(lobbiesX, y, lobbiesW, lobbiesH);
                    GUI.Label(labelRect, label, lobbyStyle);
                    if (GUI.Button(new Rect(lobbiesX + lobbiesW - joinW - 5, y + (lobbiesH - joinH) / 2, joinW, joinH), joinButton))
                    {
                        JoinLobby(lobby);
                    }
                }                                      
            }
        }                      
    } 
#endif

    string GetJoinedLobbyInfo()
    {
        if(m_JoinedLobby == null) { Debug.LogWarning("Not joined in a lobby you can get info."); return "ERROR"; }

        string info = "Name: " + m_JoinedLobby.Name + ", Private?: " + m_JoinedLobby.IsPrivate;
        info += ", Mode: " + m_JoinedLobby.Data[KEY_GAME_MODE].Value;
        info += ", Code: " + m_JoinedLobby.LobbyCode + "\n";
        info += "---Players: " + m_JoinedLobby.Players.Count + "---\n";
        foreach (Player player in m_JoinedLobby.Players)
        {            
            info += "Name: " + player.Data[KEY_PLAYER_NAME].Value + "\n";
        }

        return info;
    }

    
}
