using System.Text.Json;
using System.Text.Json.Serialization;

namespace Camille.Xmpp;

public class Message : XmppElement
{
    [JsonInclude]
    private string _type;
    [JsonInclude]
    private string _id;
    [JsonInclude]
    private string? _from;
    [JsonInclude]
    private string? _to;
    [JsonInclude]
    private string _body;

    public Message(string type, string id, string? from, string? to, string body)
    {
        _type = type;
        _id = id;
        _from = from;
        _to = to;
        _body = body;
    }

    public override void Send(StreamWriter writer)
    {
        string xml = $"<message id='{_id}' type='{_type}' ";
        if (_from != null) xml += $"from='{_from}' ";
        if (_to != null) xml += $"to='{_to}' ";
        xml += $"><body>{_body}</body></message>";
        writer.Write(xml);
    }

    public static Message? FromJson(string json)
    {
        return JsonSerializer.Deserialize<Message>(json);
    }
    
    public string ToJson()
    {
        return JsonSerializer.Serialize(this);
    }
}