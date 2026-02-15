using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Sunrise.API.Serializable.Request;

public class CreateClanRequest
{
    [Required]
    [MinLength(2)]
    [MaxLength(32)]
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [MaxLength(2048)]
    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }
}
