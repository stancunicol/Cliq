using Cliq.Domain;

namespace Cliq.IntegrationTests;

internal static class TestData
{
    internal static PageDefinition Page(string url = "https://example.test/login") =>
        new(
            PageDefinition.CurrentSchemaVersion,
            "login",
            "Login",
            url,
            [
                new PageElement(
                    "username",
                    "username",
                    "input",
                    StepAction.Fill,
                    new Locator(LocatorStrategy.TestId, "username")),
                new PageElement(
                    "submit",
                    "submit",
                    "button",
                    StepAction.Click,
                    new Locator(LocatorStrategy.XPath, "//button[@type='submit']")),
            ],
            DateTimeOffset.UtcNow);

    internal static TestCase Test(string url = "https://example.test/login") =>
        new(
            TestCase.CurrentSchemaVersion,
            "login-flow",
            "Login flow",
            "Integration fixture",
            ["integration"],
            [
                new TestStep("navigate", StepAction.Navigate, "Open login", true, Url: url),
                new TestStep(
                    "fill",
                    StepAction.Fill,
                    "Enter username",
                    true,
                    "login",
                    "username",
                    Value: StepValue.Literal("tester")),
                new TestStep("click", StepAction.Click, "Submit", true, "login", "submit"),
            ],
            DateTimeOffset.UtcNow);
}
