using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviourPunCallbacks
{
    public List<Player> players = new();
    [SerializeField] GameObject m_playerPrefab;
    private void Start()
    {
        var go = PhotonNetwork.Instantiate("Player", Vector3.up, Quaternion.identity, 0);
        PhotonNetwork.LocalPlayer.TagObject = go;
    }


    public override void OnLeftRoom()
    {
        SceneManager.LoadScene(0);
    }
    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        if(PhotonNetwork.IsMasterClient)
        {
            Debug.Log($"Player joined: {newPlayer.NickName}");
        }
    }
}
