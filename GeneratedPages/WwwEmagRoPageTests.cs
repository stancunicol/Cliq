using System.Collections.Generic;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;

namespace Generated;

[TestFixture]
public sealed class WwwEmagRoPageTests
{
    private IWebDriver _driver = null!;

    [OneTimeSetUp]
    public void VerifyWwwEmagRoPage()
    {
        using var driver = new EdgeDriver();
        driver.Navigate().GoToUrl("https://www.emag.ro/?msockid=390698c7eb656c54005e8e21ea9f6d63");
        var missing = new List<string>();

        if (!TryFind(driver, By.CssSelector("div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"))) missing.Add("deschide (div:nth-child(1) > div:nth-child(3) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > nav:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(1)"))) missing.Add("a_1 (div:nth-child(10) > nav:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > nav:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(2)"))) missing.Add("a_2 (div:nth-child(10) > nav:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(2))");
        if (!TryFind(driver, By.Id("searchboxAutocomplete"))) missing.Add("searchboxautocomplete (#searchboxAutocomplete)");
        if (!TryFind(driver, By.Id("searchboxTrigger"))) missing.Add("searchboxtrigger (#searchboxTrigger)");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > nav:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > form:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(2)"))) missing.Add("caut (div:nth-child(10) > nav:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > form:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(2))");
        if (!TryFind(driver, By.Id("my_wishlist"))) missing.Add("my_wishlist (#my_wishlist)");
        if (!TryFind(driver, By.Id("my_cart"))) missing.Add("my_cart (#my_cart)");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(1)"))) missing.Add("a_8 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(1)"))) missing.Add("a_9 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"))) missing.Add("a_10 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > a:nth-child(1)"))) missing.Add("a_11 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(5) > a:nth-child(1)"))) missing.Add("a_12 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(5) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(6) > a:nth-child(1)"))) missing.Add("a_13 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(6) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(1)"))) missing.Add("a_14 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(2)"))) missing.Add("a_15 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(2))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(2) > div:nth-child(1) > a:nth-child(1)"))) missing.Add("ofertele_emag (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(2) > div:nth-child(1) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(2) > div:nth-child(2) > a:nth-child(1)"))) missing.Add("genius (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(2) > div:nth-child(2) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(2) > div:nth-child(3) > a:nth-child(1)"))) missing.Add("easy_buyback (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(2) > div:nth-child(3) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(2) > div:nth-child(4) > a:nth-child(1)"))) missing.Add("genius_deals (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(2) > div:nth-child(4) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(2) > div:nth-child(5) > a:nth-child(1)"))) missing.Add("oferta_zilei (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(2) > div:nth-child(5) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(2) > div:nth-child(6) > a:nth-child(1)"))) missing.Add("resigilate (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(2) > div:nth-child(6) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > h2:nth-child(1)"))) missing.Add("produse_alese_pentru_tine (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > h2:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > h2:nth-child(1) > button:nth-child(1)"))) missing.Add("button_23 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > h2:nth-child(1) > button:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"))) missing.Add("button_24 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"))) missing.Add("smart_deals (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)"))) missing.Add("manara_erotic_oracle_chakra_eros_and_a (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)"))) missing.Add("manara_erotic_oracle_chakra_eros_and_a_2 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(2) > a:nth-child(1)"))) missing.Add("review_uri_2 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(2) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"))) missing.Add("adauga_in_cos (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"))) missing.Add("button_30 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"))) missing.Add("a_31 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)"))) missing.Add("pasator_de_rosii_home_hghd1200pp_compat (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)"))) missing.Add("pasator_de_rosii_home_hghd1200pp_compat_2 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"))) missing.Add("adauga_in_cos_2 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"))) missing.Add("button_35 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"))) missing.Add("smart_deals_2 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)"))) missing.Add("voal_de_mireasa_qttvbtna_3x1_5m_polies (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)"))) missing.Add("voal_de_mireasa_qttvbtna_3x1_5m_polies_2 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"))) missing.Add("adauga_in_cos_3 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"))) missing.Add("button_40 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"))) missing.Add("a_41 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)"))) missing.Add("set_de_walkie_talkies_pentru_copii_3_bu (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)"))) missing.Add("set_de_walkie_talkies_pentru_copii_3_bu_2 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"))) missing.Add("adauga_in_cos_4 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(5) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"))) missing.Add("button_45 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(5) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(5) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"))) missing.Add("oferta_zilei_2 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(5) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(5) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)"))) missing.Add("folie_de_vinil_pentru_spate_compatibil_c (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(5) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(5) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)"))) missing.Add("folie_de_vinil_pentru_spate_compatibil_c_2 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(5) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(5) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"))) missing.Add("adauga_in_cos_5 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(5) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(6) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"))) missing.Add("button_50 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(6) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(6) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"))) missing.Add("super_pret (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(6) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(6) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1)"))) missing.Add("puma_pantofi_sport_flatform_de_piele_m (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(6) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(6) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1) > h2:nth-child(1)"))) missing.Add("puma_pantofi_sport_flatform_de_piele_m_2 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(6) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1) > h2:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(6) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"))) missing.Add("adauga_in_cos_6 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(6) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(7) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"))) missing.Add("button_55 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(7) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(7) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"))) missing.Add("a_56 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(7) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(7) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1)"))) missing.Add("oem_geanta_dama_mica_nod_papion_alba (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(7) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(7) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1) > h2:nth-child(1)"))) missing.Add("oem_geanta_dama_mica_nod_papion_alba_2 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(7) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1) > h2:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(7) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"))) missing.Add("adauga_in_cos_7 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(7) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"))) missing.Add("button_60 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"))) missing.Add("a_61 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)"))) missing.Add("oukitel_c62_6_88_128gb_4gb_5150mah_mov (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)"))) missing.Add("oukitel_c62_6_88_128gb_4gb_5150mah_mov_2 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"))) missing.Add("adauga_in_cos_8 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(9) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"))) missing.Add("button_65 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(9) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(9) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"))) missing.Add("a_66 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(9) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(9) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)"))) missing.Add("husa_bestcase_slim_glassy_compatibila (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(9) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(9) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)"))) missing.Add("husa_bestcase_slim_glassy_compatibila_2 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(9) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(9) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"))) missing.Add("adauga_in_cos_9 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(9) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(10) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"))) missing.Add("button_70 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(10) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(10) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"))) missing.Add("multi_deals (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(10) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(10) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)"))) missing.Add("unitate_intern_aer_condi_ionat_tip_case (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(10) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(10) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)"))) missing.Add("unitate_intern_aer_condi_ionat_tip_case_2 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(10) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(10) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"))) missing.Add("adauga_in_cos_10 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(10) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(11) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"))) missing.Add("button_75 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(11) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(11) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"))) missing.Add("a_76 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(11) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(11) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)"))) missing.Add("filtru_ulei_volvo_fh_ii_540_din_2012_kw (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(11) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(11) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)"))) missing.Add("filtru_ulei_volvo_fh_ii_540_din_2012_kw_2 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(11) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(11) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"))) missing.Add("adauga_in_cos_11 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(11) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(12) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"))) missing.Add("button_80 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(12) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(12) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"))) missing.Add("a_81 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(12) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(12) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)"))) missing.Add("aspirator_robot_inteligent_negru_70_90 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(12) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(12) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)"))) missing.Add("aspirator_robot_inteligent_negru_70_90_2 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(12) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(12) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"))) missing.Add("adauga_in_cos_12 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(12) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(13) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"))) missing.Add("button_85 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(13) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(13) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"))) missing.Add("a_86 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(13) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(13) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)"))) missing.Add("televizor_qled_smart_hisense_50e7q_ultr (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(13) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(13) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)"))) missing.Add("televizor_qled_smart_hisense_50e7q_ultr_2 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(13) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(13) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"))) missing.Add("adauga_in_cos_13 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(13) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(14) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"))) missing.Add("button_90 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(14) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(14) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"))) missing.Add("super_pret_2 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(14) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(14) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)"))) missing.Add("laptop_apple_macbook_pro_14_cu_procesor (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(14) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(14) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)"))) missing.Add("laptop_apple_macbook_pro_14_cu_procesor_2 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(14) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(14) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(2) > a:nth-child(1)"))) missing.Add("de_review_uri_21 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(14) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(2) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(14) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"))) missing.Add("adauga_in_cos_14 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(14) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(15) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"))) missing.Add("button_96 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(15) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(15) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"))) missing.Add("multi_deals_2 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(15) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(15) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)"))) missing.Add("apa_de_parfum_luxerom_blue_icon_100ml (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(15) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(15) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)"))) missing.Add("apa_de_parfum_luxerom_blue_icon_100ml_2 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(15) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(15) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"))) missing.Add("adauga_in_cos_15 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(15) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(16) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"))) missing.Add("button_101 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(16) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(16) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"))) missing.Add("a_102 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(16) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(16) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1)"))) missing.Add("baldinini_pantofi_sport_wedge_cu_garni (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(16) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(16) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1) > h2:nth-child(1)"))) missing.Add("baldinini_pantofi_sport_wedge_cu_garni_2 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(16) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1) > h2:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(16) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > a:nth-child(1)"))) missing.Add("vezi_detalii (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(16) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(17) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"))) missing.Add("button_106 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(17) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(17) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"))) missing.Add("super_pret_3 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(17) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(17) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1)"))) missing.Add("max_co_rochie_evazata_cu_maneci_scurte (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(17) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(17) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1) > h2:nth-child(1)"))) missing.Add("max_co_rochie_evazata_cu_maneci_scurte_2 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(17) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1) > h2:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(17) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"))) missing.Add("adauga_in_cos_16 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(17) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(18) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"))) missing.Add("button_111 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(18) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(18) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"))) missing.Add("super_pret_4 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(18) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(18) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)"))) missing.Add("set_de_constructie_353_piese_pistol_be (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(18) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(18) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)"))) missing.Add("set_de_constructie_353_piese_pistol_be_2 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(18) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(18) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"))) missing.Add("adauga_in_cos_17 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(18) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(19) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"))) missing.Add("button_116 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(19) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(19) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"))) missing.Add("cadou_super_pret (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(19) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(19) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1)"))) missing.Add("super_trends_bratara_tennis_din_argint (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(19) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(19) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1) > h2:nth-child(1)"))) missing.Add("super_trends_bratara_tennis_din_argint_2 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(19) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1) > h2:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(19) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"))) missing.Add("adauga_in_cos_18 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(19) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(20) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"))) missing.Add("button_121 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(20) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(20) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"))) missing.Add("super_pret_5 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(20) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(20) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)"))) missing.Add("apple_watch_se_2025_gps_carcasa_star (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(20) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(20) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)"))) missing.Add("apple_watch_se_2025_gps_carcasa_star_2 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(20) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(20) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(2) > a:nth-child(1)"))) missing.Add("de_review_uri_87 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(20) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(2) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(20) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"))) missing.Add("adauga_in_cos_19 (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(20) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > h3:nth-child(1)"))) missing.Add("aboneaz_te_la_newsletter_i_afl_cu_pri (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > h3:nth-child(1))");
        if (!TryFind(driver, By.Name("name"))) missing.Add("name ([name=\"name\"])");
        if (!TryFind(driver, By.Name("email"))) missing.Add("email ([name=\"email\"])");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > form:nth-child(3) > div:nth-child(1) > div:nth-child(3) > button:nth-child(1)"))) missing.Add("m_abonez (div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > form:nth-child(3) > div:nth-child(1) > div:nth-child(3) > button:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > section:nth-child(6) > div:nth-child(1) > div:nth-child(1) > h2:nth-child(1)"))) missing.Add("istoricul_tau_de_navigare (div:nth-child(10) > section:nth-child(6) > div:nth-child(1) > div:nth-child(1) > h2:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > section:nth-child(6) > div:nth-child(1) > div:nth-child(3) > div:nth-child(1) > h2:nth-child(1)"))) missing.Add("alti_vizitatori_au_fost_interesati_si_de (div:nth-child(10) > section:nth-child(6) > div:nth-child(1) > div:nth-child(3) > div:nth-child(1) > h2:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > section:nth-child(6) > div:nth-child(1) > div:nth-child(3) > div:nth-child(1) > a:nth-child(2)"))) missing.Add("sterge_istoricul_de_navigare (div:nth-child(10) > section:nth-child(6) > div:nth-child(1) > div:nth-child(3) > div:nth-child(1) > a:nth-child(2))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(1)"))) missing.Add("servicii_pentru_clienti (div:nth-child(10) > footer:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(1)"))) missing.Add("comenzi_si_livrare (div:nth-child(10) > footer:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"))) missing.Add("suport_clienti (div:nth-child(10) > footer:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > a:nth-child(1)"))) missing.Add("emag_ro (div:nth-child(10) > footer:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(1)"))) missing.Add("dante_international (div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(2)"))) missing.Add("varianta_mobile (div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(2))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(1)"))) missing.Add("a_140 (div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(2)"))) missing.Add("a_141 (div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(2))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(3)"))) missing.Add("a_142 (div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(3))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(4)"))) missing.Add("a_143 (div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(4))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"))) missing.Add("flip_ro (div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(2)"))) missing.Add("flip_bg (div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(2))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(3)"))) missing.Add("rejoy_hu (div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(3))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(4)"))) missing.Add("emag_bg (div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(4))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(5)"))) missing.Add("emag_hu (div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(5))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(8)"))) missing.Add("a_149 (div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(8))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(26) > div:nth-child(1) > div:nth-child(2) > a:nth-child(1)"))) missing.Add("intra_in_cont (div:nth-child(26) > div:nth-child(1) > div:nth-child(2) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(26) > div:nth-child(1) > div:nth-child(2) > a:nth-child(2)"))) missing.Add("cont_nou (div:nth-child(26) > div:nth-child(1) > div:nth-child(2) > a:nth-child(2))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(29) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > p:nth-child(3) > a:nth-child(1)"))) missing.Add("politica_de_utilizare_cookie_uri (div:nth-child(29) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > p:nth-child(3) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(29) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"))) missing.Add("accept_toate (div:nth-child(29) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(29) > div:nth-child(1) > div:nth-child(2) > button:nth-child(2)"))) missing.Add("refuz_toate (div:nth-child(29) > div:nth-child(1) > div:nth-child(2) > button:nth-child(2))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(29) > div:nth-child(1) > div:nth-child(2) > button:nth-child(3)"))) missing.Add("administreaz_preferin_ele (div:nth-child(29) > div:nth-child(1) > div:nth-child(2) > button:nth-child(3))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(33) > div:nth-child(1) > a:nth-child(2)"))) missing.Add("vezi_oferta (div:nth-child(33) > div:nth-child(1) > a:nth-child(2))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(33) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"))) missing.Add("precomand_acum_noile_samsung_galaxy_vez (div:nth-child(33) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1))");

        if (missing.Count > 0)
        {
            Assert.Fail("Page Object verification failed - missing element(s): " + string.Join(", ", missing));
        }
    }

    private static bool TryFind(IWebDriver driver, By by)
    {
        try
        {
            driver.FindElement(by);
            return true;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    [SetUp]
    public void SetUp()
    {
        _driver = new EdgeDriver();
        _driver.Navigate().GoToUrl("https://www.emag.ro/?msockid=390698c7eb656c54005e8e21ea9f6d63");
    }

    [TearDown]
    public void TearDown()
    {
        _driver.Quit();
        _driver.Dispose();
    }

    [Test]
    public void Deschide_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void A1_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > nav:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void A2_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > nav:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(2)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void Searchboxautocomplete_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.Id("searchboxAutocomplete"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void Searchboxtrigger_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.Id("searchboxTrigger"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void Caut_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > nav:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > form:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(2)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void MyWishlist_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.Id("my_wishlist"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void MyCart_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.Id("my_cart"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void A8_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void A9_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void A10_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void A11_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void A12_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(5) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void A13_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(6) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void A14_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void A15_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(2)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void OferteleEmag_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(2) > div:nth-child(1) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void Genius_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(2) > div:nth-child(2) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void EasyBuyback_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(2) > div:nth-child(3) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void GeniusDeals_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(2) > div:nth-child(4) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void OfertaZilei_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(2) > div:nth-child(5) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void Resigilate_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(2) > div:nth-child(6) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void ProduseAlesePentruTine_TextMatchesSnapshot()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > h2:nth-child(1)"));
        Assert.That(element.Text, Is.EqualTo("Produse alese pentru tine"));
    }

    [Test]
    public void Button23_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > h2:nth-child(1) > button:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void Button24_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void SmartDeals_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void ManaraEroticOracleChakraErosAndA_TextMatchesSnapshot()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)"));
        Assert.That(element.Text, Is.EqualTo("Manara Erotic Oracle: Chakra, Eros And Astrology - Milo Manara"));
    }

    [Test]
    public void ManaraEroticOracleChakraErosAndA2_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void ReviewUri2_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(2) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void AdaugaInCos_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void Button30_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void A31_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void PasatorDeRosiiHomeHghd1200ppCompat_TextMatchesSnapshot()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)"));
        Assert.That(element.Text, Is.EqualTo("Pasator de rosii HOME HGHD1200PP, compatibil cu masina de tocat carne HOME HGHD1200"));
    }

    [Test]
    public void PasatorDeRosiiHomeHghd1200ppCompat2_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void AdaugaInCos2_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void Button35_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void SmartDeals2_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void VoalDeMireasaQttvbtna3x15mPolies_TextMatchesSnapshot()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)"));
        Assert.That(element.Text, Is.EqualTo("Voal de mireasa QttvbTna, 3x1.5m, poliester, alb"));
    }

    [Test]
    public void VoalDeMireasaQttvbtna3x15mPolies2_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void AdaugaInCos3_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void Button40_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void A41_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void SetDeWalkieTalkiesPentruCopii3Bu_TextMatchesSnapshot()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)"));
        Assert.That(element.Text, Is.EqualTo("Set de Walkie Talkies pentru copii, 3 bucati, raza de 3 mile, cu lanterne cu iluminare de fundal"));
    }

    [Test]
    public void SetDeWalkieTalkiesPentruCopii3Bu2_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void AdaugaInCos4_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void Button45_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(5) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void OfertaZilei2_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(5) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void FolieDeVinilPentruSpateCompatibilC_TextMatchesSnapshot()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(5) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)"));
        Assert.That(element.Text, Is.EqualTo("Folie de vinil pentru spate compatibil cu Vivo Y71T, rezistent la zgarieturi, Marmura alba"));
    }

    [Test]
    public void FolieDeVinilPentruSpateCompatibilC2_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(5) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void AdaugaInCos5_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(5) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void Button50_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(6) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void SuperPret_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(6) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void PumaPantofiSportFlatformDePieleM_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(6) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void PumaPantofiSportFlatformDePieleM2_TextMatchesSnapshot()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(6) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1) > h2:nth-child(1)"));
        Assert.That(element.Text, Is.EqualTo("Puma - Pantofi sport flatform de piele Mayze Classic, Alb, 7"));
    }

    [Test]
    public void AdaugaInCos6_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(6) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void Button55_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(7) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void A56_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(7) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void OemGeantaDamaMicaNodPapionAlba_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(7) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void OemGeantaDamaMicaNodPapionAlba2_TextMatchesSnapshot()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(7) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1) > h2:nth-child(1)"));
        Assert.That(element.Text, Is.EqualTo("OEM - Geanta dama mica, nod papion, albastru, 13x5x9cm"));
    }

    [Test]
    public void AdaugaInCos7_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(7) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void Button60_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void A61_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void OukitelC62688128gb4gb5150mahMov_TextMatchesSnapshot()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)"));
        Assert.That(element.Text, Is.EqualTo("Oukitel C62 6.88\" 128GB/4GB 5150mAh Mov"));
    }

    [Test]
    public void OukitelC62688128gb4gb5150mahMov2_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void AdaugaInCos8_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void Button65_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(9) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void A66_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(9) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void HusaBestcaseSlimGlassyCompatibila_TextMatchesSnapshot()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(9) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)"));
        Assert.That(element.Text, Is.EqualTo("Husa BestCase® Slim Glassy, Compatibila Cu Xiaomi Redmi Note 12 5G, White Orchid, HD Colors, Protectie camera, Margini ridicate pentru protectia ecranului, G 1402"));
    }

    [Test]
    public void HusaBestcaseSlimGlassyCompatibila2_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(9) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void AdaugaInCos9_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(9) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void Button70_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(10) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void MultiDeals_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(10) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void UnitateInternAerCondiIonatTipCase_TextMatchesSnapshot()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(10) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)"));
        Assert.That(element.Text, Is.EqualTo("Unitate internă aer condiționat tip casetă Bosch CL5000L 55000 Btu/h, 4C, 160"));
    }

    [Test]
    public void UnitateInternAerCondiIonatTipCase2_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(10) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void AdaugaInCos10_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(10) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void Button75_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(11) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void A76_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(11) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void FiltruUleiVolvoFhIi540Din2012Kw_TextMatchesSnapshot()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(11) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)"));
        Assert.That(element.Text, Is.EqualTo("Filtru ulei VOLVO FH II 540 din 2012 KW 397 CP 540 CMC 12777, auto, Bosch, 4x Stickere auto cu VOLVO"));
    }

    [Test]
    public void FiltruUleiVolvoFhIi540Din2012Kw2_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(11) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void AdaugaInCos11_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(11) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void Button80_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(12) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void A81_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(12) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void AspiratorRobotInteligentNegru7090_TextMatchesSnapshot()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(12) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)"));
        Assert.That(element.Text, Is.EqualTo("Aspirator robot inteligent, negru, 70-90m², incarcare rapida"));
    }

    [Test]
    public void AspiratorRobotInteligentNegru70902_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(12) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void AdaugaInCos12_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(12) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void Button85_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(13) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void A86_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(13) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void TelevizorQledSmartHisense50e7qUltr_TextMatchesSnapshot()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(13) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)"));
        Assert.That(element.Text, Is.EqualTo("Televizor QLED Smart Hisense 50E7Q, Ultra HD 4k, 50\", 126cm, Negru"));
    }

    [Test]
    public void TelevizorQledSmartHisense50e7qUltr2_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(13) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void AdaugaInCos13_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(13) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void Button90_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(14) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void SuperPret2_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(14) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void LaptopAppleMacbookPro14CuProcesor_TextMatchesSnapshot()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(14) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)"));
        Assert.That(element.Text, Is.EqualTo("Laptop Apple MacBook Pro 14\" cu procesor Apple M5, 10 nuclee CPU si 10 nuclee GPU, 16GB RAM, 1TB SSD, Space Black, Tastatura Internationala, Manual RO"));
    }

    [Test]
    public void LaptopAppleMacbookPro14CuProcesor2_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(14) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void DeReviewUri21_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(14) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(2) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void AdaugaInCos14_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(14) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void Button96_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(15) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void MultiDeals2_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(15) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void ApaDeParfumLuxeromBlueIcon100ml_TextMatchesSnapshot()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(15) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)"));
        Assert.That(element.Text, Is.EqualTo("Apa de parfum Luxerom Blue Icon 100ml + dozator parfum reincarcabil 5ml, aroma complexa, design elegant"));
    }

    [Test]
    public void ApaDeParfumLuxeromBlueIcon100ml2_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(15) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void AdaugaInCos15_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(15) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void Button101_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(16) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void A102_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(16) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void BaldininiPantofiSportWedgeCuGarni_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(16) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void BaldininiPantofiSportWedgeCuGarni2_TextMatchesSnapshot()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(16) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1) > h2:nth-child(1)"));
        Assert.That(element.Text, Is.EqualTo("Baldinini - Pantofi sport wedge cu garnituri de piele intoarsa, Albastru, 43"));
    }

    [Test]
    public void VeziDetalii_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(16) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void Button106_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(17) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void SuperPret3_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(17) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void MaxCoRochieEvazataCuManeciScurte_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(17) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void MaxCoRochieEvazataCuManeciScurte2_TextMatchesSnapshot()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(17) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1) > h2:nth-child(1)"));
        Assert.That(element.Text, Is.EqualTo("Max&Co - Rochie evazata cu maneci scurte, Maro nisip, 42"));
    }

    [Test]
    public void AdaugaInCos16_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(17) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void Button111_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(18) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void SuperPret4_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(18) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void SetDeConstructie353PiesePistolBe_TextMatchesSnapshot()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(18) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)"));
        Assert.That(element.Text, Is.EqualTo("Set de constructie, 353 piese, Pistol Beretta 92"));
    }

    [Test]
    public void SetDeConstructie353PiesePistolBe2_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(18) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void AdaugaInCos17_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(18) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void Button116_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(19) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void CadouSuperPret_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(19) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void SuperTrendsBrataraTennisDinArgint_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(19) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void SuperTrendsBrataraTennisDinArgint2_TextMatchesSnapshot()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(19) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1) > h2:nth-child(1)"));
        Assert.That(element.Text, Is.EqualTo("SUPER TRENDS - Bratara Tennis Din Argint 925 ® Cu Zirconii Clear, Bratara Eleganta Pentru Femei, Bratara Tennis Argint Cu Pietre Stralucitoare Tip Zirconiu, Bijuterie Premium Stil Sparkling Tennis, Bratara Argint Tennis Dama Reglabila, Cadou Luxos"));
    }

    [Test]
    public void AdaugaInCos18_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(19) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void Button121_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(20) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void SuperPret5_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(20) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void AppleWatchSe2025GpsCarcasaStar_TextMatchesSnapshot()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(20) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)"));
        Assert.That(element.Text, Is.EqualTo("Apple Watch SE (2025), GPS, Carcasa Starlight Aluminium 40mm, Starlight Sport Band - S/M"));
    }

    [Test]
    public void AppleWatchSe2025GpsCarcasaStar2_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(20) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void DeReviewUri87_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(20) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(2) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void AdaugaInCos19_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(20) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void AboneazTeLaNewsletterIAflCuPri_TextMatchesSnapshot()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > h3:nth-child(1)"));
        Assert.That(element.Text, Is.EqualTo("Abonează-te la newsletter și află cu prioritate despre reducerile și ofertele exclusive din eMAG"));
    }

    [Test]
    public void Name_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.Name("name"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void Email_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.Name("email"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void MAbonez_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > form:nth-child(3) > div:nth-child(1) > div:nth-child(3) > button:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void IstoriculTauDeNavigare_TextMatchesSnapshot()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > section:nth-child(6) > div:nth-child(1) > div:nth-child(1) > h2:nth-child(1)"));
        Assert.That(element.Text, Is.EqualTo("Istoricul tau de navigare"));
    }

    [Test]
    public void AltiVizitatoriAuFostInteresatiSiDe_TextMatchesSnapshot()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > section:nth-child(6) > div:nth-child(1) > div:nth-child(3) > div:nth-child(1) > h2:nth-child(1)"));
        Assert.That(element.Text, Is.EqualTo("Alti vizitatori au fost interesati si de:"));
    }

    [Test]
    public void StergeIstoriculDeNavigare_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > section:nth-child(6) > div:nth-child(1) > div:nth-child(3) > div:nth-child(1) > a:nth-child(2)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void ServiciiPentruClienti_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void ComenziSiLivrare_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void SuportClienti_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void EmagRo_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void DanteInternational_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void VariantaMobile_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(2)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void A140_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void A141_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(2)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void A142_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(3)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void A143_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(4)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void FlipRo_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void FlipBg_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(2)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void RejoyHu_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(3)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void EmagBg_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(4)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void EmagHu_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(5)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void A149_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(8)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void IntraInCont_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(26) > div:nth-child(1) > div:nth-child(2) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void ContNou_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(26) > div:nth-child(1) > div:nth-child(2) > a:nth-child(2)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void PoliticaDeUtilizareCookieUri_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(29) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > p:nth-child(3) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void AcceptToate_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(29) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void RefuzToate_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(29) > div:nth-child(1) > div:nth-child(2) > button:nth-child(2)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void AdministreazPreferinEle_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(29) > div:nth-child(1) > div:nth-child(2) > button:nth-child(3)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void VeziOferta_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(33) > div:nth-child(1) > a:nth-child(2)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void PrecomandAcumNoileSamsungGalaxyVez_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(33) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }
}
