using System.ComponentModel.DataAnnotations;

namespace Whisprr.Entities.Models;

public class SocialTopic
{
  public Guid Id { get; set; }

  public string[] Keywords { get; set; } = [];

  [Required]
  public required string Language { get; set; }
}