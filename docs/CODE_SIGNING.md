# Code Signing Guide for Aura Video Studio

This guide explains code signing for Windows installers - why it's important, how to get a certificate, and how to configure signing.

## Table of Contents

1. [Why Code Signing?](#why-code-signing)
2. [Understanding Certificates](#understanding-certificates)
3. [Obtaining a Certificate](#obtaining-a-certificate)
4. [Configuring Code Signing](#configuring-code-signing)
5. [Signing Locally](#signing-locally)
6. [GitHub Actions Configuration](#github-actions-configuration)
7. [Verification](#verification)
8. [Certificate Management](#certificate-management)
9. [Troubleshooting](#troubleshooting)

## Why Code Signing?

### The Problem: SmartScreen Warnings

Without code signing, Windows shows scary warnings to users:

```
Windows protected your PC
Microsoft Defender SmartScreen prevented an unrecognized app from starting.
Running this app might put your PC at risk.
```

**Impact**:
- Users won't trust your installer
- Many users will give up and not install
- Looks unprofessional
- Antivirus software may flag it

### The Solution: Code Signing Certificate

A code signing certificate:
- ✅ Proves the software publisher's identity
- ✅ Ensures code hasn't been tampered with
- ✅ Eliminates SmartScreen warnings (eventually)
- ✅ Increases user trust and download rates
- ✅ Reduces antivirus false positives

### Reality Check

**Initial Period** (first few weeks):
- SmartScreen warnings will still appear
- Even with valid certificate
- Certificate needs to build "reputation"
- Based on number of downloads and user feedback

**After Building Reputation**:
- No more SmartScreen warnings
- Smooth installation experience
- Professional appearance

**Timeline**:
- Standard Certificate: 2-6 months to build reputation
- EV Certificate: Immediate trust (no reputation needed)

## Understanding Certificates

### Types of Code Signing Certificates

#### 1. Standard Code Signing (OV)

**Organization Validation (OV)**

- **Cost**: $200-$400 per year
- **Validation**: Email + business registration verification
- **Timeline**: 1-3 business days
- **Delivery**: Digital file (PFX)
- **Storage**: On your computer
- **SmartScreen**: Must build reputation over time

**Best for**:
- Independent developers
- Startups
- Small businesses
- Budget-conscious projects

**Providers**:
- Sectigo/Comodo: ~$200/year
- DigiCert: ~$400/year
- SSL.com: ~$250/year
- GlobalSign: ~$300/year

#### 2. Extended Validation (EV)

**Extended Validation (EV)**

- **Cost**: $400-$800 per year
- **Validation**: Extensive business verification
- **Timeline**: 3-7 business days
- **Delivery**: USB hardware token
- **Storage**: On hardware token only
- **SmartScreen**: Immediate trust (no reputation period!)

**Best for**:
- Established businesses
- High-volume downloads
- Professional image critical
- Want immediate trust

**Providers**:
- DigiCert: ~$600/year
- SSL.com: ~$450/year
- Sectigo: ~$500/year
- Entrust: ~$700/year

### Comparison

| Feature | Standard (OV) | Extended Validation (EV) |
|---------|---------------|---------------------------|
| Cost | $200-$400/year | $400-$800/year |
| Validation | Email + docs | Extensive verification |
| Timeline | 1-3 days | 3-7 days |
| Storage | File (PFX) | Hardware token (USB) |
| SmartScreen | Builds reputation | Instant trust |
| Portability | Easy (copy file) | Hardware token required |
| CI/CD | Easy to automate | Requires HSM or special setup |
| Best for | Indie devs, startups | Enterprises, high-volume |

**Recommendation**:
- **Starting out?** Standard (OV) - Build reputation over time
- **Serious business?** EV - Worth the extra cost for instant trust
- **Tight budget?** Standard (OV) or wait until you have users

## Obtaining a Certificate

### Step 1: Choose a Provider

**Popular Providers**:

1. **Sectigo (formerly Comodo)**
   - Website: https://sectigo.com/
   - Cost: ~$200-$250/year (OV), ~$500/year (EV)
   - Pros: Affordable, good reputation
   - Cons: Average support

2. **DigiCert**
   - Website: https://www.digicert.com/
   - Cost: ~$400/year (OV), ~$600/year (EV)
   - Pros: Premium provider, excellent support, fast
   - Cons: More expensive

3. **SSL.com**
   - Website: https://www.ssl.com/
   - Cost: ~$250/year (OV), ~$450/year (EV)
   - Pros: Good balance of price and service
   - Cons: Validation can be slow

4. **GlobalSign**
   - Website: https://www.globalsign.com/
   - Cost: ~$300/year (OV)
   - Pros: Well-known, trusted
   - Cons: Limited EV options

### Step 2: Prepare Documents

**For Standard (OV) Certificate**:

*Individual developers:*
- Valid government ID (driver's license, passport)
- Proof of address (utility bill, bank statement)
- Email at domain you're signing (or use personal email)

*Businesses:*
- Business registration documents
- Tax ID / EIN
- Business address proof
- Officer/owner ID
- Business email address

**For EV Certificate**:

*All of the above, plus:*
- D&B DUNS number (or equivalent)
- Business phone listed in directory
- Verified business bank account
- Incorporation documents
- May require notarization

**Tips**:
- Have documents ready before ordering
- Use official business email (not Gmail/Yahoo)
- Ensure business details match exactly across all documents
- Response time affects how quickly you get certificate

### Step 3: Order Certificate

1. Go to provider website
2. Select "Code Signing Certificate"
3. Choose Standard (OV) or Extended Validation (EV)
4. Select duration (1 year, 2 years, 3 years)
   - Multi-year often cheaper
   - Saves renewal hassle
5. Complete checkout
6. Wait for validation email

### Step 4: Validation Process

**Standard (OV)**:
1. Receive validation email
2. Click verification link
3. Submit documents
4. Answer any follow-up questions
5. Receive certificate (1-3 days)

**EV**:
1. Receive validation email
2. Submit extensive documentation
3. Verification phone calls
4. Wait for hardware token to ship
5. Receive token (3-7 days + shipping)

### Step 5: Download/Receive Certificate

**Standard (OV)**:
- Download PFX file from provider portal
- Save securely with password
- Backup in secure location

**EV**:
- Receive hardware token by mail
- Install token drivers
- Configure token software
- Test signing on development machine

## Configuring Code Signing

### Standard Certificate (PFX File)

#### 1. Save Certificate

```powershell
# Create secure directory
New-Item -ItemType Directory -Path "C:\Certificates" -Force

# Copy certificate
Copy-Item "Downloads\mycert.pfx" -Destination "C:\Certificates\aura-cert.pfx"

# Set secure permissions (optional)
icacls "C:\Certificates\aura-cert.pfx" /inheritance:r /grant:r "$env:USERNAME:(R)"
```

#### 2. Configure electron-builder

The certificate is already configured in `Aura.Desktop/package.json`:

```json
{
  "build": {
    "win": {
      "certificateFile": "win-certificate.pfx",
      "certificatePassword": "",
      "signingHashAlgorithms": ["sha256"],
      "rfc3161TimeStampServer": "http://timestamp.digicert.com"
    }
  }
}
```

#### 3. Set Environment Variables

```powershell
# For local builds
$env:WIN_CSC_LINK="C:\Certificates\aura-cert.pfx"
$env:WIN_CSC_KEY_PASSWORD="your_certificate_password"

# Now build
cd Aura.Desktop
npm run build:win
```

**Or** copy certificate to project:

```powershell
# Copy certificate to project (NOT recommended for public repos)
Copy-Item "C:\Certificates\aura-cert.pfx" -Destination "Aura.Desktop\win-certificate.pfx"

# Add password to build command
$env:WIN_CSC_KEY_PASSWORD="password"
npm run build:win
```

### EV Certificate (Hardware Token)

#### 1. Install Token Drivers

Follow manufacturer instructions (usually SafeNet or Thales)

#### 2. Configure Token Software

- Install eToken software
- Test token access
- Note token password/PIN

#### 3. Update Build Configuration

For EV certificates on hardware tokens, electron-builder needs special configuration:

```json
{
  "build": {
    "win": {
      "sign": "./scripts/sign-windows.js",
      "signingHashAlgorithms": ["sha256"]
    }
  }
}
```

The custom signing script (`scripts/sign-windows.js`) handles hardware token signing.

#### 4. Build

```powershell
# Token must be plugged in
# Enter PIN when prompted
npm run build:win
```

## Signing Locally

### Quick Test

```powershell
# Build installer with signing
cd Aura.Desktop
$env:WIN_CSC_LINK="path\to\cert.pfx"
$env:WIN_CSC_KEY_PASSWORD="your_password"
npm run build:win
```

### Verify Signing

```powershell
# Check if installer is signed
cd dist
Get-AuthenticodeSignature "Aura-Video-Studio-Setup-1.0.0.exe"

# Should show:
# Status: Valid
# StatusMessage: Signature verified
# SignerCertificate: CN=Your Company Name
```

### Manual Signing (with signtool)

If you want to sign manually:

```powershell
# Find signtool.exe (comes with Windows SDK)
$signtool = "C:\Program Files (x86)\Windows Kits\10\bin\x64\signtool.exe"

# Sign installer
& $signtool sign `
  /f "path\to\cert.pfx" `
  /p "certificate_password" `
  /tr "http://timestamp.digicert.com" `
  /td SHA256 `
  /fd SHA256 `
  /v `
  "Aura-Video-Studio-Setup-1.0.0.exe"
```

## GitHub Actions Configuration

### For Standard Certificate (PFX)

#### 1. Convert Certificate to Base64

```powershell
# Read certificate file
$certBytes = [System.IO.File]::ReadAllBytes("C:\Certificates\aura-cert.pfx")

# Convert to base64
$base64 = [System.Convert]::ToBase64String($certBytes)

# Copy to clipboard
$base64 | Set-Clipboard

# Or save to file
$base64 | Out-File "cert-base64.txt"
```

#### 2. Add GitHub Secrets

1. Go to your repository on GitHub
2. Settings → Secrets and variables → Actions
3. Click "New repository secret"

**Add two secrets**:

**Secret 1: WIN_CSC_LINK**
- Name: `WIN_CSC_LINK`
- Value: (paste base64 string from step 1)

**Secret 2: WIN_CSC_KEY_PASSWORD**
- Name: `WIN_CSC_KEY_PASSWORD`
- Value: (your certificate password)

#### 3. Workflow Configuration

The GitHub Actions workflow (`.github/workflows/build-windows-installer.yml`) is already configured:

```yaml
- name: Build Windows installer
  env:
    WIN_CSC_LINK: ${{ secrets.WIN_CSC_LINK }}
    WIN_CSC_KEY_PASSWORD: ${{ secrets.WIN_CSC_KEY_PASSWORD }}
    GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
  run: |
    cd Aura.Desktop
    npm run build:win
```

#### 4. Test Workflow

1. Push a commit or tag
2. Go to Actions tab
3. Watch build progress
4. Download artifact
5. Verify signature

### For EV Certificate (Hardware Token)

EV certificates on hardware tokens cannot be used directly in GitHub Actions (no physical USB access).

**Options**:

1. **Build Locally**: Sign releases on your machine with token
2. **Use HSM**: Cloud HSM service (expensive)
3. **Use Standard Cert**: For CI/CD, switch to standard cert

**Recommended**: Build and sign locally, then upload to GitHub releases manually.

## Verification

### Windows Explorer

1. Right-click installer
2. Properties → Digital Signatures
3. Should show your company name
4. Status: "This digital signature is OK"

### PowerShell

```powershell
Get-AuthenticodeSignature "Aura-Video-Studio-Setup-1.0.0.exe" | Format-List *
```

**Expected Output**:
```
SignerCertificate : CN=Your Company, O=Your Company, L=City, S=State, C=US
TimeStamperCertificate : CN=DigiCert Timestamp 2023
Status : Valid
StatusMessage : Signature verified.
```

### SmartScreen Test

1. Copy installer to fresh Windows VM
2. Download and run installer
3. Check for warnings:
   - No warning = Certificate working!
   - Warning = Building reputation (normal for first few months)

### signtool Verification

```powershell
$signtool = "C:\Program Files (x86)\Windows Kits\10\bin\x64\signtool.exe"
& $signtool verify /pa /v "Aura-Video-Studio-Setup-1.0.0.exe"
```

## Certificate Management

### Storage Security

**DO**:
- ✅ Store certificate in encrypted location
- ✅ Use strong password
- ✅ Backup certificate securely (encrypted cloud, safe, etc.)
- ✅ Limit access (only authorized team members)
- ✅ Use environment variables (not hardcoded paths)

**DON'T**:
- ❌ Commit certificate to Git repository
- ❌ Share certificate password in plain text
- ❌ Email certificate without encryption
- ❌ Store in public cloud without encryption
- ❌ Use weak password

### Backup

```powershell
# Backup certificate
Copy-Item "C:\Certificates\aura-cert.pfx" -Destination "D:\Backups\Certificates\aura-cert-backup-2025-11-10.pfx"

# Or to encrypted cloud storage
# Use 7-Zip with AES-256 encryption:
7z a -p -mhe=on "aura-cert.7z" "C:\Certificates\aura-cert.pfx"
# Upload aura-cert.7z to secure cloud storage
```

### Renewal

**When to Renew**:
- 30 days before expiration
- Certificate provider will send reminder emails

**Renewal Process**:
1. Order renewal from provider
2. Validation (usually faster for renewals)
3. Receive new certificate
4. Update secrets/environment variables
5. Test with new certificate
6. Update CI/CD before old cert expires

**Important**: Keep old certificate until all signed releases are deprecated.

### Revocation

**If certificate is compromised**:
1. Contact provider immediately
2. Request certificate revocation
3. Get new certificate with different key
4. Update all signing configurations
5. Re-sign and re-release installers

### Monitoring

**Check Expiration**:
```powershell
# View certificate expiration
$cert = Get-PfxCertificate -FilePath "C:\Certificates\aura-cert.pfx"
$cert.NotAfter
# Output: expiration date
```

**Set Calendar Reminder**:
- 90 days before expiration: Start renewal process
- 30 days before: Ensure renewal complete
- 7 days before: Emergency mode if not renewed

## Troubleshooting

### "Certificate not found" Error

**Problem**: electron-builder can't find certificate

**Solutions**:
```powershell
# Check environment variable
echo $env:WIN_CSC_LINK
# Should show certificate path

# Check file exists
Test-Path "C:\Certificates\aura-cert.pfx"
# Should be True

# Try absolute path
$env:WIN_CSC_LINK="C:\full\path\to\cert.pfx"
```

### "Invalid Password" Error

**Problem**: Certificate password incorrect

**Solutions**:
```powershell
# Test password
$cert = Get-PfxCertificate -FilePath "C:\Certificates\aura-cert.pfx"
# If prompts, enter password
# If loads, password is correct

# Check environment variable
echo $env:WIN_CSC_KEY_PASSWORD

# Special characters may need escaping
$env:WIN_CSC_KEY_PASSWORD='password'  # Use single quotes
```

### "Signing Failed" Error

**Problem**: signtool fails to sign

**Checks**:
1. **signtool installed**: Windows SDK required
2. **Certificate valid**: Not expired
3. **Timestamp server**: Try different servers
4. **Permissions**: Run as administrator

**Try different timestamp server**:
```json
{
  "win": {
    "rfc3161TimeStampServer": "http://timestamp.sectigo.com"
  }
}
```

### SmartScreen Still Shows Warnings

**Problem**: Signed installer still triggers SmartScreen

**This is NORMAL for new certificates!**

**Solutions**:
1. **Wait**: Build reputation over 2-6 months
2. **EV Certificate**: Switch to EV for instant trust
3. **More downloads**: Reputation based on usage
4. **Clean history**: No malware reports

**Ways to build reputation faster**:
- Encourage downloads and installations
- Never distribute malware (obvious but critical)
- Consistent signing (same certificate, same publisher)
- Good user feedback (no uninstalls immediately after install)

### Certificate Expired

**Problem**: Forgot to renew, certificate expired

**Impact**:
- New builds won't sign
- Old installers still valid (if timestamped)
- SmartScreen warnings return

**Solution**:
1. Order new certificate immediately
2. Express validation if available
3. Update secrets/environment variables
4. Re-sign latest release
5. Set better renewal reminders

### Private Key Not Found

**Problem**: Certificate imports but can't access private key

**Solution**:
```powershell
# Re-import PFX with proper flags
$password = ConvertTo-SecureString -String "your_password" -Force -AsPlainText
Import-PfxCertificate -FilePath "cert.pfx" `
  -CertStoreLocation Cert:\CurrentUser\My `
  -Password $password `
  -Exportable
```

## Cost Analysis

### Return on Investment

**Without Code Signing**:
- 50-70% of users abandon installation (SmartScreen warning)
- Poor first impression
- Support requests about safety
- Antivirus false positives

**With Code Signing**:
- 5-10% abandonment (initially, with reputation)
- Professional appearance
- Builds trust
- Fewer support requests

**Break-even**: ~100-200 serious users

### Budget Recommendations

**Indie/Hobby Project**:
- Start without code signing
- Document installation process clearly
- Add code signing when have regular users
- **Cost**: $0 initially, $200-$250/year when ready

**Startup/Small Business**:
- Start with Standard (OV) certificate
- Upgrade to EV when budget allows
- **Cost**: $200-$400/year (Standard)

**Established Business**:
- Use EV certificate from day one
- Immediate trust = better conversion
- **Cost**: $400-$800/year (EV)

## Resources

### Certificate Providers

- [Sectigo](https://sectigo.com/ssl-certificates-tls/code-signing)
- [DigiCert](https://www.digicert.com/signing/code-signing-certificates)
- [SSL.com](https://www.ssl.com/certificates/code-signing/)
- [GlobalSign](https://www.globalsign.com/en/code-signing-certificate)

### Documentation

- [Electron Builder Code Signing](https://www.electron.build/code-signing)
- [Microsoft Code Signing](https://learn.microsoft.com/en-us/windows/win32/seccrypto/cryptography-tools)
- [SmartScreen Information](https://learn.microsoft.com/en-us/windows/security/threat-protection/microsoft-defender-smartscreen/microsoft-defender-smartscreen-overview)

### Tools

- [signtool.exe](https://learn.microsoft.com/en-us/dotnet/framework/tools/signtool-exe) (Windows SDK)
- [osslsigncode](https://github.com/mtrojnar/osslsigncode) (Cross-platform signing)

## Getting Help

**Certificate Issues**:
- Contact your certificate provider's support
- Check provider documentation
- Most providers have 24/7 support

**Signing Configuration**:
- GitHub Issues: https://github.com/coffee285/aura-video-studio/issues
- electron-builder Issues: https://github.com/electron-userland/electron-builder/issues
- Stack Overflow: Tag `code-signing`, `electron-builder`

---

**Last Updated**: 2025-11-10  
**Version**: 1.0.0  
**For**: Aura Video Studio maintainers and developers
