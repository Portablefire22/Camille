using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;

namespace Camille.Xmpp;

public class Message : BaseChatElement<Message>
{
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    private string _type;

    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    private string _body;

    public Message()
    {
        _type = "chat";
        _body = "INTERNAL DEBUG MESSAGE, VALUE WAS NOT SET";
    }
    
    public Message(string type, string id, string? from, string? to, string body)
    {
        _type = type;
        _id = id;
        _from = from;
        _to = to;
        _body = body;
    }

    public string? GetRecipient()
    {
        return _to;
    }

    public string? GetSender()
    {
        return _from;
    }

    public void SetSender(string from)
    {
        _from = from;
    }
    
    public override void Send(StreamWriter writer)
    {
        string xml = $"<message id='{_id}' type='{_type}' ";
        if (_from != null) xml += $"from='{_from}' ";
        if (_to != null) xml += $"to='{_to}' ";
        xml += $"><body>{_body}</body></message>";
        writer.Write(xml);
    }

    public Message(XmlReader reader)
    {
        _to = reader.GetAttribute("to") ?? throw new XmlException("message does not have a 'to' attribute");
        _type = reader.GetAttribute("type") ?? throw new XmlException("message type cannot be null");
        _from = reader.GetAttribute("from");
        _id = reader.GetAttribute("id");

        if (!reader.Read() || reader.NodeType == XmlNodeType.EndElement)
        {
            _body = "";
        }
        // We should check if this is a body, but I don't exactly know what can be contained in a message yet
        if (!reader.Read()) throw new Exception("could not read message contents");
        _body = reader.Value;
    }
    
    public static Message? FromJson(string json)
    {
        return JsonSerializer.Deserialize<Message>(json);
    }

    public override string ToJson()
    {
        return JsonSerializer.Serialize(this);
    }
}