using System.Text.Json;
using System.Text.Json.Serialization;

namespace Camille.Xmpp;

public abstract class BaseChatElement<T> : XmppElement
{
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    protected string? _id;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    protected string? _from;
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    protected string? _to;

    public abstract string ToJson();
}