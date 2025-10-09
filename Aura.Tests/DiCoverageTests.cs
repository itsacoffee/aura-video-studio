// tests/Aura.Tests/DiCoverageTests.cs
using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aura.Tests
{
    public class DiCoverageTests
    {
        private static readonly Type[] ProviderInterfaces = new[] {
            Type.GetType("Aura.Core.ILlmProvider, Aura.Core"),
            Type.GetType("Aura.Core.ITtsProvider, Aura.Core"),
            Type.GetType("Aura.Core.IImageProvider, Aura.Core"),
            Type.GetType("Aura.Core.IStockProvider, Aura.Core"),
            Type.GetType("Aura.Core.IVideoComposer, Aura.Core"),
        }.Where(t => t != null).ToArray();

        [Fact]
        public void All_Provider_Implementations_Are_Registered()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.GetName().Name != null && a.GetName().Name.StartsWith("Aura."))
                .ToArray();

            var concrete = ProviderInterfaces
                .SelectMany(iface =>
                    assemblies.SelectMany(a =>
                        a.GetTypes()
                         .Where(t => t != null && !t.IsAbstract && !t.IsInterface && iface.IsAssignableFrom(t))
                         .Select(t => (iface, impl: t))))
                .ToList();

            var services = new ServiceCollection();
            TryInvokeConfigureServices(assemblies, services);
            var provider = services.BuildServiceProvider(validateScopes: false);

            var missing = concrete
                .Where(x => provider.GetServices(x.iface).All(s => s.GetType() != x.impl))
                .Select(x => $"{x.iface.Name} -> {x.impl.FullName}")
                .ToList();

            Assert.True(missing.Count == 0,
                "Missing DI registrations for:\n" + string.Join("\n", missing));
        }

        private static void TryInvokeConfigureServices(Assembly[] assemblies, IServiceCollection services)
        {
            var adders = assemblies
                .SelectMany(a => a.GetTypes())
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
                .Where(m => (m.Name == "AddAura" || m.Name == "ConfigureServices") &&
                            m.GetParameters().Length == 1 &&
                            m.GetParameters()[0].ParameterType == typeof(IServiceCollection));

            foreach (var m in adders)
            {
                try { m.Invoke(null, new object[] { services }); } catch {}
            }
        }
    }
}
