using System.Text.Json.Serialization;

namespace MailGrabber.Models;

public sealed class GraphMessagePage
{
    [JsonPropertyName("value")]
    public List<GraphMessageItem> Value { get; set; } = [];

    [JsonPropertyName("@odata.nextLink")]
    public string? NextLink { get; set; }
}

public sealed class GraphMessageItem
{
    public string? Subject { get; set; }

    public DateTimeOffset? ReceivedDateTime { get; set; }

    public GraphRecipient? From { get; set; }

    public GraphRecipient? Sender { get; set; }
}

public sealed class GraphRecipient
{
    public GraphEmailAddress? EmailAddress { get; set; }
}

public sealed class GraphEmailAddress
{
    public string? Address { get; set; }

    public string? Name { get; set; }
}