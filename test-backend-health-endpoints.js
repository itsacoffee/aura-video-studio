/**
 * Backend Health Endpoints Verification
 * Tests all health endpoints to verify they exist and return expected formats
 */

const http = require("http");

const BACKEND_HOST = "127.0.0.1";
const BACKEND_PORT = 5005;
const BASE_URL = `http://${BACKEND_HOST}:${BACKEND_PORT}`;

const colors = {
  reset: "\x1b[0m",
  red: "\x1b[31m",
  green: "\x1b[32m",
  yellow: "\x1b[33m",
  blue: "\x1b[34m",
  cyan: "\x1b[36m",
};

function log(color, symbol, message, details = "") {
  console.log(
    `${color}${symbol}${colors.reset} ${message}${
      details ? "\n  " + details : ""
    }`
  );
}

function makeRequest(path, options = {}) {
  return new Promise((resolve, reject) => {
    const req = http.request(
      {
        hostname: BACKEND_HOST,
        port: BACKEND_PORT,
        path: path,
        method: options.method || "GET",
        headers: options.headers || {},
        timeout: options.timeout || 5000,
      },
      (res) => {
        let data = "";
        res.on("data", (chunk) => (data += chunk));
        res.on("end", () => {
          try {
            const parsed = data ? JSON.parse(data) : null;
            resolve({
              statusCode: res.statusCode,
              headers: res.headers,
              data: parsed,
              rawData: data,
            });
          } catch (e) {
            resolve({
              statusCode: res.statusCode,
              headers: res.headers,
              data: null,
              rawData: data,
              parseError: e.message,
            });
          }
        });
      }
    );

    req.on("error", reject);
    req.on("timeout", () => {
      req.destroy();
      reject(new Error("Request timeout"));
    });

    if (options.body) {
      req.write(JSON.stringify(options.body));
    }
    req.end();
  });
}

async function testHealthEndpoint(path, expectedFields = []) {
  console.log(`\n${colors.cyan}Testing: ${path}${colors.reset}`);

  try {
    const result = await makeRequest(path);

    if (result.statusCode === 200) {
      log(colors.green, "✓", `Status: ${result.statusCode} OK`);

      if (result.data) {
        log(
          colors.blue,
          "ℹ",
          "Response data:",
          JSON.stringify(result.data, null, 2)
        );

        // Check expected fields
        const missingFields = expectedFields.filter(
          (field) => !(field in result.data)
        );
        if (missingFields.length > 0) {
          log(
            colors.yellow,
            "⚠",
            `Missing expected fields: ${missingFields.join(", ")}`
          );
        } else if (expectedFields.length > 0) {
          log(colors.green, "✓", "All expected fields present");
        }
      } else {
        log(colors.yellow, "⚠", "Response has no JSON body");
        log(colors.blue, "ℹ", "Raw response:", result.rawData);
      }

      return { success: true, ...result };
    } else {
      log(colors.red, "✗", `Status: ${result.statusCode}`);
      log(
        colors.red,
        "",
        "Response:",
        JSON.stringify(result.data || result.rawData, null, 2)
      );
      return { success: false, ...result };
    }
  } catch (error) {
    log(colors.red, "✗", `Error: ${error.message}`);
    if (error.code) {
      log(colors.red, "", `Code: ${error.code}`);
    }
    return { success: false, error: error.message };
  }
}

async function testCorsHeaders(path) {
  console.log(`\n${colors.cyan}Testing CORS for: ${path}${colors.reset}`);

  try {
    const result = await makeRequest(path, {
      headers: {
        Origin: "file://",
        "Access-Control-Request-Method": "GET",
      },
    });

    const corsHeaders = {
      "access-control-allow-origin":
        result.headers["access-control-allow-origin"],
      "access-control-allow-methods":
        result.headers["access-control-allow-methods"],
      "access-control-allow-headers":
        result.headers["access-control-allow-headers"],
    };

    const hasCors = Object.values(corsHeaders).some((v) => v !== undefined);

    if (hasCors) {
      log(colors.green, "✓", "CORS headers present");
      Object.entries(corsHeaders).forEach(([key, value]) => {
        if (value) {
          log(colors.blue, "  ", `${key}: ${value}`);
        }
      });
    } else {
      log(colors.yellow, "⚠", "No CORS headers found");
      log(
        colors.yellow,
        "",
        "This may cause issues in Electron with file:// protocol"
      );
    }

    return { hasCors, corsHeaders };
  } catch (error) {
    log(colors.red, "✗", `Error: ${error.message}`);
    return { hasCors: false, error: error.message };
  }
}

