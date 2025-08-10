using Photon.Pun;
using UnityEngine;

public class PortalEndpoint : MonoBehaviourPun, IPunInstantiateMagicCallback
{
    public PlayerController owner;
    public bool isLeft;

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        var d = info.photonView.InstantiationData;
        isLeft = (bool)d[0];
        var ownerView = PhotonView.Find((int)d[1]);
        if (ownerView) owner = ownerView.GetComponent<PlayerController>();
    }

    [PunRPC]
    public void NetMove(Vector3 pos, Quaternion rot)
    {
        transform.SetPositionAndRotation(pos, rot);
    }

    void OnTriggerEnter(Collider other)
    {
        var pc = other.GetComponentInParent<PlayerController>();
        if (pc && pc.photonView && pc.photonView.IsMine)
            pc.TryTeleportFrom(this);
    }
}