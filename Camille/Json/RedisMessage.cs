using System.Text.Json;
using System.Text.Json.Serialization;
using Camille.Xmpp;

namespace Camille.Json;

public class RedisMessage(string type, string o)
{
    public RedisMessage() : this("", "")
    {
        ;
    }

    [JsonInclude]
    public string Type { get; set; } = type;

    [JsonInclude]
    public string Object { get; set; } = o;

    public static RedisMessage? FromJson(string json)
    {
        return JsonSerializer.Deserialize<RedisMessage>(json);
    }
    
    public string ToJson()
    {
        return JsonSerializer.Serialize(this);
    }
}