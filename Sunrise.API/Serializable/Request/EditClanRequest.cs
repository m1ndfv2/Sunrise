using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Sunrise.API.Serializable.Request;

public class EditClanNameRequest
{
    [Required]
    [MinLength(2)]
    [MaxLength(32)]
    [JsonPropertyName("name")]
    public string Name { get; set; }
}

public class EditClanAvatarRequest
{
    [MaxLength(2048)]
    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }
}

public class EditClanDescriptionRequest
{
    [MaxLength(2048)]
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
