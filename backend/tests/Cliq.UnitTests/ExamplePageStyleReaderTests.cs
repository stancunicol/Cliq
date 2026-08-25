using Cliq.Infrastructure.Generation;

namespace Cliq.UnitTests;

[TestFixture]
public sealed class ExamplePageStyleReaderTests
{
    [Test]
    public void Read_CliqsOwnConvention_DetectsAsIs()
    {
        const string source = """
            namespace Test;

            public sealed class LoginPage
            {
                private readonly IWebDriver _driver;

                public LoginPage(IWebDriver driver)
                {
                    _driver = driver;
                }
            }
            """;

        var style = ExamplePageStyleReader.Read(source);

        Assert.Multiple(() =>
        {
            Assert.That(style.DriverParameterName, Is.EqualTo("driver"));
            Assert.That(style.DriverFieldName, Is.EqualTo("_driver"));
            Assert.That(ExamplePageStyleReader.DeriveWaitFieldName(style.DriverFieldName), Is.EqualTo("_wait"));
        });
    }

    // Matches the real test-fixtures/mock-project/ExistingPageObjects/LoginPage.cs fixture: a
    // plain, unprefixed field name.
    [Test]
    public void Read_PageFactoryStyleFixture_DetectsUnprefixedFieldName()
    {
        const string source = """
            namespace MockProject.PageObjects;

            public class LoginPage
            {
                private readonly IWebDriver driver;

                public LoginPage(IWebDriver driver)
                {
                    this.driver = driver;
                }
            }
            """;

        var style = ExamplePageStyleReader.Read(source);

        Assert.Multiple(() =>
        {
            Assert.That(style.DriverParameterName, Is.EqualTo("driver"));
            Assert.That(style.DriverFieldName, Is.EqualTo("driver"));
            Assert.That(ExamplePageStyleReader.DeriveWaitFieldName(style.DriverFieldName), Is.EqualTo("wait"));
        });
    }

    [Test]
    public void Read_NoIWebDriverMemberOrParameter_FallsBackToDefaults()
    {
        const string source = """
            namespace Test;

            public sealed class EmptyPage
            {
            }
            """;

        var style = ExamplePageStyleReader.Read(source);

        Assert.Multiple(() =>
        {
            Assert.That(style.DriverParameterName, Is.EqualTo(ExamplePageStyle.Default.DriverParameterName));
            Assert.That(style.DriverFieldName, Is.EqualTo(ExamplePageStyle.Default.DriverFieldName));
        });
    }

    [Test]
    public void DeriveWaitFieldName_MPrefixed_PreservesThePrefix()
    {
        Assert.That(ExamplePageStyleReader.DeriveWaitFieldName("m_driver"), Is.EqualTo("m_wait"));
    }

    [Test]
    public void Read_ExampleWithExplicitWaitField_UsesItsActualNameInsteadOfDeriving()
    {
        const string source = """
            namespace Test;

            public sealed class LoginPage
            {
                private readonly IWebDriver driver;
                private readonly WebDriverWait pageWait;

                public LoginPage(IWebDriver driver)
                {
                    this.driver = driver;
                    pageWait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                }
            }
            """;

        var style = ExamplePageStyleReader.Read(source);

        Assert.That(style.WaitFieldName, Is.EqualTo("pageWait"));
    }

    [Test]
    public void Read_ExampleWithoutAWaitField_LeavesWaitFieldNameNull()
    {
        const string source = """
            namespace Test;

            public sealed class LoginPage
            {
                private readonly IWebDriver driver;

                public LoginPage(IWebDriver driver)
                {
                    this.driver = driver;
                }
            }
            """;

        var style = ExamplePageStyleReader.Read(source);

        Assert.That(style.WaitFieldName, Is.Null);
    }

    // Before FindPageClass existed, Read just took the first ClassDeclarationSyntax found
    // anywhere in the file, including nested ones - a private helper class declared inside the
    // real page class, if it happened to come first in a document-order scan, would shadow it.
    [Test]
    public void Read_NestedHelperClassDeclaredBeforeItsOwnMembers_DoesNotShadowTheOuterPageClass()
    {
        const string source = """
            namespace Test;

            public sealed class LoginPage
            {
                private sealed class Helper
                {
                    private readonly IWebDriver driver;
                }

                private readonly IWebDriver driver;

                public LoginPage(IWebDriver driver)
                {
                    this.driver = driver;
                }
            }
            """;

        var style = ExamplePageStyleReader.Read(source);

        Assert.That(style.DriverFieldName, Is.EqualTo("driver"));
    }

    // Before FindPageClass preferred a constructor with an IWebDriver parameter, Read just took
    // whichever top-level class appeared first in the file - an unrelated helper class declared
    // earlier than the real page class would have been read instead of it.
    [Test]
    public void Read_UnrelatedTopLevelClassDeclaredFirst_PrefersTheClassWithAWebDriverConstructor()
    {
        const string source = """
            namespace Test;

            public sealed class Helper
            {
                public string Name { get; set; } = "";
            }

            public sealed class LoginPage
            {
                private readonly IWebDriver driver;

                public LoginPage(IWebDriver driver)
                {
                    this.driver = driver;
                }
            }
            """;

        var style = ExamplePageStyleReader.Read(source);

        Assert.That(style.DriverFieldName, Is.EqualTo("driver"));
    }
}
