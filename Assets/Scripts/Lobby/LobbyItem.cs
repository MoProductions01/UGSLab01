using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyItem : MonoBehaviour
{
    private Lobby m_Lobby;    

    public void Init(Lobby lobby)
    {
        m_Lobby = lobby;
        TMP_Text lobbyText = transform.GetChild(0).GetComponent<TMP_Text>();
        lobbyText.text = lobby.Name + ": " + lobby.Players.Count + "/" + lobby.MaxPlayers + "\t" + lobby.Data[StreamlineLobby.KEY_GAME_MODE].Value; // todo - rename KEY_GAME_MODE
    }

    public void OnClickJoinLobby()
    {
        GameObject.FindObjectOfType<StreamlineLobby>().JoinLobby(m_Lobby);
    }
}
