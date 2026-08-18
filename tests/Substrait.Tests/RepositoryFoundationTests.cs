using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Substrait.Tests;

[TestClass]
public sealed class RepositoryFoundationTests
{
    [TestMethod]
    public void LibraryUsesExpectedAssemblyName()
    {
        Assembly assembly = Assembly.Load("Microsoft.Substrait");

        Assert.AreEqual("Microsoft.Substrait", assembly.GetName().Name);
    }
}
