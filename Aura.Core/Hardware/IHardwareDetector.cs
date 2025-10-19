using System.Threading.Tasks;
using Aura.Core.Models;

namespace Aura.Core.Hardware;

public interface IHardwareDetector
{
    Task<SystemProfile> DetectSystemAsync();
    SystemProfile ApplyManualOverrides(SystemProfile detected, HardwareOverrides overrides);
    Task RunHardwareProbeAsync();
}
