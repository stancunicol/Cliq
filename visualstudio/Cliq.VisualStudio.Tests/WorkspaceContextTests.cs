using System;
using System.IO;
using Cliq.VisualStudio.Core;
using NUnit.Framework;

namespace Cliq.VisualStudio.Tests;

[TestFixture]
public sealed class WorkspaceContextTests
{
    private string _root = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "cliq-vsix-context-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    [Test]
    public void ResolveDefaultPrefersUserOverrideThenSolution()
    {
        var solution = Directory.CreateDirectory(Path.Combine(_root, "solution")).FullName;
        var overridePath = Directory.CreateDirectory(Path.Combine(_root, "override")).FullName;
        var context = new IdeContextSnapshot(solution, null, null, null, null);

        Assert.That(WorkspacePathResolver.ResolveDefault(context, overridePath), Is.EqualTo(overridePath));
        Assert.That(WorkspacePathResolver.ResolveDefault(context, null), Is.EqualTo(solution));
    }

    [Test]
    public void ResolveSelectedCliqDocumentFindsWorkspaceAncestor()
    {
        var project = Directory.CreateDirectory(Path.Combine(_root, "project")).FullName;
        var tests = Directory.CreateDirectory(Path.Combine(project, ".cliq", "tests")).FullName;
        File.WriteAllText(Path.Combine(project, ".cliq", "project.json"), "{}");
        var testPath = Path.Combine(tests, "checkout.json");
        File.WriteAllText(testPath, "{}");
        var context = new IdeContextSnapshot(null, null, testPath, null, null);

        Assert.That(WorkspacePathResolver.ResolveForSelectedScope(context), Is.EqualTo(project));
        Assert.That(context.SelectedDocumentKind, Is.EqualTo(CliqDocumentKind.Test));
        Assert.That(context.SelectedCliqEntityId, Is.EqualTo("checkout"));
    }

    [Test]
    public void OverrideKeyIsStableAndCaseInsensitive()
    {
        var first = WorkspacePathResolver.OverrideKey(Path.Combine(_root, "Solution"));
        var second = WorkspacePathResolver.OverrideKey(Path.Combine(_root, "solution"));

        Assert.That(first, Is.EqualTo(second));
        Assert.That(first, Has.Length.EqualTo(64));
    }
}
