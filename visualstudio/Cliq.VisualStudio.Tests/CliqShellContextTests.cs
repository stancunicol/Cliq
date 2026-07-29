using System;
using System.IO;
using Cliq.VisualStudio.Core;
using Cliq.VisualStudio.Core.Shell;
using NUnit.Framework;

namespace Cliq.VisualStudio.Tests;

[TestFixture]
public sealed class CliqShellContextTests
{
    private string _root = null!;
    private string _project = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "cliq-vsix-shell-" + Guid.NewGuid().ToString("N"));
        _project = Directory.CreateDirectory(Path.Combine(_root, "project")).FullName;
        Directory.CreateDirectory(Path.Combine(_project, ".cliq", "tests"));
        Directory.CreateDirectory(Path.Combine(_project, ".cliq", "pages"));
        File.WriteAllText(Path.Combine(_project, ".cliq", "project.json"), "{}");
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    [TestCase(" https://example.test/products?id=12 ", true)]
    [TestCase("HTTP://LOCALHOST:4200/path", true)]
    [TestCase("ftp://example.test/file", false)]
    [TestCase("/relative/path", false)]
    [TestCase("example.test", false)]
    [TestCase("", false)]
    public void EditorScanIsVisibleOnlyForAnAbsoluteHttpSelection(string selection, bool visible)
    {
        var context = Snapshot(
            activeDocument: Path.Combine(_project, "source.cs"),
            selectedText: selection,
            selectionKind: IdeSelectionKind.Editor);

        var state = CliqShellContext.Evaluate(CliqShellCommand.ScanSelectedUrl, context);

        Assert.That(state.Visible, Is.EqualTo(visible));
        Assert.That(state.Enabled, Is.EqualTo(visible));
        Assert.That(state.Text, Is.Null);
    }

    [Test]
    public void TestDocumentOffersOpenRunAndGenerateButNotRescan()
    {
        var context = Snapshot(
            selectedItem: Path.Combine(_project, ".cliq", "tests", "checkout.json"),
            selectionKind: IdeSelectionKind.File);

        AssertAvailable(CliqShellCommand.OpenCliqDocument, context);
        AssertAvailable(CliqShellCommand.RunCliqTest, context);
        AssertAvailable(CliqShellCommand.GenerateCliqDocument, context);
        AssertHidden(CliqShellCommand.RescanCliqPage, context);
    }

    [Test]
    public void PageDocumentOffersOpenRescanAndGenerateButNotRun()
    {
        var context = Snapshot(
            selectedItem: Path.Combine(_project, ".cliq", "pages", "checkout.json"),
            selectionKind: IdeSelectionKind.File);

        AssertAvailable(CliqShellCommand.OpenCliqDocument, context);
        AssertAvailable(CliqShellCommand.RescanCliqPage, context);
        AssertAvailable(CliqShellCommand.GenerateCliqDocument, context);
        AssertHidden(CliqShellCommand.RunCliqTest, context);
    }

    [Test]
    public void NormalProjectFileOnlyOffersBindAndOpenAction()
    {
        var context = Snapshot(
            selectedItem: Path.Combine(_project, "CheckoutTests.cs"),
            selectionKind: IdeSelectionKind.File);

        AssertAvailable(CliqShellCommand.BindProjectAndOpen, context);
        AssertHidden(CliqShellCommand.OpenCliqDocument, context);
        AssertHidden(CliqShellCommand.RunCliqTest, context);
        AssertHidden(CliqShellCommand.RescanCliqPage, context);
    }

    [TestCase(IdeSelectionKind.Solution)]
    [TestCase(IdeSelectionKind.Project)]
    public void SolutionAndProjectScopesOfferWorkspaceActions(IdeSelectionKind selectionKind)
    {
        var context = Snapshot(selectionKind: selectionKind);

        AssertAvailable(CliqShellCommand.BindWorkspace, context);
        AssertAvailable(CliqShellCommand.OpenScopedWorkbench, context);
        AssertAvailable(CliqShellCommand.GenerateScopedAll, context);
    }

    [Test]
    public void MainCommandsRemainAvailableForWorkbenchSelections()
    {
        var context = Snapshot(
            selectedItem: Path.Combine(_project, "CheckoutTests.cs"),
            selectionKind: IdeSelectionKind.File);

        var run = CliqShellContext.Evaluate(CliqShellCommand.RunSelectedTest, context);
        var generate = CliqShellContext.Evaluate(CliqShellCommand.GenerateSelected, context);
        var generateAll = CliqShellContext.Evaluate(CliqShellCommand.GenerateAll, context);

        Assert.Multiple(() =>
        {
            Assert.That(run.Visible, Is.True);
            Assert.That(run.Enabled, Is.True);
            Assert.That(generate.Visible, Is.True);
            Assert.That(generate.Enabled, Is.True);
            Assert.That(generateAll.Enabled, Is.True);
        });
    }

    private IdeContextSnapshot Snapshot(
        string? selectedItem = null,
        string? activeDocument = null,
        string? selectedText = null,
        IdeSelectionKind selectionKind = IdeSelectionKind.Unknown)
    {
        return new IdeContextSnapshot(
            _root,
            _project,
            selectedItem,
            activeDocument,
            selectedText,
            selectionKind);
    }

    private static void AssertAvailable(CliqShellCommand command, IdeContextSnapshot context)
    {
        var state = CliqShellContext.Evaluate(command, context);
        Assert.Multiple(() =>
        {
            Assert.That(state.Visible, Is.True, command.ToString());
            Assert.That(state.Enabled, Is.True, command.ToString());
        });
    }

    private static void AssertHidden(CliqShellCommand command, IdeContextSnapshot context)
    {
        var state = CliqShellContext.Evaluate(command, context);
        Assert.Multiple(() =>
        {
            Assert.That(state.Visible, Is.False, command.ToString());
            Assert.That(state.Enabled, Is.False, command.ToString());
        });
    }
}
