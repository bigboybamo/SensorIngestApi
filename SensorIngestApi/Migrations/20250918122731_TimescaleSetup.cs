using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SensorIngestApi.Migrations
{
    /// <inheritdoc />
    public partial class TimescaleSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_readings",
                table: "readings");

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "readings",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_readings",
                table: "readings",
                columns: new[] { "Id", "ts", "device_id" });

            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS timescaledb;");

            // 2) Turn 'readings' into a hypertable partitioned by time + device
            migrationBuilder.Sql(@"
        SELECT create_hypertable('readings','ts','device_id', 4, if_not_exists => TRUE);
    ");

            // 3) Compression + retention policies for raw readings
            migrationBuilder.Sql(@"
        ALTER TABLE readings SET (
          timescaledb.compress,
          timescaledb.compress_orderby = 'ts DESC',
          timescaledb.compress_segmentby = 'device_id'
        );
        SELECT add_compression_policy('readings', INTERVAL '7 days');   -- compress >7d
        SELECT add_retention_policy('readings',  INTERVAL '30 days');   -- drop    >30d
    ");

            // 4) 1-second continuous aggregate for fast charts
            migrationBuilder.Sql(@"
        CREATE MATERIALIZED VIEW IF NOT EXISTS readings_1s
        WITH (timescaledb.continuous) AS
        SELECT time_bucket(INTERVAL '1 second', ts) AS bucket,
               device_id,
               COUNT(*)   AS cnt,
               AVG(value) AS avg,
               MIN(value) AS min,
               MAX(value) AS max
        FROM readings
        GROUP BY bucket, device_id
        WITH NO DATA;

        -- auto-refresh policy
        SELECT add_continuous_aggregate_policy(
          'readings_1s',
          start_offset => INTERVAL '7 days',
          end_offset   => INTERVAL '1 minute',
          schedule_interval => INTERVAL '1 minute'
        );
    ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_readings",
                table: "readings");

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "readings",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_readings",
                table: "readings",
                column: "Id");

            migrationBuilder.Sql("DROP MATERIALIZED VIEW IF EXISTS readings_1s;");
            migrationBuilder.Sql("SELECT remove_continuous_aggregate_policy('readings_1s');");
            migrationBuilder.Sql("SELECT remove_compression_policy('readings');");
            migrationBuilder.Sql("SELECT remove_retention_policy('readings');");
        }
    }
}
