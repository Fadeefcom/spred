using System.ComponentModel.DataAnnotations;

namespace TrackService.Models.Entities;

public class TrackLink
{
    [StringLength(50)]
    public string Platform { get; set; } = string.Empty;

    [StringLength(100)]
    public string Value { get; set; } = string.Empty;
}