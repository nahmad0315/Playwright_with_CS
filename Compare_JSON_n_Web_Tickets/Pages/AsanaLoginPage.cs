using System;
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
        private ILocator CreateButton => _page.Locator("span.OmnibuttonButtonCard-label:has-text(\"Create\")");

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

            await CreateButton.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 20000
            });
        }

        public async Task<bool> IsLoggedInAsync(int timeout = 10000)
        {
            try
            {
                await CreateButton.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = timeout
                });

                return await CreateButton.IsVisibleAsync();
            }
            catch
            {
                return false;
            }
        }
    }
}
