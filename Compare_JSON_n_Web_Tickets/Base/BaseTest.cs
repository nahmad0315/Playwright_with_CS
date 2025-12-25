using Microsoft.Playwright;
using NUnit.Framework;

namespace Compare_JSON_n_Web_Tickets.Base
{
    public abstract class BaseTest
    {
        protected IPlaywright? Playwright;
        protected IBrowser? Browser;
        protected IPage? Page;

        [SetUp]
        public void Setup()
        {
            Playwright = Microsoft.Playwright.Playwright
                .CreateAsync()
                .GetAwaiter()
                .GetResult();

            Browser = Playwright.Chromium
                .LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = false
                })
                .GetAwaiter()
                .GetResult();

            Page = Browser.NewPageAsync()
                .GetAwaiter()
                .GetResult();
        }

        [TearDown]
        public async Task Teardown()
        {
            if (Browser is not null)
            {
                await Browser.CloseAsync();
            }

            Playwright?.Dispose();
        }
    }
}
