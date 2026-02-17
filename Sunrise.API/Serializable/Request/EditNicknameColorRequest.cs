using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Sunrise.API.Serializable.Request;

public class EditNicknameColorRequest
{
    [Required]
    [JsonPropertyName("nickname_color")]
    [RegularExpression("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})$")]
    public string NicknameColor { get; set; } = string.Empty;
}
