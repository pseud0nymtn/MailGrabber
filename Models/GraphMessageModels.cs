using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MailGrabber.Models;

public partial class GraphMessagePage : ObservableObject
{
    [property: JsonPropertyName("value")]
    [ObservableProperty]
    private List<GraphMessageItem> value = [];

    [property: JsonPropertyName("@odata.nextLink")]
    [ObservableProperty]
    private string? nextLink;
}

public partial class GraphMessageItem : ObservableObject
{
    [ObservableProperty]
    private string? subject;

    [ObservableProperty]
    private DateTimeOffset? receivedDateTime;

    [ObservableProperty]
    private GraphRecipient? from;

    [ObservableProperty]
    private GraphRecipient? sender;
}

public partial class GraphRecipient : ObservableObject
{
    [ObservableProperty]
    private GraphEmailAddress? emailAddress;
}

public partial class GraphEmailAddress : ObservableObject
{
    [ObservableProperty]
    private string? address;

    [ObservableProperty]
    private string? name;
}
