using System;
using System.Collections.Generic;

namespace SWP.Domain.Models.Entity;

public class MedicalRecord
{
    public int MedicalRecordId { get; set; }

    public string? RecordType { get; set; }

    public string? Diagnosis { get; set; }

    public string? Symptoms { get; set; }

    public string? Treatment { get; set; }

    public string? Medication { get; set; }

    public string? Dosage { get; set; }

    public DateTime RecordDate { get; set; }

    public DateOnly? FollowUpDate { get; set; }

    public string? Status { get; set; }

    public decimal? Cost { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public int HorseId { get; set; }

    public int? CreateBy { get; set; }

    public virtual User? CreateByNavigation { get; set; }

    public virtual Horse Horse { get; set; } = null!;
}
