using Photon.Pun;
using UnityEngine;
using TMPro;

public class PlayerController : MonoBehaviourPun, IPunObservable
{
    public int movementSpeed = 1;
    public TMP_Text nametag;

    Vector3 netPos;

    void Awake()
    {
        netPos = transform.position;
    }

    void Start()
    {
        nametag.text = photonView.Owner.NickName;
    }

    void Update()
    {
        if (photonView.IsMine)
        {
            float inputH = Input.GetAxis("Horizontal");
            transform.Translate(inputH * movementSpeed * Time.deltaTime * Vector3.right);
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, netPos, 10f * Time.deltaTime);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting) stream.SendNext(transform.position);
        else netPos = (Vector3)stream.ReceiveNext();
    }
}
