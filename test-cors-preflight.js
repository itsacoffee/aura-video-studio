/**
 * CORS Preflight Request Test
 * Tests OPTIONS preflight requests that browsers send before actual requests
 */

const http = require("http");

const BACKEND_HOST = "127.0.0.1";
const BACKEND_PORT = 5005;

const colors = {
  reset: "\x1b[0m",
  red: "\x1b[31m",
  green: "\x1b[32m",
  yellow: "\x1b[33m",
  cyan: "\x1b[36m",
};

function testPreflight(path, origin) {
  return new Promise((resolve, reject) => {
    console.log(`\n${colors.cyan}Testing preflight: ${path}${colors.reset}`);
    console.log(`Origin: ${origin}`);

    const options = {
      hostname: BACKEND_HOST,
      port: BACKEND_PORT,
      path: path,
      method: "OPTIONS",
      headers: {
        Origin: origin,
        "Access-Control-Request-Method": "GET",
        "Access-Control-Request-Headers": "content-type",
      },
    };

    const req = http.request(options, (res) => {
      console.log(`Status: ${res.statusCode}`);
      console.log("\nResponse Headers:");

      const corsHeaders = [
        "access-control-allow-origin",
        "access-control-allow-methods",
        "access-control-allow-headers",
        "access-control-allow-credentials",
        "access-control-max-age",
      ];

      let hasCors = false;
      corsHeaders.forEach((header) => {
        const value = res.headers[header];
        if (value) {
          console.log(
            `  ${colors.green}✓${colors.reset} ${header}: ${colors.cyan}${value}${colors.reset}`
          );
          hasCors = true;
        } else {
          console.log(
            `  ${colors.red}✗${colors.reset} ${header}: ${colors.yellow}(not set)${colors.reset}`
          );
        }
      });

      if (hasCors) {
        console.log(
          `\n${colors.green}✓ CORS headers present${colors.reset}`
        );
      } else {
        console.log(
          `\n${colors.red}✗ NO CORS headers found${colors.reset}`
        );
        console.log(
          `${colors.yellow}⚠ This will block requests from Electron!${colors.reset}`
        );
      }

      resolve({ hasCors, statusCode: res.statusCode, headers: res.headers });
    });

    req.on("error", reject);
    req.end();
  });
}

function testActualRequest(path, origin) {
  return new Promise((resolve, reject) => {
    console.log(`\n${colors.cyan}Testing actual GET: ${path}${colors.reset}`);
    console.log(`Origin: ${origin}`);

    const options = {
      hostname: BACKEND_HOST,
      port: BACKEND_PORT,
      path: path,
      method: "GET",
      headers: {
        Origin: origin,
      },
    };

    const req = http.request(options, (res) => {
      let data = "";
      res.on("data", (chunk) => (data += chunk));
      res.on("end", () => {
        console.log(`Status: ${res.statusCode}`);

        const corsHeader = res.headers["access-control-allow-origin"];
        const credentialsHeader =
          res.headers["access-control-allow-credentials"];

        if (corsHeader) {
          console.log(
            `  ${colors.green}✓${colors.reset} access-control-allow-origin: ${colors.cyan}${corsHeader}${colors.reset}`
          );
        } else {
          console.log(
            `  ${colors.red}✗${colors.reset} access-control-allow-origin: ${colors.yellow}(not set)${colors.reset}`
          );
        }

        if (credentialsHeader) {
          console.log(
            `  ${colors.green}✓${colors.reset} access-control-allow-credentials: ${colors.cyan}${credentialsHeader}${colors.reset}`
          );
        }

        try {
          const parsed = JSON.parse(data);
          console.log(`\nResponse: ${JSON.stringify(parsed).substring(0, 100)}...`);
        } catch (e) {
          console.log(`\nResponse: ${data.substring(0, 100)}...`);
        }

        resolve({
          hasCors: !!corsHeader,
          statusCode: res.statusCode,
          headers: res.headers,
        });
      });
    });

    req.on("error", reject);
    req.end();
  });
}

async function runTests() {
  console.log(`${colors.cyan}=${"=".repeat(70)}${colors.reset}`);
  console.log(`${colors.cyan}CORS PREFLIGHT & ACTUAL REQUEST TESTS${colors.reset}`);
  console.log(`${colors.cyan}=${"=".repeat(70)}${colors.reset}`);

  const testScenarios = [
    { origin: "file://", description: "Electron (file:// protocol)" },
    { origin: "null", description: "Electron (null origin)" },
    { origin: "http://localhost:5173", description: "Vite dev server" },
    { origin: "http://127.0.0.1:5173", description: "Vite dev server (127.0.0.1)" },
  ];

  for (const scenario of testScenarios) {
    console.log(
      `\n${colors.cyan}${"=".repeat(70)}${colors.reset}`
    );
    console.log(
      `${colors.cyan}Scenario: ${scenario.description}${colors.reset}`
    );
    console.log(
      `${colors.cyan}${"=".repeat(70)}${colors.reset}`
    );

    try {
      // Test preflight (OPTIONS)
      const preflightResult = await testPreflight(
        "/health/live",
        scenario.origin
      );

      // Test actual request (GET)
      const actualResult = await testActualRequest(
        "/health/live",
        scenario.origin
      );

      // Summary for this scenario
      console.log(`\n${colors.cyan}Summary for ${scenario.description}:${colors.reset}`);
      if (preflightResult.hasCors && actualResult.hasCors) {
        console.log(
          `${colors.green}✓ CORS working - both preflight and actual requests have headers${colors.reset}`
        );
      } else if (!preflightResult.hasCors && !actualResult.hasCors) {
        console.log(
          `${colors.red}✗ CORS NOT working - no headers on either preflight or actual requests${colors.reset}`
        );
        console.log(
          `${colors.yellow}  This will cause "CORS policy" errors in browsers/Electron${colors.reset}`
        );
      } else {
        console.log(
          `${colors.yellow}⚠ CORS partially working - inconsistent headers${colors.reset}`
        );
      }
    } catch (error) {
      console.log(
        `${colors.red}✗ Test failed: ${error.message}${colors.reset}`
      );
    }
  }

  console.log(`\n${colors.cyan}=${"=".repeat(70)}${colors.reset}`);
  console.log(`${colors.cyan}RECOMMENDATIONS${colors.reset}`);
  console.log(`${colors.cyan}=${"=".repeat(70)}${colors.reset}`);
  console.log(`
If CORS headers are missing:

1. Verify CORS middleware is registered in Program.cs:
   ${colors.green}app.UseCors(AuraCorsPolicy);${colors.reset}

2. Ensure it's placed BEFORE other middleware that handles requests:
   ${colors.cyan}app.UseRouting();
   app.UseCors(AuraCorsPolicy);  // <- MUST be here
   app.UseAuthentication();
   app.UseAuthorization();${colors.reset}

3. Check the CORS policy configuration allows file:// protocol:
   ${colors.green}policy.SetIsOriginAllowed(origin =>
   {
       if (string.IsNullOrWhiteSpace(origin) || origin == "null")
           return true;  // Allow Electron
       ...
   });${colors.reset}

4. For Electron, the frontend makes requests with:
   - Origin: ${colors.yellow}null${colors.reset} (most common)
   - Origin: ${colors.yellow}file://${colors.reset} (some cases)
   - Origin: ${colors.yellow}(not set)${colors.reset} (rare)

5. Restart the backend after making CORS changes.
  `);
}

runTests().catch((error) => {
  console.error(`${colors.red}Test suite failed:${colors.reset}`, error);
  process.exit(1);
});

