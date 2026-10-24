using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace SWP.Infrastructure.Persistence;

public static class InjuryWorkflowSchemaUpdater
{
    public static void Apply(ApplicationDbContext db)
    {
        using var stream = typeof(InjuryWorkflowSchemaUpdater).Assembly.GetManifestResourceStream(
            "SWP.Infrastructure.Data.Sql.InjuryWorkflow.sql") ?? throw new InvalidOperationException("Thiếu script InjuryWorkflow.sql.");
        using var reader = new StreamReader(stream);
        using var tx = db.Database.BeginTransaction();
        foreach (var batch in Regex.Split(reader.ReadToEnd(), @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(batch)) db.Database.ExecuteSqlRaw(batch);
        }
        tx.Commit();
    }
}
