using UnityEngine;
using Mirror;
using Steamworks;
using TMPro;

public class SteamLobby : MonoBehaviour
{
    public GameObject hostButton;
    public GameObject inviteButton;
    public TextMeshProUGUI debugLogText;

    private NetworkManager networkManager;

    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> lobbyEntered;

    private const string HOSTADDRESSKEY = "HostAdress";

    private CSteamID currentLobbyID;

    private void Start()
    {
        networkManager = GetComponent<NetworkManager>();

        if (!SteamManager.Initialized) return;

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
    }

    public void HostLobby()
    {
        hostButton.SetActive(false);
        inviteButton.SetActive(false);

        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, networkManager.maxConnections);
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            hostButton.SetActive(true);
            Debug.LogError("Lobby creation failed.");
            return;
        }

        Log("Lobby created successfully!");

        currentLobbyID = new CSteamID(callback.m_ulSteamIDLobby); 

        networkManager.StartHost();

        SteamMatchmaking.SetLobbyData(currentLobbyID, HOSTADDRESSKEY, SteamUser.GetSteamID().ToString());

        inviteButton.SetActive(false); 
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        Log("Received Steam invite, joining lobby...");
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {

        Log("[Steam] Lobby entered successfully. ID: " + callback.m_ulSteamIDLobby);
        
        currentLobbyID = new CSteamID(callback.m_ulSteamIDLobby); 

        if (NetworkServer.active) return;

        string hostAddress = SteamMatchmaking.GetLobbyData(currentLobbyID, HOSTADDRESSKEY);

        networkManager.networkAddress = hostAddress;
        networkManager.StartClient();

        hostButton.SetActive(false);
        inviteButton.SetActive(false);
    }

    private void Log(string message)
    {
        Debug.Log(message);
        if (debugLogText != null)
        {
            debugLogText.text += message + "\n";
        }
    }

    public void InviteFriends()
    {
        if (!SteamManager.Initialized || currentLobbyID == CSteamID.Nil)
        {
            Debug.LogWarning("Steam not initialized or lobby ID not set.");
            return;
        }

        Log("Opening Steam Invite Overlay...");
        SteamFriends.ActivateGameOverlayInviteDialog(currentLobbyID);
    }
}
