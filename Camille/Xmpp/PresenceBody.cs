using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace Camille.Xmpp;

[XmlRoot("body")]
public class PresenceBody
{
    [JsonInclude]
    [ XmlElement("profileIcon")]
    public int ProfileIcon { get;set; } = 1;

    [JsonInclude]
    [ XmlElement("level")]
    public int Level { get;set; }

    [JsonInclude]
    [XmlElement("wins")]
    public int Wins { get;set; }

    [JsonInclude]
    [XmlElement("leaves")]
    public int Leaves { get;set; }

    [JsonInclude]
    [XmlElement("odinWins")]
    public int OdinWins { get;set; }

    [JsonInclude]
    [XmlElement("odinLeaves")]
    public int OdinLeaves { get;set; }

    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [XmlElement("queueType")]
    public string? QueueType { get;set; }

    [JsonInclude]
    [XmlElement("rankedLosses")]
    public int RankedLosses { get;set; }

    [JsonInclude]
    [XmlElement("rankedRating")]
    public int RankedRating { get;set; }

    [JsonInclude] 
    [XmlElement("tier")]
    public string Tier { get;set; } = "Unranked";

    [JsonInclude]
    [XmlElement("gameStatus")]
    public string GameStatus { get;set; } = "outOfGame";

    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [XmlElement("statusMsg")]
    public string? StatusMessage { get;set; }
}