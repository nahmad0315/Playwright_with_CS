using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;

namespace Compare_JSON_n_Web_Tickets.Base
{
    public abstract class BaseTest
    {
        protected IPlaywright? _playwright;
        protected IBrowser? _browser;
        protected IBrowserContext? _context;
        protected IPage? _page;

        // Read-only protected property to expose the initialized page safely to tests and page objects
        protected IPage Page => _page ?? throw new InvalidOperationException("Page is not initialized. Ensure Setup() completed successfully.");

        [SetUp]
        public async Task Setup()
        {
            var headlessEnv = Environment.GetEnvironmentVariable("PLAYWRIGHT_HEADLESS");
            bool headless = false;
            if (!string.IsNullOrEmpty(headlessEnv))
            {
                headless = headlessEnv.Equals("1", StringComparison.OrdinalIgnoreCase)
                    || headlessEnv.Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            _playwright = await Microsoft.Playwright.Playwright.CreateAsync();

            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = headless
            });

            _context = await _browser.NewContextAsync();
            _page = await _context.NewPageAsync();
        }

        [TearDown]
        public async Task Teardown()
        {
            if (_context is not null)
            {
                await _context.CloseAsync();
            }

            if (_browser is not null)
            {
                await _browser.CloseAsync();
            }

            _playwright?.Dispose();
        }
    }
}
