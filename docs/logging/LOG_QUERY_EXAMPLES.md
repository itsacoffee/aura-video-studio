# Log Query Examples

This guide provides practical examples for querying and analyzing logs from the Aura application.

## Table of Contents

- [Using grep](#using-grep)
- [Using Log Aggregation Tools](#using-log-aggregation-tools)
- [Common Queries](#common-queries)
- [Performance Analysis](#performance-analysis)
- [Security Auditing](#security-auditing)

## Using grep

### Basic Searches

```bash
# Find all errors in today's logs
grep "ERROR" logs/aura-api-$(date +%Y-%m-%d).log

# Find logs for a specific user
grep "UserId.*user_123" logs/*.log

# Find logs for a specific project
grep "ProjectId.*proj_456" logs/*.log

# Case-insensitive search
grep -i "timeout" logs/*.log

# Count occurrences
grep -c "ERROR" logs/*.log
```

### With Context

```bash
# Show 5 lines before and after match
grep -B 5 -A 5 "OutOfMemoryException" logs/*.log

# Show 10 lines of context
grep -C 10 "Failed to generate video" logs/*.log

# Show filename with matches
grep -H "ERROR" logs/*.log
```

### Multiple Patterns

```bash
# Match multiple patterns (OR)
grep -E "ERROR|WARN" logs/*.log

# Match pattern and exclude another
grep "ERROR" logs/*.log | grep -v "Ignore"

# Match multiple files
grep "proj_123" logs/aura-api-*.log logs/errors-*.log
```

### Time-Based Searches

```bash
# Find logs from a specific hour
grep "2024-01-15 14:" logs/aura-api-*.log

# Find logs in a time range (14:00-15:00)
grep "2024-01-15 14:" logs/aura-api-*.log

# Find logs from the last hour
grep "$(date -u -d '1 hour ago' '+%Y-%m-%d %H'):" logs/aura-api-$(date +%Y-%m-%d).log

# Find logs between specific times
grep -E "2024-01-15 (14:3[0-9]|14:4[0-9]|14:50)" logs/aura-api-*.log
```

### Performance Queries

```bash
# Find slow requests (>5 seconds)
grep "Duration.*[5-9][0-9]{3,}" logs/aura-api-*.log

# Find slow database queries
grep "DatabaseQuery.*Duration" logs/performance-*.log | \
  awk '$NF > 1000'

# Find all performance warnings
grep "SLOW" logs/warnings-*.log

# Find specific slow operations
grep "VideoGeneration.*Duration" logs/performance-*.log
```

### Error Analysis

```bash
# Group errors by type
grep "ERROR" logs/errors-*.log | \
  grep -oP "ExceptionType.*?}," | \
  sort | uniq -c | sort -nr

# Find stack traces
grep -A 20 "ERROR" logs/errors-*.log | \
  grep "   at "

# Find errors with specific message
grep "ERROR.*DatabaseConnectionFailed" logs/errors-*.log
```

### Correlation Tracking

```bash
# Find all logs for a correlation ID
CORR_ID="abc123def456"
grep "$CORR_ID" logs/*.log | sort

# Find all logs for a trace ID
TRACE_ID="7f8e9d0c1b2a"
grep "TraceId.*$TRACE_ID" logs/*.log | sort

# Visualize request flow
CORR_ID="abc123def456"
grep "$CORR_ID" logs/*.log | \
  grep -E "(Request|Response|Duration)" | \
  sort
```

## Using Log Aggregation Tools

### Azure Log Analytics (KQL)

#### Basic Queries

```kusto
// All errors in the last hour
AuraLogs
| where Timestamp > ago(1h)
| where Level == "ERROR"
| project Timestamp, Message, Component, Context

// Logs for a specific correlation ID
AuraLogs
| where CorrelationId == "abc123"
| project Timestamp, Level, Message, TraceId, SpanId
| order by Timestamp asc

// Logs for a specific user
AuraLogs
| where Context contains "user_123"
| project Timestamp, Level, Message, Action
```

#### Performance Analysis

```kusto
// Top 10 slowest operations
AuraLogs
| where Level == "Performance"
| project OperationName, DurationMs, Context
| top 10 by DurationMs desc

// Average duration by operation
AuraLogs
| where Level == "Performance"
| summarize avg(DurationMs) by OperationName
| order by avg_DurationMs desc

// Performance over time
AuraLogs
| where Level == "Performance"
| summarize avg(DurationMs) by OperationName, bin(Timestamp, 1h)
| render timechart

// 95th percentile latency
AuraLogs
| where Level == "Performance"
| summarize percentile(DurationMs, 95) by OperationName
| order by percentile_DurationMs desc
```

#### Error Analysis

```kusto
// Error frequency
AuraLogs
| where Level == "ERROR"
| summarize count() by ExceptionType
| order by count_ desc

// Errors by component
AuraLogs
| where Level == "ERROR"
| summarize count() by Component, bin(Timestamp, 1h)
| render columnchart

// Error messages
AuraLogs
| where Level == "ERROR"
| project Timestamp, Message, Component, StackTrace
| order by Timestamp desc
| take 100
```

#### Trace Analysis

```kusto
// Complete trace view
let traceId = "abc123";
AuraLogs
| where TraceId == traceId
| project Timestamp, Level, SpanId, ParentSpanId, Message, DurationMs
| order by Timestamp asc

// Trace hierarchy visualization
let traceId = "abc123";
AuraLogs
| where TraceId == traceId
| project SpanId, ParentSpanId, OperationName, DurationMs
| render tree

// Trace timeline
let traceId = "abc123";
AuraLogs
| where TraceId == traceId
| project Timestamp, OperationName, DurationMs
| render timechart
```

### Splunk

#### Basic Queries

```spl
# All errors
index=aura Level=ERROR

# Specific correlation ID
index=aura CorrelationId="abc123"

# Time range
index=aura earliest=-1h latest=now

# User activity
index=aura UserId="user_123"
| table _time, Message, Action, Component
```

#### Performance Queries

```spl
# Top slow operations
index=aura Level=Performance
| stats avg(DurationMs) as AvgDuration by OperationName
| sort -AvgDuration
| head 10

# Performance trend
index=aura Level=Performance OperationName="VideoGeneration"
| timechart avg(DurationMs) as AvgDuration span=1h

# Percentiles
index=aura Level=Performance
| stats perc95(DurationMs) as P95 by OperationName
| sort -P95
```

#### Error Analysis

```spl
# Error count by type
index=aura Level=ERROR
| stats count by ExceptionType
| sort -count

# Error rate over time
index=aura
| timechart count(eval(Level="ERROR")) as Errors span=5m

# Top error messages
index=aura Level=ERROR
| top Message
```

### Elasticsearch (ELK Stack)

#### Basic Queries

```json
// All errors
{
  "query": {
    "term": { "Level": "ERROR" }
  }
}

// Correlation ID
{
  "query": {
    "term": { "CorrelationId": "abc123" }
  },
  "sort": [{ "Timestamp": "asc" }]
}

// Time range
{
  "query": {
    "range": {
      "Timestamp": {
        "gte": "now-1h",
        "lte": "now"
      }
    }
  }
}
```

#### Aggregations

```json
// Error count by type
{
  "size": 0,
  "aggs": {
    "error_types": {
      "terms": {
        "field": "ExceptionType.keyword",
        "size": 10
      }
    }
  }
}

// Average duration by operation
{
  "size": 0,
  "aggs": {
    "operations": {
      "terms": {
        "field": "OperationName.keyword"
      },
      "aggs": {
        "avg_duration": {
          "avg": { "field": "DurationMs" }
        }
      }
    }
  }
}

// Performance histogram
{
  "size": 0,
  "aggs": {
    "duration_histogram": {
      "histogram": {
        "field": "DurationMs",
        "interval": 100
      }
    }
  }
}
```

## Common Queries

### Find Recent Errors

```bash
# Last 100 errors
grep "ERROR" logs/errors-*.log | tail -100

# Errors in the last hour
find logs/ -name "errors-*.log" -mmin -60 -exec grep "ERROR" {} \;

# Unique error messages
grep "ERROR" logs/errors-*.log | \
  cut -d']' -f4- | \
  sort -u
```

### Track User Activity

```bash
# All actions by a user
USER_ID="user_123"
grep "UserId.*$USER_ID" logs/*.log | \
  grep -E "(Action|Operation)" | \
  sort

# User login history
grep "UserId.*$USER_ID" logs/audit-*.log | \
  grep "Login"

# User's recent errors
grep "UserId.*$USER_ID" logs/*.log | \
  grep "ERROR"
```

### Monitor System Health

```bash
# Count log levels
for level in DEBUG INFO WARN ERROR; do
  echo "$level: $(grep -c "$level" logs/aura-api-$(date +%Y-%m-%d).log)"
done

# Recent warnings and errors
grep -E "WARN|ERROR" logs/aura-api-$(date +%Y-%m-%d).log | tail -50

# Check for repeated errors
grep "ERROR" logs/errors-*.log | \
  cut -d']' -f4 | \
  sort | uniq -c | \
  sort -nr | \
  head -10
```

## Performance Analysis

### Identify Bottlenecks

```bash
# Top 10 slowest operations
grep "Performance" logs/performance-*.log | \
  grep -oP "OperationName\":\"[^\"]*\"|DurationMs\":\d+" | \
  paste -d' ' - - | \
  sort -t':' -k2 -nr | \
  head -10

# Operations taking more than 5 seconds
grep "Performance.*DurationMs" logs/performance-*.log | \
  awk -F'DurationMs":' '{print $2}' | \
  awk '{if ($1 > 5000) print $0}'
```

### Response Time Analysis

```bash
# Average response time by endpoint
grep "Request completed" logs/aura-api-*.log | \
  awk '{print $5, $NF}' | \
  awk '{sum[$1]+=$2; count[$1]++} END {for (i in sum) print i, sum[i]/count[i]}' | \
  sort -k2 -nr

# Slow requests by hour
grep "Duration.*ms" logs/aura-api-*.log | \
  grep -oP "\d{4}-\d{2}-\d{2} \d{2}:\d{2}" | \
  cut -d':' -f1 | \
  sort | uniq -c
```

## Security Auditing

### Authentication Events

```bash
# Failed login attempts
grep "LoginAttempt.*false" logs/audit-*.log

# Successful logins by IP
grep "LoginAttempt.*true" logs/audit-*.log | \
  grep -oP "IpAddress.*?," | \
  sort | uniq -c

# Multiple failed logins from same IP
grep "LoginAttempt.*false" logs/audit-*.log | \
  grep -oP "IpAddress\":\"[^\"]*" | \
  sort | uniq -c | \
  awk '$1 > 5'
```

### Access Patterns

```bash
# API key usage
grep "ApiKey" logs/audit-*.log | \
  grep -oP "ApiKey\":\"[^\"]*" | \
  sort | uniq -c

# Unauthorized access attempts
grep "Unauthorized" logs/audit-*.log

# Admin actions
grep "Admin" logs/audit-*.log | \
  grep "Action"
```

### Data Access

```bash
# Project access by user
grep "ProjectAccess" logs/audit-*.log | \
  grep "user_123"

# Sensitive operations
grep -E "Delete|Export|Admin" logs/audit-*.log | \
  tail -100

# Bulk operations
grep "Bulk" logs/audit-*.log
```

## Tips and Tricks

### Creating Log Aliases

```bash
# Add to .bashrc or .zshrc
alias log-errors='grep "ERROR" logs/errors-$(date +%Y-%m-%d).log'
alias log-slow='grep "SLOW" logs/warnings-$(date +%Y-%m-%d).log'
alias log-corr='f(){ grep "$1" logs/*.log | sort; }; f'
alias log-user='f(){ grep "UserId.*$1" logs/*.log; }; f'
```

### Log Monitoring Scripts

```bash
#!/bin/bash
# monitor-errors.sh - Alert on new errors

LAST_CHECK=$(cat /tmp/last_error_check 2>/dev/null || echo "1 hour ago")
ERRORS=$(grep "ERROR" logs/errors-$(date +%Y-%m-%d).log | \
         awk -v d="$LAST_CHECK" '$1 > d')

if [ ! -z "$ERRORS" ]; then
    echo "New errors detected:"
    echo "$ERRORS"
    # Send alert (email, Slack, etc.)
fi

date > /tmp/last_error_check
```

### Log Rotation

```bash
# Compress old logs
find logs/ -name "*.log" -mtime +7 -exec gzip {} \;

# Delete very old logs
find logs/ -name "*.log.gz" -mtime +90 -delete

# Check log sizes
du -sh logs/*.log | sort -h
```

## Further Reading

- [Logging Best Practices](./LOGGING_BEST_PRACTICES.md)
- [Debugging with Traces](./DEBUGGING_WITH_TRACES.md)
- [Local Logging Setup](./LOCAL_LOGGING_SETUP.md)
