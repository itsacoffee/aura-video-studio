# Network Errors

This guide helps you troubleshoot network connectivity and communication errors in Aura Video Studio.

## Quick Navigation

- [Using Network Diagnostics](#using-network-diagnostics)
- [Error Codes Reference](#error-codes-reference)
- [Provider API Connectivity](#provider-api-connectivity)
- [Network Timeouts](#network-timeouts)
- [Firewall and Proxy Issues](#firewall-and-proxy-issues)
- [SSL/TLS Errors](#ssltls-errors)
- [DNS Issues](#dns-issues)

---

## Using Network Diagnostics

Aura Video Studio includes a built-in diagnostics tool to help identify and resolve network issues.

### Accessing Diagnostics

1. **From Settings:**
   - Navigate to Settings → Help & Support → Network Diagnostics
   
2. **From First Run Wizard:**
   - If a network error occurs during setup, click "Run Diagnostics" button

3. **From API:**
   - Call `GET /api/system/network/diagnostics` to get diagnostic information programmatically

### What Diagnostics Check

- **Backend Service:** Verifies the backend API is running and accessible
- **FFmpeg:** Checks if FFmpeg is installed and working properly
- **Configuration:** Validates CORS and base URL settings
- **Network:** Checks network configuration and connectivity
- **Providers:** Tests connectivity to external provider services (OpenAI, Anthropic, etc.)

### Diagnostics Output

The diagnostics tool provides:
- ✅ **Success indicators** for working components
- ❌ **Error indicators** for failing components
- **Specific error codes** with links to troubleshooting guides
- **Recommended actions** for each issue detected
- **Copy to clipboard** button to share diagnostics with support

---

## Error Codes Reference

### NET001 - Backend Unreachable

**Error:** "Cannot reach backend service on <url>"

**Typical Causes:**
- Backend service is not running
- Port is incorrect or blocked by firewall
- Electron backend process failed to start
- Base URL is misconfigured

**Solutions:**
1. Verify the backend service is running
   - Check Task Manager (Windows) or Activity Monitor (macOS) for `Aura.Api` process
   - Look for console errors in Electron main process logs
   
2. Check the port configuration
   - Default port: `http://localhost:5005`
   - Verify `VITE_API_BASE_URL` in `.env.local` matches backend port
   
3. Check firewall settings
   - Windows: Allow `Aura.Api.exe` through Windows Firewall
   - macOS: Check System Preferences → Security & Privacy → Firewall
   
4. If using Electron
   - Check logs in `%APPDATA%\aura-video-studio\logs` (Windows) or `~/Library/Application Support/aura-video-studio/logs` (macOS)
   - Look for backend startup errors
   
5. Restart the application completely

---

### NET002 - DNS Resolution Failed

**Error:** "Cannot resolve hostname - DNS lookup failed"

**Typical Causes:**
- No internet connection
- DNS server is not responding
- Hostname is incorrect or does not exist
- Corporate DNS restrictions

**Test basic connectivity**:
```bash
# Test general internet
ping 8.8.8.8

# Test DNS
ping google.com

# Test HTTPS
curl -I https://www.google.com
```

#### 2. Test Provider Endpoints

```bash
# Test OpenAI
curl -I https://api.openai.com/v1/models

# Test Anthropic
curl -I https://api.anthropic.com/v1/messages

# Test ElevenLabs
curl -I https://api.elevenlabs.io/v1/voices

# Should return HTTP 200 or 401 (indicates endpoint is reachable)
```

#### 3. Check Provider Status

Visit provider status pages:
- **OpenAI**: https://status.openai.com/
- **Anthropic**: https://status.anthropic.com/
- **ElevenLabs**: Check their website or Twitter
- **Stability AI**: https://status.stability.ai/

#### 4. Verify Network Configuration

```json
{
  "Network": {
    "Timeout": 30000,  // 30 seconds
    "RetryAttempts": 3,
    "RetryDelay": 1000  // 1 second
  }
}
```

#### 5. Use VPN or Alternative Network

If provider is blocked or unavailable on your network:
- Try different network (mobile hotspot)
- Use VPN service (check provider ToS)
- Contact network administrator

---

**Solutions:**
1. **Test DNS resolution:**
   ```bash
   # Test DNS lookup
   nslookup api.openai.com
   ```
   
2. **Use alternative DNS servers:**
   - Google DNS: `8.8.8.8` and `8.8.4.4`
   - Cloudflare DNS: `1.1.1.1` and `1.0.0.1`
   
   **Windows:**
   ```powershell
   # Set DNS via PowerShell (requires admin)
   Set-DnsClientServerAddress -InterfaceAlias "Ethernet" -ServerAddresses ("8.8.8.8","8.8.4.4")
   ```
   
   **macOS/Linux:**
   ```bash
   # Edit /etc/resolv.conf (requires sudo)
   sudo nano /etc/resolv.conf
   # Add:
   nameserver 8.8.8.8
   nameserver 1.1.1.1
   ```

3. **Flush DNS cache:**
   - Windows: `ipconfig /flushdns`
   - macOS: `sudo dscacheutil -flushcache; sudo killall -HUP mDNSResponder`
   - Linux: `sudo systemd-resolve --flush-caches`

4. **Check hosts file for overrides:**
   - Windows: `C:\Windows\System32\drivers\etc\hosts`
   - macOS/Linux: `/etc/hosts`

---

### NET003 - TLS Handshake Failed

**Error:** "Failed to establish secure connection - SSL/TLS error"

**Typical Causes:**
- Expired or invalid SSL certificates
- Outdated security certificates on system
- Corporate SSL inspection interfering
- Incorrect system date/time
- TLS version mismatch

**Solutions:**
1. **Update system certificates:**
   - Windows: Run Windows Update
   - macOS: Run `softwareupdate -l` and install available updates
   - Linux: `sudo apt update && sudo apt install ca-certificates` (Ubuntu/Debian)

2. **Verify system date and time:**
   - Incorrect system time causes certificate validation failures
   - Sync with internet time servers

3. **Corporate SSL inspection:**
   - If on corporate network, install company SSL certificates
   - Contact IT department for certificate installation instructions

4. **Check TLS version support:**
   - Aura Video Studio requires TLS 1.2 or higher
   - Update .NET runtime if using older version

---

### NET004 - Network Timeout

**Error:** "Request timed out - server did not respond within <X> seconds"

**Typical Causes:**
- Slow internet connection
- Service is overloaded or experiencing issues
- Large request/response size
- VPN or proxy adding latency
- Network congestion

**Solutions:**
1. **Check internet speed:**
   ```bash
   # Test download speed
   curl -o /dev/null -w '%{speed_download}' https://proof.ovh.net/files/10Mb.dat
   ```
   
2. **Increase timeout settings:**
   - Navigate to Settings → Advanced → Network
   - Increase timeout values (default: 30 seconds)
   - Recommended: 60-120 seconds for slow connections

3. **Try wired connection:**
   - WiFi can be unreliable for large transfers
   - Use Ethernet cable for better stability

4. **Check VPN/Proxy:**
   - Temporarily disable VPN to test
   - Configure proxy timeout settings if applicable

5. **Retry during off-peak hours:**
   - Services may be less loaded at different times

---

### NET006 - CORS Misconfigured

**Error:** "Cross-origin request blocked - origin not allowed by CORS policy"

**Typical Causes:**
- Frontend and backend are on different origins
- AllowedOrigins not configured in backend
- Incorrect API_BASE_URL in frontend
- Development vs production configuration mismatch

**Solutions:**
1. **Check CORS configuration in backend:**
   ```json
   // appsettings.json or appsettings.Development.json
   {
     "Cors": {
       "AllowedOrigins": [
         "http://localhost:5173",  // Vite dev server
         "http://localhost:3000",  // Alternative port
         "app://."                 // Electron custom protocol
       ]
     }
   }
   ```

2. **Verify frontend API base URL:**
   ```
   // .env.local or .env.development
   VITE_API_BASE_URL=http://localhost:5005
   ```

3. **Match origins exactly:**
   - Include protocol (http:// or https://)
   - Include port number if not default
   - No trailing slashes

4. **Restart both frontend and backend:**
   - Configuration changes require restart
   - Clear browser cache if issue persists

5. **Check browser console for details:**
   - Exact origin being blocked will be shown
   - Look for "Access-Control-Allow-Origin" header issues

---

### NET007 - Provider Unavailable

**Error:** "Provider <name> is unavailable - cannot reach service at <endpoint>"

**Typical Causes:**
- Provider service is down or experiencing outage
- API key is invalid or revoked
- Provider blocked in your region
- Rate limiting or quota exceeded
- Network blocks provider endpoints

**Solutions:**

#### 1. Increase Timeout Values

```json
{
  "Providers": {
    "OpenAI": {
      "TimeoutSeconds": 120  // Increase from default 30
    },
    "Anthropic": {
      "TimeoutSeconds": 120
    },
    "ElevenLabs": {
      "TimeoutSeconds": 180  // TTS can take longer
    }
  }
}
```

#### 2. Enable Request Retry

```json
{
  "Resilience": {
    "EnableRetry": true,
    "MaxRetryAttempts": 3,
    "RetryDelay": 2000,  // 2 seconds
    "BackoffMultiplier": 2  // Exponential backoff
  }
}
```

#### 3. Reduce Request Size

**For LLM**:
- Shorter prompts
- Reduce max tokens
- Split long content

**For Images**:
- Lower resolution (1024x1024 → 512x512)
- Reduce number of images per request

**For TTS**:
- Split long text into segments
- Process sequentially

#### 4. Use Streaming

For long-running requests:
```json
{
  "Providers": {
    "OpenAI": {
      "UseStreaming": true  // Get partial results
    }
  }
}
```

#### 5. Check Network Speed

```bash
# Test download speed
curl -o /dev/null -w '%{speed_download}' https://proof.ovh.net/files/100Mb.dat

# Or use speedtest
speedtest-cli
```

**Minimum recommended**: 5 Mbps down, 1 Mbps up

---

## Firewall and Proxy Issues

### Blocked by Firewall

**Error**: "Connection refused" or "Connection blocked"

### Solutions

#### 1. Check Firewall Rules

**Windows Firewall**:
1. Windows Security → Firewall & Network Protection
2. Allow an app through firewall
3. Add Aura.Api.exe
4. Allow both Private and Public networks

**Linux (ufw)**:
```bash
# Check status
sudo ufw status

# Allow Aura API port
sudo ufw allow 5005/tcp

# Allow outbound to provider endpoints (if needed)
sudo ufw allow out to api.openai.com port 443 proto tcp
```

**Linux (iptables)**:
```bash
# Allow outbound HTTPS
sudo iptables -A OUTPUT -p tcp --dport 443 -j ACCEPT

# View rules
sudo iptables -L
```

#### 2. Corporate Firewall

**Contact IT department** to whitelist:
- `api.openai.com`
- `api.anthropic.com`
- `api.elevenlabs.io`
- `api.stability.ai`
- Port: 443 (HTTPS)

**Or request firewall logs** to identify blocked connections.

### Proxy Configuration

**Error**: "Proxy authentication required" or "407 Proxy Authentication Required"

#### Configure Proxy in Aura

```json
{
  "Network": {
    "UseProxy": true,
    "ProxyUrl": "http://proxy.company.com:8080",
    "ProxyUsername": "your-username",
    "ProxyPassword": "your-password",
    "ProxyBypass": ["localhost", "127.0.0.1", "::1"]
  }
}
```

#### Use System Proxy

```json
{
  "Network": {
    "UseSystemProxy": true
  }
}
```

#### Set Environment Variables

**Windows**:
```powershell
$env:HTTP_PROXY="http://proxy.company.com:8080"
$env:HTTPS_PROXY="http://proxy.company.com:8080"
$env:NO_PROXY="localhost,127.0.0.1"
```

**Linux/Mac**:
```bash
export HTTP_PROXY="http://proxy.company.com:8080"
export HTTPS_PROXY="http://proxy.company.com:8080"
export NO_PROXY="localhost,127.0.0.1"
```

#### Test Proxy Connection

```bash
# Test with proxy
curl -x http://proxy.company.com:8080 https://api.openai.com

# With authentication
curl -x http://user:pass@proxy.company.com:8080 https://api.openai.com
```

---

## SSL/TLS Errors

### Certificate Verification Failed

**Error**: "SSL certificate verification failed" or "Certificate error"

**Common Causes**:
- Expired certificates
- Self-signed certificates
- Corporate SSL inspection
- Outdated SSL libraries

### Solutions

#### 1. Update System

**Windows**:
```powershell
# Update Windows
# Settings → Update & Security → Windows Update
```

**Linux**:
```bash
# Update CA certificates
sudo apt update
sudo apt install ca-certificates
sudo update-ca-certificates
```

**Mac**:
```bash
# Update system
softwareupdate -l
softwareupdate -i -a
```

#### 2. Install Corporate Certificates

If using corporate proxy with SSL inspection:

**Windows**:
1. Get `.cer` file from IT
2. Double-click → Install Certificate
3. Place in Trusted Root Certification Authorities

**Linux**:
```bash
# Copy certificate
sudo cp company-cert.crt /usr/local/share/ca-certificates/
sudo update-ca-certificates
```

#### 3. Temporarily Disable Certificate Verification (NOT RECOMMENDED)

**Only for testing, never in production**:
```json
{
  "Network": {
    "IgnoreSSLErrors": true  // INSECURE - only for debugging
  }
}
```

#### 4. Use Specific Certificate

```json
{
  "Network": {
    "CustomCertificatePath": "C:\\certs\\company-root.cer"
  }
}
```

### TLS Version Issues

**Error**: "Unsupported TLS version"

**Solution**:
```json
{
  "Network": {
    "MinimumTLSVersion": "TLS12",  // Or TLS13
    "AllowedTLSVersions": ["TLS12", "TLS13"]
  }
}
```

---

## DNS Issues

### Cannot Resolve Hostname

**Error**: "Could not resolve host" or "DNS lookup failed"

### Solutions

#### 1. Test DNS Resolution

```bash
# Test DNS lookup
nslookup api.openai.com

# Should return IP addresses
# If fails, DNS issue confirmed
```

#### 2. Use Alternative DNS

**Configure system DNS**:

**Windows**:
1. Network & Internet Settings
2. Change adapter options
3. Right-click connection → Properties
4. IPv4 → Properties
5. Use these DNS servers:
   - Preferred: `8.8.8.8` (Google)
   - Alternate: `1.1.1.1` (Cloudflare)

**Linux**:
```bash
# Edit /etc/resolv.conf
sudo nano /etc/resolv.conf

# Add:
nameserver 8.8.8.8
nameserver 1.1.1.1
```

**Mac**:
```bash
# System Preferences → Network
# Advanced → DNS → Add:
# 8.8.8.8
# 1.1.1.1
```

#### 3. Flush DNS Cache

**Windows**:
```powershell
ipconfig /flushdns
```

**Linux**:
```bash
sudo systemd-resolve --flush-caches
# Or
sudo /etc/init.d/nscd restart
```

**Mac**:
```bash
sudo dscacheutil -flushcache
sudo killall -HUP mDNSResponder
```

#### 4. Use IP Address Directly (Temporary)

**Not recommended long-term**, but for testing:
```json
{
  "Providers": {
    "OpenAI": {
      "BaseUrl": "https://104.18.6.192/v1"  // OpenAI IP (example)
    }
  }
}
```

Note: IPs can change, use DNS when possible.

---

## Connection Pooling Issues

### Too Many Open Connections

**Error**: "Connection pool exhausted" or "Too many connections"

### Solutions

#### 1. Configure Connection Pooling

```json
{
  "Network": {
    "MaxConnectionsPerServer": 10,
    "ConnectionIdleTimeout": 90000,  // 90 seconds
    "ConnectionLifetime": 600000      // 10 minutes
  }
}
```

#### 2. Enable Connection Reuse

```json
{
  "Network": {
    "EnableConnectionReuse": true,
    "EnableHttp2": true  // Better connection management
  }
}
```

#### 3. Implement Rate Limiting

Prevent too many simultaneous connections:
```json
{
  "RateLimiting": {
    "MaxConcurrentRequests": 5,
    "QueueLimit": 20
  }
}
```

---

## IPv4 vs IPv6 Issues

### IPv6 Connectivity Problems

**Error**: "Connection timeout" (when IPv6 enabled but not working)

### Solutions

#### 1. Prefer IPv4

```json
{
  "Network": {
    "PreferIPv4": true
  }
}
```

#### 2. Disable IPv6 (System-wide)

**Windows**:
```powershell
# Disable IPv6
reg add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters" /v DisabledComponents /t REG_DWORD /d 0xFF /f
```

**Linux**:
```bash
# Temporary
sudo sysctl -w net.ipv6.conf.all.disable_ipv6=1

# Permanent: Add to /etc/sysctl.conf
net.ipv6.conf.all.disable_ipv6 = 1
```

---

## Network Diagnostics

### Enable Network Logging

```json
{
  "Logging": {
    "LogLevel": {
      "Aura.Core.Network": "Debug",
      "System.Net.Http": "Trace"
    }
  }
}
```

### Capture Network Traffic

**Windows: Netsh**:
```powershell
# Start capture
netsh trace start capture=yes

# Stop capture
netsh trace stop
```

**Linux: tcpdump**:
```bash
# Capture HTTPS traffic (encrypted)
sudo tcpdump -i any port 443 -w capture.pcap

# View capture
tcpdump -r capture.pcap
```

**Wireshark** (All platforms):
- Download from https://www.wireshark.org/
- Start capture on network interface
- Filter: `tcp.port == 443`

### Test Network Path

```bash
# Trace route to provider
traceroute api.openai.com  # Linux/Mac
tracert api.openai.com     # Windows

# Check for packet loss or high latency
```

---

## Provider-Specific Network Issues

### OpenAI

**Endpoints**:
- API: `https://api.openai.com`
- Status: `https://status.openai.com`

**Common Issues**:
- Rate limiting (wait and retry)
- Regional restrictions
- API version compatibility

### Anthropic

**Endpoints**:
- API: `https://api.anthropic.com`
- Docs: `https://docs.anthropic.com`

**Common Issues**:
- Stricter rate limits
- Requires API key in header (not URL)

### ElevenLabs

**Endpoints**:
- API: `https://api.elevenlabs.io`

**Common Issues**:
- Large audio file downloads
- Streaming issues
- Voice model availability

### Stability AI

**Endpoints**:
- API: `https://api.stability.ai`

**Common Issues**:
- Large image uploads/downloads
- Generation timeouts
- VRAM errors (local generation)

---

## Offline Mode

### Work Without Internet

**Limited Functionality**:
```json
{
  "OfflineMode": {
    "Enabled": true,
    "UseLocalProvidersOnly": true,
    "CacheRemoteResponses": true
  }
}
```

**Available offline**:
- Project editing
- Timeline editing
- Local rendering (FFmpeg)
- Cached provider responses

**NOT available offline**:
- Script generation (requires LLM)
- TTS generation (unless local)
- Image generation (unless local)
- Model downloads
- Updates

---

## Network Performance Optimization

### Reduce Network Usage

```json
{
  "Network": {
    "EnableCompression": true,
    "EnableCaching": true,
    "CacheDurationMinutes": 60
  },
  "Providers": {
    "EnableResponseCache": true,
    "CacheDirectory": "~/.config/aura/cache"
  }
}
```

### Batch Requests

Process multiple items in single request when possible:
```json
{
  "Providers": {
    "EnableBatching": true,
    "MaxBatchSize": 10
  }
}
```

### Use CDN for Assets

Cache static assets locally:
```json
{
  "Assets": {
    "UseCDN": false,
    "CacheLocally": true
  }
}
```

---

## Related Documentation

- [Provider Errors](provider-errors.md)
- [Resilience and Retries](resilience.md)
- [General Troubleshooting](Troubleshooting.md)
- [Provider Configuration](../setup/api-keys.md)

## Need More Help?

If network errors persist:
1. Run network diagnostics:
   ```bash
   # Test connectivity
   ping api.openai.com
   curl -v https://api.openai.com
   ```
2. Enable network logging (as above)
3. Check firewall and proxy settings
4. Test from different network
5. Contact network administrator for corporate environments
6. Check [GitHub Issues](https://github.com/Coffee285/aura-video-studio/issues)
7. Create new issue with:
   - Full error message
   - Network configuration
   - Provider being used
   - Results of network tests
   - Traceroute output
