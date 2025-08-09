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
        Player x = new("martinotaco", 100, (int)Colors.Blue);
        GameObject playerman = PhotonNetwork.Instantiate("Player", new Vector3(0f, 1f, 0f), Quaternion.identity);
        PlayerController p = playerman.GetComponent<PlayerController>();
        p.playerName = x.Name;
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
            LoadArena();
        }
    }

    void LoadArena()
    {
        if(!PhotonNetwork.IsMasterClient)
        {
            Debug.LogError("shit is happening but im not the master");
            return;
        }

        PhotonNetwork.LoadLevel("PlayerRoom");
    }    
}
