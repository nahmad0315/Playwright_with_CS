using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace Compare_JSON_n_Web_Tickets.Pages
{
    public sealed class AsanaLoginPage
    {
        private readonly IPage _page;
        private const string Url = "https://app.asana.com/-/login";

        private ILocator UsernameInput => _page.Locator("input[type=\"email\"][name=\"e\"]");
        private ILocator ContinueButton => _page.Locator("div.LoginEmailForm-continueButton[role=\"button\"]:has-text(\"Continue\")");
        private ILocator PasswordInput => _page.Locator("input[type=\"password\"][name=\"p\"]#lui_7");
        private ILocator LoginButton => _page.Locator("div.LoginPasswordForm-loginButton[role=\"button\"]:has-text(\"Log in\")");
        // More specific create label selector (reduces accidental matches)
        private ILocator CreateLabel => _page.Locator("span.OmnibuttonButtonCard-label:has-text(\"Create\")");

        public AsanaLoginPage(IPage page)
        {
            _page = page ?? throw new ArgumentNullException(nameof(page));
        }

        public Task NavigateAsync() => _page.GotoAsync(Url);

        public async Task LoginAsync(string username, string password)
        {
            if (username is null) throw new ArgumentNullException(nameof(username));
            if (password is null) throw new ArgumentNullException(nameof(password));

            await UsernameInput.FillAsync(username);
            await ContinueButton.ClickAsync();

            await PasswordInput.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 10000
            });

            await PasswordInput.FillAsync(password);
            await LoginButton.ClickAsync();

            // Wait for network idle, then wait for a single visible Create label (use .First to avoid strict-mode)
            try
            {
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 30000 });
            }
            catch
            {
                // continue to check for Create label even if network idle times out
            }

            try
            {
                // use .First to avoid strict-mode when multiple matches exist in the DOM
                await CreateLabel.First.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 60000
                });
            }
            catch (TimeoutException)
            {
                // Save screenshot and page HTML to help debugging
                try
                {
                    Directory.CreateDirectory("TestArtifacts");
                    var screenshotPath = Path.Combine("TestArtifacts", "login-timeout.png");
                    var htmlPath = Path.Combine("TestArtifacts", "login-timeout.html");
                    await _page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath, FullPage = true });
                    var html = await _page.ContentAsync();
                    await File.WriteAllTextAsync(htmlPath, html);
                }
                catch
                {
                    // ignore any artifact write errors
                }

                throw new TimeoutException("Timed out waiting for post-login 'Create' indicator. " +
                    "Saved artifacts under TestArtifacts/ if available. Check if login succeeded, page structure changed, or additional approval/MFA is required.");
            }
        }

        public async Task<bool> IsLoggedInAsync(int timeout = 10000)
        {
            try
            {
                // Wait for the first matching Create label to be visible
                await CreateLabel.First.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = timeout
                });

                return await CreateLabel.First.IsVisibleAsync();
            }
            catch
            {
                return false;
            }
        }
    }
}
