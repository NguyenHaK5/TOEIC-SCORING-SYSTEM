using System.Text.Json.Serialization;

namespace Toeic.Infrastructure.WritingAssessment;

internal class AnthropicMessageResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("content")]
    public List<AnthropicContentBlock>? Content { get; set; }

    [JsonPropertyName("usage")]
    public AnthropicUsage? Usage { get; set; }
}

internal class AnthropicContentBlock
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

internal class AnthropicUsage
{
    [JsonPropertyName("input_tokens")]
    public int? InputTokens { get; set; }

    [JsonPropertyName("output_tokens")]
    public int? OutputTokens { get; set; }
}
