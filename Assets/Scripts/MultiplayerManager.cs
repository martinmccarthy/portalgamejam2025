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
        PhotonNetwork.GameVersion = version;
        PhotonNetwork.ConnectUsingSettings();
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
        PhotonNetwork.CreateRoom("TestRoom", new RoomOptions { MaxPlayers = maxPlayersPerRoom });
    }

    public override void OnJoinedRoom()
    {
        var go = PhotonNetwork.Instantiate("Player", Vector3.zero, Quaternion.identity, 0);
        // PhotonNetwork.LoadLevel("PlayerRoom");
    }
}
