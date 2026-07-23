namespace Generated;

// Hand-written additions to TheInternetHerokuappComLoginPage - this file is never regenerated
// or overwritten by the tool, so it's safe to add your own methods here.
public sealed partial class TheInternetHerokuappComLoginPage
{
    // A developer added this manually, to verify regeneration doesn't clobber it.
    public void LoginAs(string username, string password)
    {
        FillUsername(username);
        FillPassword(password);
        ClickLogin();
    }
}
