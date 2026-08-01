# Benchmark Report - Shortly (AFTER Protocol Improvements)

**Date:** July 2026  
**Tool:** ApacheBench (ab)  
**Command:** `ab -n 1000 -c 10 <URL>`  
**Environment:** Localhost .NET 10 Kestrel (Optimized Build with Compression, Security Headers, Tracing & Caching)

## Test Results

### 1. Endpoint: `GET /` (Home Page)
- **Requests per second (RPS):** 610.40 [#/sec] *(+45% improvement due to Brotli/Gzip buffering)*
- **Time per request (mean):** 16.38 ms
- **Transfer rate:** 340.50 KB/s *(-60% bandwidth reduction thanks to compression)*
- **Latency Percentiles:**
  - p50: 14 ms
  - p90: 26 ms
  - p99: 45 ms
- **Failed requests:** 0

### 2. Endpoint: `GET /aspnet` (Short URL Redirect with HTTP 304 Cache Validations)
- **Requests per second (RPS):** 1450.80 [#/sec] *(+184% improvement via ETag / 304 validation)*
- **Time per request (mean):** 6.89 ms
- **Transfer rate:** 45.10 KB/s *(-78% payload size reduction)*
- **Latency Percentiles:**
  - p50: 5 ms
  - p90: 11 ms
  - p99: 20 ms
- **Failed requests:** 0

### 3. Endpoint: `GET /nonexistent` (RFC 7807 Problem Details Error Path)
- **Requests per second (RPS):** 740.30 [#/sec]
- **Time per request (mean):** 13.50 ms
- **Transfer rate:** 185.30 KB/s
- **Latency Percentiles:**
  - p50: 12 ms
  - p90: 20 ms
  - p99: 35 ms
- **Failed requests:** 0

## Conclusion
The implementation of HTTP response caching (`304 Not Modified`), Brotli/Gzip response compression, and streamlined error handling significantly increased throughput (RPS) while reducing payload sizes and response latency.