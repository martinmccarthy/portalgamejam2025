using Photon.Pun;
using UnityEngine;

//rn this just implements the start game button to check photon stuff
//todo: add full menu functionality (INCLUDE CONTROLS)

public class MenuController : MonoBehaviour
{
    public MultiplayerManager m_multiplayerManager;
    [SerializeField] private string nickname;

    // figure we could just call the multiplayer manager but if we want to pass persistent data through
    // this might be the right route idk
    public void PlayGame()
    {
        PhotonNetwork.NickName = nickname;
        m_multiplayerManager.Connect();
    }
}
