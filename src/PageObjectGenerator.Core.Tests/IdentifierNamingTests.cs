using PageObjectGenerator.Core;

namespace PageObjectGenerator.Core.Tests;

[TestFixture]
public class IdentifierNamingTests
{
    [TestCase("Login Page!", "fallback", "login_page")]
    [TestCase("123abc", "fallback", "abc")]
    [TestCase("___", "fallback", "fallback")]
    [TestCase("", "fallback", "fallback")]
    [TestCase(null, "fallback", "fallback")]
    [TestCase("class", "fallback", "fallback")]
    [TestCase("username", "fallback", "username")]
    [TestCase("data-testid-value", "fallback", "data_testid_value")]
    [TestCase("  spaced out  ", "fallback", "spaced_out")]
    public void Sanitize_ProducesExpectedIdentifier(string? raw, string fallback, string expected)
    {
        Assert.That(IdentifierNaming.Sanitize(raw, fallback), Is.EqualTo(expected));
    }

    [TestCase("login_page", "LoginPage")]
    [TestCase("username", "Username")]
    [TestCase("a_0", "A0")]
    [TestCase("this_is_where_you_can", "ThisIsWhereYouCan")]
    [TestCase("multi", "Multi")]
    public void ToPascalCase_ProducesExpectedName(string snakeCase, string expected)
    {
        Assert.That(IdentifierNaming.ToPascalCase(snakeCase), Is.EqualTo(expected));
    }
}
