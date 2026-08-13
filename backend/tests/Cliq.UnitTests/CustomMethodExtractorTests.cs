using Cliq.Infrastructure.Generation;

namespace Cliq.UnitTests;

[TestFixture]
public sealed class CustomMethodExtractorTests
{
    [Test]
    public void Extract_NoMarker_ReturnsNothing()
    {
        const string source = """
            public sealed class LoginPage
            {
                public void ClickSubmit()
                {
                    _wait.Until(d => d.FindElement(By.Id("submit"))).Click();
                }
            }
            """;

        Assert.That(CustomMethodExtractor.Extract(source), Is.Empty);
    }

    [Test]
    public void Extract_BlockBodiedMethod_CapturesAttributeThroughClosingBrace()
    {
        const string source = """
            public sealed class LoginPage
            {
                [CliqCustomMethod]
                public void WaitForSpinnerToClear()
                {
                    _wait.Until(d => !d.FindElements(By.CssSelector(".spinner")).Any());
                }
            }
            """;

        var blocks = CustomMethodExtractor.Extract(source);

        Assert.That(blocks, Has.Count.EqualTo(1));
        Assert.That(blocks[0], Does.StartWith("    [CliqCustomMethod]"));
        Assert.That(blocks[0], Does.EndWith("}"));
        Assert.That(blocks[0], Does.Contain("public void WaitForSpinnerToClear()"));
    }

    [Test]
    public void Extract_XmlDocCommentAboveMarker_IsCapturedNotDropped()
    {
        const string source = """
            public sealed class LoginPage
            {
                /// <summary>Waits for the loading spinner to clear.</summary>
                [CliqCustomMethod]
                public void WaitForSpinnerToClear()
                {
                    _wait.Until(d => !d.FindElements(By.CssSelector(".spinner")).Any());
                }
            }
            """;

        var blocks = CustomMethodExtractor.Extract(source);

        Assert.That(blocks, Has.Count.EqualTo(1));
        Assert.That(blocks[0], Does.StartWith("    /// <summary>Waits for the loading spinner to clear.</summary>"));
        Assert.That(blocks[0], Does.Contain("[CliqCustomMethod]"));
    }

    [Test]
    public void Extract_StackedAttributesAboveMarker_AreAllCaptured()
    {
        const string source = """
            public sealed class LoginPage
            {
                [Obsolete("use WaitForSpinnerToClear instead")]
                [CliqCustomMethod]
                public void OldWait()
                {
                    Thread.Sleep(500);
                }
            }
            """;

        var blocks = CustomMethodExtractor.Extract(source);

        Assert.That(blocks, Has.Count.EqualTo(1));
        Assert.That(blocks[0], Does.StartWith("    [Obsolete(\"use WaitForSpinnerToClear instead\")]"));
    }

    [Test]
    public void Extract_BlankLineAboveMarker_StopsWalkingBackFurther()
    {
        const string source = """
            public sealed class LoginPage
            {
                public void Generated() { }

                [CliqCustomMethod]
                public void Custom()
                {
                    DoSomething();
                }
            }
            """;

        var blocks = CustomMethodExtractor.Extract(source);

        Assert.That(blocks, Has.Count.EqualTo(1));
        Assert.That(blocks[0], Does.StartWith("    [CliqCustomMethod]"));
        Assert.That(blocks[0], Does.Not.Contain("Generated"));
    }

    [Test]
    public void Extract_ExpressionBodiedMethod_EndsAtTheSemicolon()
    {
        const string source = """
            public sealed class LoginPage
            {
                [CliqCustomMethod]
                public bool IsSpinnerGone() => !_driver.FindElements(By.CssSelector(".spinner")).Any();

                public void ClickSubmit() { }
            }
            """;

        var blocks = CustomMethodExtractor.Extract(source);

        Assert.That(blocks, Has.Count.EqualTo(1));
        Assert.That(blocks[0], Does.Contain("IsSpinnerGone() => !_driver"));
        Assert.That(blocks[0], Does.Not.Contain("ClickSubmit"));
    }

    [Test]
    public void Extract_BracesInsideStringsAndComments_DoNotConfuseTheDepthCount()
    {
        const string source = """
            public sealed class LoginPage
            {
                [CliqCustomMethod]
                public void LogState()
                {
                    // a stray brace in a comment: {
                    var template = "{ not real json }";
                    var items = new List<int> { 1, 2, 3 };
                    Console.WriteLine($"state = {template}");
                }
            }
            """;

        var blocks = CustomMethodExtractor.Extract(source);

        Assert.That(blocks, Has.Count.EqualTo(1));
        Assert.That(blocks[0], Does.Contain("Console.WriteLine($\"state = {template}\");"));
        Assert.That(blocks[0].TrimEnd(), Does.EndWith("}"));
    }

    [Test]
    public void Extract_MultipleMarkedMethods_ReturnsEachSeparately()
    {
        const string source = """
            public sealed class LoginPage
            {
                [CliqCustomMethod]
                public void First()
                {
                    DoSomething();
                }

                public void Generated()
                {
                    Click();
                }

                [CliqCustomMethod]
                public void Second()
                {
                    DoSomethingElse();
                }
            }
            """;

        var blocks = CustomMethodExtractor.Extract(source);

        Assert.That(blocks, Has.Count.EqualTo(2));
        Assert.That(blocks[0], Does.Contain("First"));
        Assert.That(blocks[1], Does.Contain("Second"));
        Assert.That(blocks[0], Does.Not.Contain("Generated"));
        Assert.That(blocks[1], Does.Not.Contain("Generated"));
    }

