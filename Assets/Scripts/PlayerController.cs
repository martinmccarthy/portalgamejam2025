using Photon.Pun;
using UnityEngine;
using TMPro;

public class PlayerController : MonoBehaviour
{
    public int movementSpeed = 1; // for testing ts

    public string playerName = "Martinotaco";
    public TMP_Text nametag;
    private void Start()
    {
        string defaultName = "luh idiot";
        PhotonNetwork.NickName = playerName != string.Empty ? playerName : defaultName;
    
        nametag.text = PhotonNetwork.NickName;
    }

    private void Update()
    {
        float inputH = Input.GetAxis("Horizontal");

        transform.Translate(inputH * movementSpeed * Time.deltaTime * Vector3.right);
    }

}
