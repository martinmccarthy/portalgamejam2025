using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using ExitGames.Client.Photon;
using System.Collections;

public class RoundManager : MonoBehaviourPunCallbacks
{
    [SerializeField] float roundTime = 600f;
    [SerializeField] float startDelay = 5f;
    [SerializeField] TMP_Text text;

    const string RoomKey_KillerActor = "KillerActor";
    const string PlayerKey_IsKiller = "IsKiller";

    float timer;
    bool roundActive;
    int killerActor = -1;

    RectTransform canvasRoot;
    bool killerBannerShown;

    void Start()
    {
        canvasRoot = GetComponentInChildren<Canvas>(true)?.transform as RectTransform;
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("called rpc to master");
            photonView.RPC(nameof(CheckAllReady), RpcTarget.MasterClient);
        }
    }

    [PunRPC]
    void CheckAllReady()
    {
        photonView.RPC(nameof(StartRound), RpcTarget.All, startDelay);
    }

    [PunRPC]
    void StartRound(float delay)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            SelectKillerForThisRound();
        }

        roundActive = false;
        timer = delay;

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(RoomKey_KillerActor, out var act))
            killerActor = (int)act;

        SyncLocalKillerFlag();
    }

    void Update()
    {
        if (!PhotonNetwork.IsConnected) return;

        if (!roundActive)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                roundActive = true;
                timer = roundTime;
            }
        }
        else
        {
            timer -= Time.deltaTime;
            if (text) text.text = $"Round Time: {Mathf.Ceil(timer)}";
            if (timer <= 0f && PhotonNetwork.IsMasterClient)
                photonView.RPC(nameof(EndRound), RpcTarget.All);
        }
    }

    [PunRPC]
    void EndRound()
    {
        roundActive = false;
        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.LoadLevel(SceneManager.GetActiveScene().buildIndex);
    }

    void SelectKillerForThisRound()
    {
        var list = PhotonNetwork.PlayerList;
        if (list == null || list.Length == 0) return;

        var chosen = list[Random.Range(0, list.Length)];
        killerActor = chosen.ActorNumber;
        Debug.Log($"Chosen: {chosen}");
        var roomProps = new ExitGames.Client.Photon.Hashtable { { RoomKey_KillerActor, killerActor } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);

        foreach (var p in list)
        {
            bool isKiller = p.ActorNumber == killerActor;
            var props = new ExitGames.Client.Photon.Hashtable { { PlayerKey_IsKiller, isKiller } };
            p.SetCustomProperties(props);
        }
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable changed)
    {
        if (changed.ContainsKey(RoomKey_KillerActor))
        {
            killerActor = (int)changed[RoomKey_KillerActor];
            SyncLocalKillerFlag();
        }
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        photonView.RPC(nameof(StartRound), newPlayer, Mathf.Max(0f, roundActive ? timer : timer));
    }

    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (targetPlayer.IsLocal && changedProps.ContainsKey(PlayerKey_IsKiller))
            SyncLocalKillerFlag();
    }

    void SyncLocalKillerFlag()
    {
        bool isKiller = PhotonNetwork.LocalPlayer.ActorNumber == killerActor;
        bool has = PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(PlayerKey_IsKiller, out var v) && v is bool b && b;
        if (has != isKiller)
        {
            var props = new ExitGames.Client.Photon.Hashtable { { PlayerKey_IsKiller, isKiller } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
        if (isKiller && !killerBannerShown)
        {
            killerBannerShown = true;
            StartCoroutine(ShowKillerNotice());
        }
    }

    IEnumerator ShowKillerNotice()
    {
        if (canvasRoot == null) yield break;

        var go = new GameObject("KillerNotice", typeof(RectTransform));
        go.transform.SetParent(canvasRoot, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(1200, 200);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = "YOU ARE THE KILLER";
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 64f;
        tmp.raycastTarget = false;

        yield return new WaitForSeconds(3f);
        Destroy(go);
    }

    public bool LocalIsKiller()
    {
        return PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(PlayerKey_IsKiller, out var v) && (bool)v;
    }

    public int KillerActorNumber() => killerActor;
}
