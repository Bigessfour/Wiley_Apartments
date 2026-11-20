using Microsoft.EntityFrameworkCore;

namespace Wiley.Apartments.Web.Data;

/// <summary>
/// SQLite data copied onto Postgres leaves identity sequences at 1 while rows already
/// use those ids. The next insert, including every audit row, then hits the primary key.
/// </summary>
public static class PostgresIdentitySequenceRepair
{
    private const string AlignSql = """
        DO $$
        DECLARE
          rec record;
          max_id bigint;
        BEGIN
          FOR rec IN
            SELECT
              pg_get_serial_sequence(format('%I.%I', n.nspname, c.relname), a.attname) AS seq,
              format('%I.%I', n.nspname, c.relname) AS tbl,
              a.attname AS col
            FROM pg_class c
            JOIN pg_namespace n ON n.oid = c.relnamespace
            JOIN pg_attribute a ON a.attrelid = c.oid AND a.attnum > 0 AND NOT a.attisdropped
            WHERE c.relkind = 'r'
              AND n.nspname NOT IN ('pg_catalog', 'information_schema')
              AND pg_get_serial_sequence(format('%I.%I', n.nspname, c.relname), a.attname) IS NOT NULL
          LOOP
            EXECUTE format('SELECT COALESCE(MAX(%I), 0) FROM %s', rec.col, rec.tbl) INTO max_id;
            IF max_id > 0 THEN
              PERFORM setval(rec.seq, max_id, true);
            END IF;
          END LOOP;
        END $$;
        """;

    public static async Task AlignAsync(ApartmentsDbContext db, CancellationToken cancellationToken = default)
    {
        if (!db.Database.IsNpgsql())
        {
            return;
        }

        var connection = db.Database.GetDbConnection();
        var openedHere = connection.State != System.Data.ConnectionState.Open;
        if (openedHere)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = AlignSql;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            if (openedHere)
            {
                await connection.CloseAsync();
            }
        }
    }
}
