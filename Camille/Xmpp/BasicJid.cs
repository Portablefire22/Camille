namespace Camille.Xmpp;

public class BasicJid
{

    private string _jid;
    private string _username;
    private string _password;

    public bool IsAuthenticated { get; set; }

    public string Jid
    {
        get => _jid;
        set => _jid = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string Username
    {
        get => _username;
        set => _username = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string Password
    {
        get => _password;
        set => _password = value ?? throw new ArgumentNullException(nameof(value));
    }


    public BasicJid(string jid, string username, string password)
    {
        this._jid = jid;
        this._username = username;
        this._password = password;
    }
}