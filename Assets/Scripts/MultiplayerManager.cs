using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class MultiplayerManager : MonoBehaviourPunCallbacks
{
    public byte maxPlayersPerRoom = 6;
    string version = "1";

    void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
    }


    public void Connect()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.JoinRandomRoom();
            return;
        }
        PhotonNetwork.ConnectUsingSettings();
        PhotonNetwork.GameVersion = version;
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log(nameof(OnConnectedToMaster));
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = maxPlayersPerRoom });
    }

    public override void OnJoinedRoom()
    {
        PhotonNetwork.LoadLevel("PlayerRoom");
    }
}
