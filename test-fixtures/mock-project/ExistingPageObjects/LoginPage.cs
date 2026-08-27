using OpenQA.Selenium;
using OpenQA.Selenium.Support.PageObjects;

namespace MockProject.PageObjects;

// Hand-written, self-contained (no shared base) - deliberately in a style that differs from
// Cliq's own generated shape, for exercising CliqSettings.ExamplePageObjectFilePath end to end:
// PageFactory-populated [FindsBy] fields instead of By locators + WebDriverWait, action methods
// named after what they do rather than what they click/fill, and the driver kept private with no
// exposed field at all.
public class LoginPage
{
    private readonly IWebDriver driver;

    [FindsBy(How = How.CssSelector, Using = "[data-testid='username']")]
    private IWebElement usernameField;

    [FindsBy(How = How.CssSelector, Using = "[data-testid='password']")]
    private IWebElement passwordField;

    [FindsBy(How = How.CssSelector, Using = "[data-testid='submit']")]
    private IWebElement submitButton;

    public LoginPage(IWebDriver driver)
    {
        this.driver = driver;
        PageFactory.InitElements(driver, this);
    }

    public void EnterUsername(string value) => usernameField.SendKeys(value);

    public void EnterPassword(string value) => passwordField.SendKeys(value);

    public void Submit() => submitButton.Click();

    public bool IsLoaded() => submitButton.Displayed;
}