async function runTests() {
  console.log(`${colors.cyan}${"=".repeat(70)}${colors.reset}`);
  console.log(
    `${colors.cyan}BACKEND HEALTH ENDPOINTS VERIFICATION${colors.reset}`
  );
  console.log(`${colors.cyan}${"=".repeat(70)}${colors.reset}`);
  console.log(`Target: ${BASE_URL}\n`);

  const results = {
    endpoints: [],
    cors: [],
  };

  // Test all known health endpoints
  const endpoints = [
    { path: "/health/live", fields: ["status"] },
    { path: "/health/ready", fields: ["status", "checks"] },
    { path: "/health", fields: [] },
    { path: "/healthz", fields: ["status"] },
    { path: "/healthz/simple", fields: ["status"] },
    { path: "/api/health/live", fields: [] },
    { path: "/api/health/ready", fields: [] },
    { path: "/api/healthz", fields: [] },
  ];

  for (const endpoint of endpoints) {
    const result = await testHealthEndpoint(endpoint.path, endpoint.fields);
    results.endpoints.push({ ...endpoint, ...result });
  }

  // Test CORS on working endpoints
  console.log(`\n${colors.cyan}${"=".repeat(70)}${colors.reset}`);
  console.log(`${colors.cyan}CORS CONFIGURATION TESTS${colors.reset}`);
  console.log(`${colors.cyan}${"=".repeat(70)}${colors.reset}`);

  const workingEndpoints = results.endpoints.filter((e) => e.success);
  for (const endpoint of workingEndpoints) {
    const corsResult = await testCorsHeaders(endpoint.path);
    results.cors.push({ path: endpoint.path, ...corsResult });
  }

  // Test response time
  console.log(`\n${colors.cyan}${"=".repeat(70)}${colors.reset}`);
  console.log(`${colors.cyan}RESPONSE TIME TESTS${colors.reset}`);
  console.log(`${colors.cyan}${"=".repeat(70)}${colors.reset}`);

  if (workingEndpoints.length > 0) {
    const endpoint = workingEndpoints[0].path;
    const times = [];

    for (let i = 0; i < 5; i++) {
      const start = Date.now();
      try {
        await makeRequest(endpoint, { timeout: 2000 });
        const duration = Date.now() - start;
        times.push(duration);
        log(colors.blue, `${i + 1}.`, `${duration}ms`);
      } catch (error) {
        log(colors.red, `${i + 1}.`, `Failed: ${error.message}`);
      }
    }

    if (times.length > 0) {
      const avg = times.reduce((a, b) => a + b, 0) / times.length;
      const min = Math.min(...times);
      const max = Math.max(...times);

      console.log(`\n  Average: ${avg.toFixed(2)}ms`);
      console.log(`  Min: ${min}ms, Max: ${max}ms`);

      if (avg > 1000) {
        log(
          colors.yellow,
          "⚠",
          "Average response time > 1s - backend may be slow"
        );
      } else if (avg > 500) {
        log(
          colors.yellow,
          "ℹ",
          "Average response time > 500ms - acceptable but could be faster"
        );
      } else {
        log(colors.green, "✓", "Response times are good");
      }
    }
  }

  // Summary
  console.log(`\n${colors.cyan}${"=".repeat(70)}${colors.reset}`);
  console.log(`${colors.cyan}SUMMARY${colors.reset}`);
  console.log(`${colors.cyan}${"=".repeat(70)}${colors.reset}`);

  const working = results.endpoints.filter((e) => e.success);
  const failed = results.endpoints.filter((e) => !e.success);

  log(
    colors.green,
    "✓",
    `Working endpoints: ${working.length}/${results.endpoints.length}`
  );
  working.forEach((e) => {
    console.log(`  ${colors.green}•${colors.reset} ${e.path}`);
  });

  if (failed.length > 0) {
    log(
      colors.red,
      "✗",
      `Failed endpoints: ${failed.length}/${results.endpoints.length}`
    );
    failed.forEach((e) => {
      console.log(
        `  ${colors.red}•${colors.reset} ${e.path} - ${
          e.error || "Status " + e.statusCode
        }`
      );
    });
  }

  const withCors = results.cors.filter((c) => c.hasCors);
  const withoutCors = results.cors.filter((c) => !c.hasCors);

  if (withCors.length > 0) {
    log(
      colors.green,
      "✓",
      `Endpoints with CORS: ${withCors.length}/${results.cors.length}`
    );
  }
  if (withoutCors.length > 0) {
    log(
      colors.yellow,
      "⚠",
      `Endpoints without CORS: ${withoutCors.length}/${results.cors.length}`
    );
    log(colors.yellow, "", "CORS is required for Electron (file:// protocol)");
  }

  // Recommendations
  console.log(`\n${colors.cyan}RECOMMENDATIONS:${colors.reset}`);

  if (working.length === 0) {
    log(
      colors.red,
      "✗",
      "NO WORKING ENDPOINTS - Backend may not be running or misconfigured"
    );
    console.log("  1. Check if backend process is running");
    console.log("  2. Verify ASPNETCORE_URLS is set correctly");
    console.log("  3. Check for errors in backend logs");
  } else {
    const primaryEndpoint = working.find((e) => e.path === "/health/live");
    if (primaryEndpoint) {
      log(colors.green, "✓", "Primary health endpoint /health/live is working");
      console.log(`  Frontend should use: ${BASE_URL}/health/live`);
    } else {
      log(colors.yellow, "⚠", "/health/live endpoint not found");
      console.log(`  Recommended: Use ${working[0].path} instead`);
      console.log(
        `  Frontend should be configured to: ${BASE_URL}${working[0].path}`
      );
    }
  }

  if (withoutCors.length > 0) {
    log(colors.yellow, "⚠", "CORS configuration needed for Electron");
    console.log("  Add to Program.cs:");
    console.log("  builder.Services.AddCors(options => {");
    console.log('    options.AddPolicy("ElectronPolicy", builder => {');
    console.log("      builder.SetIsOriginAllowed(_ => true)");
    console.log("             .AllowAnyMethod()");
    console.log("             .AllowAnyHeader();");
    console.log("    });");
    console.log("  });");
  }

  console.log(`\n${colors.cyan}${"=".repeat(70)}${colors.reset}`);
}

// Run tests
runTests().catch((error) => {
  console.error(`${colors.red}Test suite failed:${colors.reset}`, error);
  process.exit(1);
});
