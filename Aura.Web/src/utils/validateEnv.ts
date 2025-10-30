/**
 * Environment variable validation utility
 * Validates required environment variables before app initialization
 */

interface ValidationResult {
  isValid: boolean;
  errors: string[];
}

/**
 * Validates that all required environment variables are properly configured
 * @throws Error if validation fails with detailed message
 */
export function validateEnvironment(): void {
  const result = checkEnvironment();

  if (!result.isValid) {
    const errorMessage = createErrorMessage(result.errors);
    throw new Error(errorMessage);
  }
}

/**
 * Checks environment variables and returns validation result
 */
function checkEnvironment(): ValidationResult {
  const errors: string[] = [];

  // Check VITE_API_BASE_URL is defined
  const apiBaseUrl = import.meta.env.VITE_API_BASE_URL;

  if (!apiBaseUrl) {
    errors.push('VITE_API_BASE_URL is not defined');
  } else if (typeof apiBaseUrl !== 'string' || apiBaseUrl.trim() === '') {
    errors.push('VITE_API_BASE_URL is empty');
  } else {
    // Validate it's a valid URL or relative path
    // Allow relative paths like "/api" for same-origin deployments
    if (apiBaseUrl.startsWith('/')) {
      // Relative path - this is valid for same-origin deployments
      // No further validation needed
    } else {
      // Must be a full URL
      try {
        const url = new URL(apiBaseUrl);
        // Ensure it's HTTP or HTTPS
        if (!['http:', 'https:'].includes(url.protocol)) {
          errors.push(
            `VITE_API_BASE_URL has invalid protocol "${url.protocol}" (must be http: or https:)`
          );
        }
      } catch (e) {
        errors.push(`VITE_API_BASE_URL is not a valid URL or path: "${apiBaseUrl}"`);
      }
    }
  }

  return {
    isValid: errors.length === 0,
    errors,
  };
}

/**
 * Creates a user-friendly error message with fix instructions
 */
function createErrorMessage(errors: string[]): string {
  const errorList = errors.map((e) => `  • ${e}`).join('\n');

  return `
Environment Configuration Error
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

The application cannot start due to missing or invalid environment variables:

${errorList}

How to fix:
━━━━━━━━━━━━

1. Create a file named ".env.local" in the Aura.Web directory
2. Add the following line to the file:

   VITE_API_BASE_URL=http://127.0.0.1:5005

   For production deployments, use your production API URL:
   VITE_API_BASE_URL=https://your-api-domain.com

3. Save the file and restart the development server

Example .env.local file contents:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
VITE_API_BASE_URL=http://127.0.0.1:5005
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Note: The .env.local file should NOT be committed to version control.
It's already listed in .gitignore.
`.trim();
}
