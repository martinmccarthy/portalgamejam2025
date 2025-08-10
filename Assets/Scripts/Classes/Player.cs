public class Player
{
    public string NickName;
    public int ActorNumber;
    public bool IsKiller;
    public int Score;
    public bool IsReady;

    public Player(Photon.Realtime.Player p, bool isKiller = false)
    {
        NickName = p.NickName;
        ActorNumber = p.ActorNumber;
        IsKiller = isKiller;
        Score = 0;
        IsReady = false;
    }
}
