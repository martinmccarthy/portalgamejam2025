using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleRoundManager : MonoBehaviourPunCallbacks
{
    [SerializeField] float roundTime = 600f;
    [SerializeField] float startDelay = 5f;
    [SerializeField] TMP_Text text;

    float timer;
    bool roundActive;

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
            photonView.RPC(nameof(CheckAllReady), RpcTarget.All);
    }

    [PunRPC]
    void CheckAllReady()
    {
        if (PhotonNetwork.IsMasterClient)
            photonView.RPC(nameof(StartRound), RpcTarget.All, startDelay);
    }

    [PunRPC]
    void StartRound(float delay)
    {
        roundActive = false;
        timer = delay;
    }

    void Update()
    {
        if (!PhotonNetwork.IsConnected) return;

        if (!roundActive)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                roundActive = true;
                timer = roundTime;
            }
        }
        else
        {
            timer -= Time.deltaTime;
            text.text = $"Round Time: {Mathf.Ceil(timer)}";
            if (timer <= 0 && PhotonNetwork.IsMasterClient)
                photonView.RPC(nameof(EndRound), RpcTarget.All);
        }
    }

    [PunRPC]
    void EndRound()
    {
        roundActive = false;
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
