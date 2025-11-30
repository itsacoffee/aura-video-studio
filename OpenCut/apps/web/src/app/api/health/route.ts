import { NextRequest, NextResponse } from "next/server";

export async function GET(request: NextRequest) {
  // Return health status with CORS headers for embedding
  return new NextResponse("OK", {
    status: 200,
    headers: {
      "Content-Type": "text/plain",
      // Allow embedding in Aura Video Studio
      "Access-Control-Allow-Origin": "*",
      "Access-Control-Allow-Methods": "GET, OPTIONS",
      "Access-Control-Allow-Headers": "Content-Type",
      // Allow iframe embedding
      "X-Frame-Options": "ALLOWALL",
    },
  });
}

export async function OPTIONS(request: NextRequest) {
  // Handle CORS preflight requests
  return new NextResponse(null, {
    status: 204,
    headers: {
      "Access-Control-Allow-Origin": "*",
      "Access-Control-Allow-Methods": "GET, OPTIONS",
      "Access-Control-Allow-Headers": "Content-Type",
    },
  });
}
