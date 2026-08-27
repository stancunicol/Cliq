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

    [TestCase("SubmitLocator", "    // Locator (xpath): //button[@id='submit']", true)]
    [TestCase("SubmitLocator", "    private static readonly By SubmitLocator = By.XPath(\"//button\");", false)]
    [TestCase("SubmitLocator", null, false)]
    [TestCase("submit", "    // Locator (xpath): //button[@id='submit']", false)]
    [TestCase("", "    // Locator (xpath): //button[@id='submit']", false)]
    public void EditorLocateFromSourceIsVisibleOnlyWhenALocatorFieldNameIsSelectedWithItsCommentAbove(
        string selection, string? lineAbove, bool visible)
    {
        var context = Snapshot(
            activeDocument: Path.Combine(_project, "LoginPage.cs"),
            selectedText: selection,
            selectionKind: IdeSelectionKind.Editor,
            lineAboveSelection: lineAbove);

        var state = CliqShellContext.Evaluate(CliqShellCommand.LocateElementFromSource, context);

        Assert.That(state.Visible, Is.EqualTo(visible));
        Assert.That(state.Enabled, Is.EqualTo(visible));
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
    public void NormalCSharpFileOffersBindAndOpenAction()
    {
        var context = Snapshot(
            selectedItem: Path.Combine(_project, "CheckoutTests.cs"),
            selectionKind: IdeSelectionKind.File);

        AssertAvailable(CliqShellCommand.BindProjectAndOpen, context);
        AssertHidden(CliqShellCommand.OpenCliqDocument, context);
        AssertHidden(CliqShellCommand.RunCliqTest, context);
        AssertHidden(CliqShellCommand.RescanCliqPage, context);
    }

    [Test]
    public void NormalNonCSharpFileOffersOnlyBindAndOpenAction()
    {
        var context = Snapshot(
            selectedItem: Path.Combine(_project, "README.md"),
            selectionKind: IdeSelectionKind.File);

        AssertAvailable(CliqShellCommand.BindProjectAndOpen, context);
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

        // The active document is an ordinary file, not a Cliq test/page - these stay enabled
        // here only because the Workbench itself reports an explicit selection, exactly like
        // clicking a row in the Workbench and then using the toolbar/keyboard shortcut instead of
        // the in-Workbench button.
        var run = CliqShellContext.Evaluate(CliqShellCommand.RunSelectedTest, context, hasSelectedTest: true);
        var generate = CliqShellContext.Evaluate(CliqShellCommand.GenerateSelected, context, hasSelectedPage: true);
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

    [Test]
    public void RunSelectedTestAndGenerateSelectedAreDisabledWithoutAnyRealSelection()
    {
        // Same "ordinary file, no Cliq document, workspace bound" context as the test above, but
        // without a Workbench selection this time - the whole point being fixed: a workspace being
        // bound is not, by itself, enough to say there's something to run/generate. This is the
        // exact case that used to silently trust an auto-selected-on-load Workbench item instead
        // of showing disabled.
        var context = Snapshot(
            selectedItem: Path.Combine(_project, "CheckoutTests.cs"),
            selectionKind: IdeSelectionKind.File);

        var run = CliqShellContext.Evaluate(CliqShellCommand.RunSelectedTest, context);
        var generate = CliqShellContext.Evaluate(CliqShellCommand.GenerateSelected, context);

        Assert.Multiple(() =>
        {
            Assert.That(run.Visible, Is.True, "still visible - just nothing to act on yet");
            Assert.That(run.Enabled, Is.False);
            Assert.That(generate.Visible, Is.True);
            Assert.That(generate.Enabled, Is.False);
        });
    }

    [Test]
    public void RunSelectedTestIgnoresAWorkbenchPageSelectionItNeedsATestSpecifically()
    {
        var context = Snapshot(
            selectedItem: Path.Combine(_project, "CheckoutTests.cs"),
            selectionKind: IdeSelectionKind.File);

        // A selected page is real, but RunSelectedTestAsync only ever reads Workbench.SelectedTest -
        // a page selection alone gives it nothing to run.
        var run = CliqShellContext.Evaluate(CliqShellCommand.RunSelectedTest, context, hasSelectedPage: true);

        Assert.That(run.Enabled, Is.False);
    }

    private IdeContextSnapshot Snapshot(
        string? selectedItem = null,
        string? activeDocument = null,
        string? selectedText = null,
        IdeSelectionKind selectionKind = IdeSelectionKind.Unknown,
        string? lineAboveSelection = null)
    {
        return new IdeContextSnapshot(
            _root,
            _project,
            selectedItem,
            activeDocument,
            selectedText,
            selectionKind,
            lineAboveSelection);
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
