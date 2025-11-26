# Firewall Configuration Integration Guide

## Overview
The `FirewallConfigDialog` component provides automatic Windows Firewall configuration for the Aura Video Studio backend. It detects missing firewall rules and offers one-click configuration with UAC elevation.

## Components Created

### Backend
- **`Aura.Core/Utils/FirewallUtility.cs`**: Windows Firewall management utility
  - `RuleExistsAsync()`: Check if firewall rule exists
  - `AddFirewallRuleAsync()`: Add firewall rule with UAC elevation
  - `RemoveFirewallRulesAsync()`: Remove firewall rules
  - `IsAdministrator()`: Check admin privileges
  - `IsWindows()`: Check if running on Windows

### API Endpoints
- **`POST /api/system/firewall/check`**: Check if firewall rule exists
  - Query param: `executablePath` (string)
  - Returns: `{ ruleExists: boolean }`
  
- **`POST /api/system/firewall/add`**: Add firewall rule
  - Query params: `executablePath` (string), `includePublic` (bool)
  - Returns: `{ message: string }`
  - Requires administrator privileges (returns 403 if not admin)

### Frontend
- **`Aura.Web/src/services/api/firewallApi.ts`**: API client functions
  - `checkFirewallRule(executablePath)`: Check rule existence
  - `addFirewallRule(executablePath, includePublic)`: Add firewall rule

- **`Aura.Web/src/components/System/FirewallConfigDialog.tsx`**: React component
  - Displays firewall status and configuration UI
  - Automatic configuration button
  - Manual configuration instructions
  - Success/error messaging

## Integration Examples

### Option 1: Show on Backend Connection Failure

Add to your app's health check or startup component:

```typescript
import { useState, useEffect } from 'react';
import { FirewallConfigDialog } from './components/System';
import { checkFirewallRule } from './services/api/firewallApi';
import { getHealthLive } from './services/api/healthApi';

function App() {
  const [showFirewallDialog, setShowFirewallDialog] = useState(false);

  useEffect(() => {
    const checkBackendAndFirewall = async () => {
      try {
        // Try to connect to backend
        await getHealthLive();
      } catch (error) {
        // Backend not reachable, check if firewall rule exists
        const executablePath = 'C:\\Program Files\\Aura Video Studio\\resources\\backend\\Aura.Api.exe';
        const ruleExists = await checkFirewallRule(executablePath);
        
        if (!ruleExists) {
          setShowFirewallDialog(true);
        }
      }
    };

    checkBackendAndFirewall();
  }, []);

  return (
    <>
      {/* Your app content */}
      <FirewallConfigDialog 
        open={showFirewallDialog} 
        onClose={() => setShowFirewallDialog(false)}
      />
    </>
  );
}
```

### Option 2: Manual Trigger from Settings

Add a button in system settings:

```typescript
import { useState } from 'react';
import { Button } from '@fluentui/react-components';
import { ShieldCheckmark24Regular } from '@fluentui/react-icons';
import { FirewallConfigDialog } from './components/System';

function SystemSettings() {
  const [showFirewallDialog, setShowFirewallDialog] = useState(false);

  return (
    <div>
      <h2>Network Settings</h2>
      <Button
        icon={<ShieldCheckmark24Regular />}
        onClick={() => setShowFirewallDialog(true)}
      >
        Configure Windows Firewall
      </Button>
      
      <FirewallConfigDialog 
        open={showFirewallDialog} 
        onClose={() => setShowFirewallDialog(false)}
        executablePath="C:\\Program Files\\Aura Video Studio\\resources\\backend\\Aura.Api.exe"
      />
    </div>
  );
}
```

### Option 3: First Run Setup Integration

Add to your onboarding wizard:

```typescript
import { useState } from 'react';
import { FirewallConfigDialog } from './components/System';
import { checkFirewallRule } from './services/api/firewallApi';

function OnboardingWizard() {
  const [currentStep, setCurrentStep] = useState(0);
  const [showFirewall, setShowFirewall] = useState(false);

  const checkFirewall = async () => {
    const executablePath = 'C:\\Program Files\\Aura Video Studio\\resources\\backend\\Aura.Api.exe';
    const ruleExists = await checkFirewallRule(executablePath);
    
    if (!ruleExists) {
      setShowFirewall(true);
      return false; // Block progression
    }
    return true; // Allow progression
  };

  const handleNext = async () => {
    if (currentStep === 2) { // Network setup step
      const firewallOk = await checkFirewall();
      if (!firewallOk) return;
    }
    setCurrentStep(currentStep + 1);
  };

  return (
    <>
      {/* Wizard steps */}
      <FirewallConfigDialog 
        open={showFirewall} 
        onClose={() => setShowFirewall(false)}
      />
    </>
  );
}
```

## Backend Startup Logging

The backend automatically checks firewall status on startup and logs warnings:

```
[INFO] Windows Firewall rule exists for backend
```

or

```
[WARN] Windows Firewall rule not found for C:\...\Aura.Api.exe. Users may experience connection issues.
[INFO] To add firewall rule, run as administrator or use the installer
```

## Testing

### Manual Testing
1. **Test firewall detection**:
   - Remove any existing "Aura Video Studio Backend" rules from Windows Firewall
   - Start the application
   - Verify the dialog appears

2. **Test automatic configuration**:
   - Click "Configure Firewall Automatically"
   - Approve UAC prompt
   - Verify success message
   - Confirm rule exists in Windows Firewall

3. **Test manual instructions**:
   - Follow manual steps in the dialog
   - Verify rule is created correctly

### Integration Testing
```bash
# Backend tests
cd Aura.Tests
dotnet test --filter "FirewallUtility"

# Frontend build
cd Aura.Web
npm run build
npm run typecheck
```

## Security Considerations

1. **UAC Elevation**: Adding firewall rules requires administrator privileges. The `netsh` command will trigger UAC prompt automatically.

2. **Rule Scope**: By default, rules are added for Private and Domain networks only. Public networks can be included with `includePublic=true` parameter.

3. **No Credentials**: The utility never stores or transmits credentials. It uses Windows built-in `netsh` command.

4. **Non-Windows**: All operations gracefully no-op on non-Windows platforms.

## Troubleshooting

### Issue: "Administrator privileges required" error
**Solution**: Run the application as administrator, or use the installer which automatically adds rules during installation.

### Issue: Dialog doesn't appear despite backend being unreachable
**Solution**: Check that the firewall check logic is properly integrated in your health check flow.

### Issue: Firewall rule exists but backend still unreachable
**Solution**: Check other potential issues:
- Backend process actually running?
- Correct port (default 5005)?
- Antivirus blocking connections?
- VPN or proxy interference?

## Configuration

### Custom Executable Path
If your backend is in a different location:

```typescript
<FirewallConfigDialog 
  open={true}
  onClose={() => {}}
  executablePath="C:\\Custom\\Path\\To\\Aura.Api.exe"
/>
```

### Include Public Networks
To add rules for public networks (not recommended for security):

```typescript
import { addFirewallRule } from './services/api/firewallApi';

await addFirewallRule(executablePath, true); // includePublic = true
```

## Future Enhancements

Potential improvements for future PRs:
- [ ] Auto-detect executable path from environment
- [ ] Support for multiple network interfaces
- [ ] Advanced rule configuration (specific ports, protocols)
- [ ] Integration with NSIS installer for automatic rule creation
- [ ] Telemetry for firewall configuration success rates
