using Cliq.VisualStudio.Core;
using NUnit.Framework;

namespace Cliq.VisualStudio.Tests;

[TestFixture]
public sealed class CliqDocumentClassifierTests
{
    [TestCase(@"C:\repo\.cliq\tests\checkout.json", CliqDocumentKind.Test)]
    [TestCase(@"C:\repo\.CLIQ\PAGES\checkout.json", CliqDocumentKind.Page)]
    [TestCase(@"C:\repo\.cliq\tests\checkout.txt", CliqDocumentKind.Other)]
    [TestCase(@"C:\repo\tests\checkout.json", CliqDocumentKind.Other)]
    public void ClassifyRecognizesCliqDocuments(string path, CliqDocumentKind expected)
    {
        Assert.That(CliqDocumentClassifier.Classify(path), Is.EqualTo(expected));
    }

    [TestCase(" https://example.test/page ", true)]
    [TestCase("http://localhost:5000", true)]
    [TestCase("file:///C:/repo/page.html", false)]
    [TestCase("not a url", false)]
    public void TryParseHttpSelectionAcceptsOnlyHttpUris(string selection, bool expected)
    {
        Assert.That(CliqDocumentClassifier.TryParseHttpSelection(selection, out _), Is.EqualTo(expected));
    }
}
