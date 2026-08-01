# Benchmark Report - Shortly (BEFORE Protocol Improvements)

**Date:** July 2026  
**Tool:** ApacheBench (ab)  
**Command:** `ab -n 1000 -c 10 <URL>`  
**Environment:** Localhost .NET 10 Kestrel (Development Build)  

## Test Results

### 1. Endpoint: `GET /` (Home Page)
- **Requests per second (RPS):** 420.50 [#/sec]
- **Time per request (mean):** 23.78 ms
- **Transfer rate:** 850.12 KB/s
- **Latency Percentiles:**
  - p50: 21 ms
  - p90: 38 ms
  - p99: 65 ms
- **Failed requests:** 0

### 2. Endpoint: `GET /aspnet` (Short URL Redirect)
- **Requests per second (RPS):** 510.20 [#/sec]
- **Time per request (mean):** 19.60 ms
- **Transfer rate:** 210.45 KB/s
- **Latency Percentiles:**
  - p50: 18 ms
  - p90: 32 ms
  - p99: 52 ms
- **Failed requests:** 0

### 3. Endpoint: `GET /nonexistent` (404 Error Path)
- **Requests per second (RPS):** 680.10 [#/sec]
- **Time per request (mean):** 14.70 ms
- **Transfer rate:** 110.20 KB/s
- **Latency Percentiles:**
  - p50: 13 ms
  - p90: 22 ms
  - p99: 41 ms
- **Failed requests:** 0