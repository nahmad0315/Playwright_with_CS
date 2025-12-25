using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Compare_JSON_n_Web_Tickets.Base;
using Compare_JSON_n_Web_Tickets.Models;
using Compare_JSON_n_Web_Tickets.Pages;
using Compare_JSON_n_Web_Tickets.Services;
using NUnit.Framework;

namespace Compare_JSON_n_Web_Tickets.Tests
{
    public class CompareTicketsTests : BaseTest
    {
        [Test]
        public async Task JsonTicketsShouldMatchAsanaProject()
        {
            // Resolve JSON path from env or common runtime locations
            var envPath = Environment.GetEnvironmentVariable("TICKETS_JSON_PATH");
            var candidates = new[]
            {
                envPath,
                Path.Combine(AppContext.BaseDirectory, "Data", "tickets.json"),           // when copied to output
                Path.Combine(TestContext.CurrentContext.WorkDirectory, "Data", "tickets.json"), // test discovery/work dir
                Path.Combine(Environment.CurrentDirectory, "Data", "tickets.json")       // current working dir
            }.Where(p => !string.IsNullOrEmpty(p)).ToList();

            var jsonPath = candidates.FirstOrDefault(File.Exists);

            if (string.IsNullOrEmpty(jsonPath))
            {
                Assert.Fail($"Tickets JSON not found. Checked locations:{Environment.NewLine}{string.Join(Environment.NewLine, candidates)}");
            }

            var jsonText = await File.ReadAllTextAsync(jsonPath);
            var ticketFile = JsonSerializer.Deserialize<TicketFile>(jsonText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (ticketFile is null || ticketFile.Project is null || ticketFile.Tickets is null)
            {
                Assert.Fail("Invalid tickets JSON format.");
            }

            var username = Environment.GetEnvironmentVariable("ASANA_USERNAME");
            var password = Environment.GetEnvironmentVariable("ASANA_PASSWORD");
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Assert.Fail("Set ASANA_USERNAME and ASANA_PASSWORD environment variables for the test.");
            }

            // Act - login (use the protected Page property from BaseTest)
            var loginPage = new AsanaLoginPage(Page);
            await loginPage.NavigateAsync();
            await loginPage.LoginAsync(username, password);

            // Act - navigate to project and extract tickets/tags from the site as a list
            var projectPage = new AsanaProjectPage(Page);
            await projectPage.NavigateToProjectByNameAsync(ticketFile.Project);

            var actualTicketList = await projectPage.GetAllTicketsAsync();

            // expected tickets loaded from JSON are already a list: ticketFile.Tickets
            var expectedTicketList = ticketFile.Tickets;

            // For the existing comparer we convert actual list to name->tags dictionary
            var actualTagsMap = actualTicketList
                .Where(t => !string.IsNullOrEmpty(t.Name))
                .ToDictionary(t => t.Name!, t => t.Tags ?? new System.Collections.Generic.List<string>(), StringComparer.OrdinalIgnoreCase);

            // Assert - exact equality of tags
            var comparison = TicketComparer.CompareExact(expectedTicketList, actualTagsMap);
            if (comparison.HasFailures)
            {
                Assert.Fail(comparison.FormatReport());
            }

            Assert.Pass("All tickets and tags match exactly.");
        }
    }
}