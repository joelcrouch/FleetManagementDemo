/* ======================== SQL Server Diagnostics - Fleet Management ===================
Author: Joel Purpose: Monitoring queries for performance, growth, and health checks. 
------------------------------------------- */  
/* currently executing queries? */
SELECT 
    r.session_id,
    r.status,
    r.command,
    t.text AS sql_text,
    r.cpu_time,
    r.total_elapsed_time,
    r.reads,
    r.writes
FROM sys.dm_exec_requests r
CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) t
WHERE r.session_id > 50 -- exclude system sessions
ORDER BY r.total_elapsed_time DESC;

/*  Top 10 slowest queries (requires Query Store enabled) */
SELECT TOP 10
    qsq.query_id,
    qt.query_sql_text,
    qs.avg_duration AS avg_duration_ms,
    qs.avg_cpu_time AS avg_cpu_ms,
    qs.count_executions
FROM sys.query_store_query_text AS qt
JOIN sys.query_store_query AS qsq ON qt.query_text_id = qsq.query_text_id
JOIN sys.query_store_plan AS qsp ON qsq.query_id = qsp.query_id
JOIN sys.query_store_runtime_stats AS qs ON qsp.plan_id = qs.plan_id
ORDER BY qs.avg_duration DESC;

/*Database size and growth */
SELECT
    DB_NAME(database_id) AS database_name,
    SUM(size) * 8 / 1024 AS size_mb
FROM sys.master_files
GROUP BY database_id;

/*Index fragmentation check */
SELECT
    dbschemas.[name] AS schema_name,
    dbtables.[name] AS table_name,
    dbindexes.[name] AS index_name,
    indexstats.avg_fragmentation_in_percent
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') AS indexstats
JOIN sys.tables dbtables ON dbtables.[object_id] = indexstats.[object_id]
JOIN sys.schemas dbschemas ON dbtables.[schema_id] = dbschemas.[schema_id]
JOIN sys.indexes dbindexes ON dbindexes.[object_id] = indexstats.[object_id]
    AND indexstats.index_id = dbindexes.index_id
WHERE indexstats.avg_fragmentation_in_percent > 10
ORDER BY indexstats.avg_fragmentation_in_percent DESC;

/*Active connections by database */
SELECT
    DB_NAME(dbid) AS database_name,
    COUNT(dbid) AS active_connections
FROM sys.sysprocesses
WHERE dbid > 0
GROUP BY dbid
ORDER BY active_connections DESC;