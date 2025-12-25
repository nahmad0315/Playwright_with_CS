using System;
using NUnit.Framework;
using Compare_JSON_n_Web_Tickets.Base;
using Compare_JSON_n_Web_Tickets.Pages;

namespace Compare_JSON_n_Web_Tickets.Tests
{
    public class AsanaLoginTests : BaseTest
    {
        private AsanaLoginPage? _loginPage;

        [SetUp]
        public void TestSetup()
        {
            if (Page is null)
                Assert.Fail("Page not initialized.");

            _loginPage = new AsanaLoginPage(Page);
        }

        [Test]
        public async Task LoginTest()
        {
            if (_loginPage is null)
                Assert.Fail("Login page not initialized.");

            var username = Environment.GetEnvironmentVariable("ASANA_USER") ?? "jajex50119@arugy.com";
            var password = Environment.GetEnvironmentVariable("ASANA_PASS") ?? "testuser12345*";

            await _loginPage.NavigateAsync();
            await _loginPage.LoginAsync(username, password);

            Assert.That(
                await _loginPage.IsLoggedInAsync(),
                "Expected to be logged in and see the Create button."
            );
        }
    }
}