    [Test]
    public void CountMarkerOccurrences_NoMarkers_ReturnsZero()
    {
        const string source = """
            public sealed class LoginPage
            {
                public void ClickSubmit() { }
            }
            """;

        Assert.That(CustomMethodExtractor.CountMarkerOccurrences(source), Is.Zero);
    }

    [Test]
    public void CountMarkerOccurrences_CountsEveryOccurrenceEvenInsideCommentsOrStrings()
    {
        const string source = """
            public sealed class LoginPage
            {
                // TODO: remove [CliqCustomMethod] before merging

                [CliqCustomMethod]
                public void First()
                {
                    DoSomething();
                }
            }
            """;

        // Deliberately naive: unlike Extract, this counts the stray mention inside the comment
        // too - that asymmetry is what makes it useful as an independent cross-check.
        Assert.That(CustomMethodExtractor.CountMarkerOccurrences(source), Is.EqualTo(2));
        Assert.That(CustomMethodExtractor.Extract(source), Has.Count.EqualTo(1));
    }

    [Test]
    public void Extract_RoundTrip_ReExtractingAppendedBlockYieldsTheSameBlock()
    {
        const string customMethod = """
                [CliqCustomMethod]
                public void WaitForSpinnerToClear()
                {
                    _wait.Until(d => !d.FindElements(By.CssSelector(".spinner")).Any());
                }
            """;

        var regenerated = PageObjectCodeGenerator.Generate(
            "LoginPage", "Generated", [], 10_000, [customMethod]);
        var reExtracted = CustomMethodExtractor.Extract(regenerated);

        Assert.That(reExtracted, Has.Count.EqualTo(1));
        Assert.That(reExtracted[0], Is.EqualTo(customMethod));
    }

    [Test]
    public void ExtractAll_UnclosedMethodAtEndOfFile_ReturnsBestEffortSpanInsteadOfNothing()
    {
        const string source = """
            public sealed class LoginPage
            {
                [CliqCustomMethod]
                public void Broken()
                {
                    DoSomething();
            """;

        var spans = CustomMethodExtractor.ExtractAll(source);

        Assert.That(spans, Has.Count.EqualTo(1));
        Assert.That(spans[0].ClosedCleanly, Is.False);
        Assert.That(spans[0].SourceText, Does.Contain("[CliqCustomMethod]"));
        Assert.That(spans[0].SourceText, Does.Contain("Broken"));
        Assert.That(CustomMethodExtractor.Extract(source), Is.Empty);
    }

    [Test]
    public void ExtractAll_UnterminatedStringLiteral_ReturnsBestEffortSpanInsteadOfNothing()
    {
        // An unterminated string/comment is the one failure FindMethodEnd can never recover from
        // by "accidentally" balancing back out later (unlike a bare missing '}', which later,
        // unrelated braces further down the file can inadvertently close) - see the doc comment on
        // ExtractAll for why a plain missing brace isn't used to exercise this path instead.
        const string source = """
            public sealed class LoginPage
            {
                [CliqCustomMethod]
                public void Broken()
                {
                    var s = "unterminated;
                }
            }
            """;

        var spans = CustomMethodExtractor.ExtractAll(source);

        Assert.That(spans, Has.Count.EqualTo(1));
        Assert.That(spans[0].ClosedCleanly, Is.False);
        Assert.That(spans[0].SourceText, Does.Contain("Broken"));
        Assert.That(CustomMethodExtractor.Extract(source), Is.Empty);
    }

    [Test]
    public void IsBalanced_EqualBraceCounts_ReturnsTrue()
    {
        const string source = """
            public sealed class LoginPage
            {
                [CliqCustomMethod]
                public void Clean()
                {
                    DoSomething();
                }
            }
            """;

        Assert.That(CustomMethodExtractor.IsBalanced(source), Is.True);
    }

    [Test]
    public void IsBalanced_MissingClosingBrace_ReturnsFalse()
    {
        const string source = """
            public sealed class LoginPage
            {
                [CliqCustomMethod]
                public void Broken()
                {
                    DoSomething();
            }
            """;

        Assert.That(CustomMethodExtractor.IsBalanced(source), Is.False);
    }

    [Test]
    public void IsBalanced_StrayBraceInsideStringOrComment_IsIgnored()
    {
        const string source = """
            public sealed class LoginPage
            {
                // a stray brace in a comment: {
                [CliqCustomMethod]
                public void Clean()
                {
                    var template = "{ not real json }";
                }
            }
            """;

        Assert.That(CustomMethodExtractor.IsBalanced(source), Is.True);
    }

    [Test]
    public void ExtractAll_AllCleanMethods_MatchesExtractExactly()
    {
        const string source = """
            public sealed class LoginPage
            {
                [CliqCustomMethod]
                public void First()
                {
                    DoSomething();
                }
            }
            """;

        var spans = CustomMethodExtractor.ExtractAll(source);

        Assert.That(spans, Has.Count.EqualTo(1));
        Assert.That(spans[0].ClosedCleanly, Is.True);
        Assert.That(spans[0].SourceText, Is.EqualTo(CustomMethodExtractor.Extract(source)[0]));
    }
}
