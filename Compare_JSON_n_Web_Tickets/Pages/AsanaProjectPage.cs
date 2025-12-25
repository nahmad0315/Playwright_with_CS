using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Compare_JSON_n_Web_Tickets.Models;

namespace Compare_JSON_n_Web_Tickets.Pages
{
    public sealed class AsanaProjectPage
    {
        private readonly IPage _page;

        public AsanaProjectPage(IPage page)
        {
            _page = page ?? throw new ArgumentNullException(nameof(page));
        }

        public async Task NavigateToProjectByNameAsync(string projectName)
        {
            if (string.IsNullOrEmpty(projectName)) throw new ArgumentNullException(nameof(projectName));

            // Prefer anchor that contains a span with the project name (matches the supplied markup)
            // find project anchor in sidebar
            var locatorBySpan = _page.Locator($"a:has(span:has-text(\"{projectName}\"))");
            ILocator projectLocator;
            if (await locatorBySpan.CountAsync() > 0)
            {
                projectLocator = locatorBySpan.First;
            }
            else
            {
                var locatorByAria = _page.Locator($"a[aria-label=\"{projectName}, Project\"]");
                if (await locatorByAria.CountAsync() > 0)
                    projectLocator = locatorByAria.First;
                else
                    throw new InvalidOperationException($"Project '{projectName}' not found in the sidebar.");
            }

            // this performs the click
            await projectLocator.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
            await projectLocator.ClickAsync();

            // then wait for the project board to load
            await _page.WaitForSelectorAsync("text=To Do", new PageWaitForSelectorOptions { Timeout = 15000 });
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        // New: return all visible tickets from the currently open project page
        public async Task<List<TicketModel>> GetAllTicketsAsync()
        {
            // Evaluate in page context to collect cards, title and tag texts.
            // This is intentionally generic to work across small DOM differences.
            var items = await _page.EvaluateAsync<dynamic[]>(
                @"() => {
                    // Identify candidate card elements; common anchors: role=listitem or typical board card classes
                    const cardSelectors = [
                        '[role=""listitem""]',
                        '.BoardCard',
                        '.TaskCard',
                        '.ThemeableCardPresentation',
                        '.TaskRow',
                        '.ItemRow'
                    ];
                    let nodes = [];
                    for (const s of cardSelectors) {
                        nodes = nodes.concat(Array.from(document.querySelectorAll(s)));
                    }
                    // deduplicate nodes while preserving order
                    nodes = Array.from(new Set(nodes));

                    const results = [];
                    for (const card of nodes) {
                        try {
                            // gather text candidates inside the card
                            const candidates = Array.from(card.querySelectorAll('h1,h2,h3,h4,span,div,button,a'));
                            const texts = candidates
                                .map(n => n.textContent && n.textContent.trim())
                                .filter(t => t && t.length > 0);
                            if (!texts.length) continue;

                            // choose title as the longest text (heuristic)
                            let title = texts.reduce((a, b) => (a.length >= b.length ? a : b), '');
                            title = title.trim();
                            if (!title) continue;

                            // tags: short texts different from title and short length (<= 40)
                            const tags = texts
                                .filter(t => t !== title && t.length <= 40)
                                .map(t => t.trim())
                                .filter(t => t.length > 0);

                            const uniqueTags = Array.from(new Set(tags));
                            results.push({ Name: title, Tags: uniqueTags });
                        } catch (e) {
                            // ignore single-card errors
                        }
                    }
                    return results;
                }").ConfigureAwait(false);

            // Map dynamic results to TicketModel
            var list = new List<TicketModel>();
            foreach (var item in items)
            {
                try
                {
                    string name = item.Name;
                    List<string> tags = new List<string>();
                    if (item.Tags is object)
                    {
                        foreach (var t in item.Tags)
                        {
                            if (t is string s && !string.IsNullOrWhiteSpace(s))
                            {
                                tags.Add(s.Trim());
                            }
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        list.Add(new TicketModel
                        {
                            Name = name.Trim(),
                            Tags = tags.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
                        });
                    }
                }
                catch
                {                
                    // ignore single mapping errors
                }
            }

            return list;
        }

        // Existing method kept (optional) for targeted extraction by expected tickets
        public async Task<Dictionary<string, List<string>>> GetTagsForTicketsAsync(IEnumerable<TicketModel> tickets)
        {
            var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var ticket in tickets)
            {
                if (string.IsNullOrEmpty(ticket.Name))
                {
                    continue;
                }

                // Use exact text match to find the task title element
                var titleLocator = _page.GetByText(ticket.Name, new PageGetByTextOptions { Exact = true });

                var found = await WaitForTitleAsync(titleLocator);
                if (!found)
                {
                    result[ticket.Name] = new List<string>();
                    continue;
                }

                var handle = await titleLocator.First.ElementHandleAsync();
                if (handle is null)
                {
                    result[ticket.Name] = new List<string>();
                    continue;
                }

                // Evaluate within the task card to collect label texts (tags)
                var tags = await _page.EvaluateAsync<string[]>(
                    @"(el) => {
                        const card = el.closest('[role=""listitem""]') || el.closest('div');
                        if (!card) return [];
                        const candidates = Array.from(card.querySelectorAll('span, div, button, a'));
                        const texts = candidates
                            .map(n => n.textContent && n.textContent.trim())
                            .filter(t => t && t.length > 0 && t.length <= 40);
                        return Array.from(new Set(texts));
                    }", handle).ConfigureAwait(false);

                var cleaned = (tags ?? Array.Empty<string>())
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrEmpty(t))
                    .ToList();

                result[ticket.Name] = cleaned;
            }

            return result;
        }

        private async Task<bool> WaitForTitleAsync(ILocator titleLocator, int maxAttempts = 6)
        {
            for (var i = 0; i < maxAttempts; i++)
            {
                try
                {
                    if (await titleLocator.IsVisibleAsync().ConfigureAwait(false))
                    {
                        return true;
                    }

                    await titleLocator.First.WaitForAsync(new LocatorWaitForOptions { Timeout = 2000 }).ConfigureAwait(false);
                    if (await titleLocator.IsVisibleAsync().ConfigureAwait(false)) return true;
                }
                catch
                {
                    // ignore and attempt to scroll for lazy-loaded cards
                }

                await _page.EvaluateAsync("() => window.scrollBy(0, window.innerHeight / 1.5)").ConfigureAwait(false);
                await Task.Delay(500).ConfigureAwait(false);
            }

            return false;           
        }
    }
}