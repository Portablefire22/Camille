using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace Camille.Xmpp;

public class Presence : BaseChatElement<Presence>
{

    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    private PresenceBody? _body;

    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    private string? _show;
    [JsonInclude]
    private int _priority;

    public Presence()
    {
    }

    public Presence(XmlReader reader)
    {
        _to = reader.GetAttribute("to");
        _from = reader.GetAttribute("from");
        while (reader.Read() && !(reader.NodeType == XmlNodeType.EndElement && reader.Name == "presence"))
        {
            if (reader.NodeType == XmlNodeType.EndElement) continue;
            switch (reader.Name)
            {
                case "show":
                    reader.Read();
                    _show = reader.Value;
                    break;
                case "priority":
                    reader.Read();
                    _priority = int.Parse(reader.Value);
                    break;
                case "status":
                    reader.Read();
                    // Status is a string, so we need to parse it differently :)
                    var xml = reader.Value;
                    var ser = new XmlSerializer(typeof(PresenceBody));
                    using (TextReader read = new StringReader(xml))
                    {
                        _body = (PresenceBody?)ser.Deserialize(read);
                    }
                    break;
            } 
        }
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
    
    public static Presence? FromJson(string json)
    {
        return JsonSerializer.Deserialize<Presence>(json);
    }

    public override string ToJson()
    {
        return JsonSerializer.Serialize(this);
    }

    public override void Send(StreamWriter writer)
    {

        var str = $"<presence to='{_to}'>" +
                  $"<priority>{_priority}</priority>" +
                  $"<status";
        if (_body != null)
        {
            str += "<body>" +
                   $"<profileIcon>{_body.ProfileIcon}</profileIcon>" +
                   $"<level>{_body.Level}</level>" +
                   $"<wins>{_body.Wins}</wins>" +
                   $"<odinWins>{_body.OdinWins}</odinWins>" +
                   $"<odinLeaves>{_body.OdinLeaves}</odinLeaves>" +
                   $"<queueType/>" +
                   $"<rankedLosses>{_body.RankedLosses}</rankedLosses>" +
                   $"<rankedRating>{_body.RankedRating}</rankedRating>" +
                   $"<tier>{_body.Tier}</tier>" +
                   $"<gameStatus>{_body.GameStatus}</gameStatus>";

            if (_body.StatusMessage != null)
            {
                str += $"<statusMsg>{_body.StatusMessage}</statusMsg>";
            }
            else
            {
                str += "<statusMsg/>";
            }

            str += $"</body>";
        }
        str += $"</status>";
        if (_show != null) str += $"<show>{_show}</show>";
        str += $"<x xmlns='http://jabber.org/protocol/muc'/></presence>";
        writer.Write(str);
    }
}