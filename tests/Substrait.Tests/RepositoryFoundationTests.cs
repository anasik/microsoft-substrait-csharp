using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Substrait.Tests;

[TestClass]
public sealed class RepositoryFoundationTests
{
    [TestMethod]
    public void LibraryUsesExpectedAssemblyName()
    {
        string assemblyPath = Path.Combine(AppContext.BaseDirectory, "Microsoft.Substrait.dll");
        AssemblyName assemblyName = AssemblyName.GetAssemblyName(assemblyPath);

        Assert.AreEqual("Microsoft.Substrait", assemblyName.Name);
    }
}
