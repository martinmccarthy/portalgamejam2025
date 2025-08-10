using Photon.Pun;
using TMPro;
using UnityEngine;

//rn this just implements the start game button to check photon stuff
//todo: add full menu functionality (INCLUDE CONTROLS)

public class MenuController : MonoBehaviour
{
    [SerializeField] MultiplayerManager m_multiplayerManager;
    [SerializeField] TMP_Text m_nameInput;

    // figure we could just call the multiplayer manager but if we want to pass persistent data through
    // this might be the right route idk
    public void PlayGame()
    {
        PhotonNetwork.NickName = m_nameInput.text;
        m_multiplayerManager.StartGame();
    }
}
