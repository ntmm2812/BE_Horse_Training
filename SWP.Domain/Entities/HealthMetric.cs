using System;
using System.Collections.Generic;

namespace SWP.Domain.Models.Entity;

public class HealthMetric
{
    public long MetricId { get; set; }

    public int HorseId { get; set; }

    public decimal? HeartRate { get; set; }

    public decimal? RespiratoryRate { get; set; }

    public decimal? BodyTemperature { get; set; }

    public decimal? BloodOxygen { get; set; }

    public decimal? SpeedKmh { get; set; }

    public decimal? DistanceKm { get; set; }

    public string? StressLevel { get; set; }

    public DateTime RecordedAt { get; set; }

    public string? DataSource { get; set; }

    public string? Notes { get; set; }

    public virtual Horse Horse { get; set; } = null!;
}
