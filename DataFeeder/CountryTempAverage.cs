using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataFeeder;

[Table("temperatures")]
public record CountryTempAverage
{
    [Required]
    [Column("date")]
    public DateOnly Date { get; set; }

    [Required]
    [Column("country")]
    public string Country { get; set; } = string.Empty;

    [Column("avgtemperature")]
    public double? AverageTemperature { get; set; }
    [Column("uncertainty")]
    public double? AverageTemperatureUncertainty { get; set; }
}
