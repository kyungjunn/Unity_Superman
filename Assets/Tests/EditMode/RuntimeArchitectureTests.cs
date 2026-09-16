using System.Linq;
using System.Reflection;
using GrowNa.Core;
using NUnit.Framework;

namespace GrowNa.Tests
{
    public class RuntimeArchitectureTests
    {
        [Test]
        public void Runtime_types_declare_no_static_Instance()
        {
            var offenders = typeof(Wallet).Assembly.GetTypes()
                .Where(t => t.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static) != null)
                .Select(t => t.FullName)
                .OrderBy(n => n)
                .ToArray();
            Assert.IsEmpty(offenders, string.Join(", ", offenders));
        }
    }
}
