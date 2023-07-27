using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Tourism.DataAccess;
using Tourism.Models;

namespace Tourism.FeatureTests
{
    public class AssessmentTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public AssessmentTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        private TourismContext GetDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<TourismContext>();
            optionsBuilder.UseInMemoryDatabase("TestDatabase");

            var context = new TourismContext(optionsBuilder.Options);
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            return context;
        }

        // UPDATE Tests
        [Fact]
        public async Task Edit_ReturnsFormViewPrePopulated()
        {
            // Arrange
            var context = GetDbContext();
            var client = _factory.CreateClient();

            State california = new State { Name = "California", Abbreviation = "CA", TimeZone = "Pacific" };
            State ohio = new State { Name = "Ohio", Abbreviation = "OH", TimeZone = "Eastern" };
            context.States.Add(california);
            context.States.Add(ohio);
            context.SaveChanges();

            // Act
            var response = await client.GetAsync($"/states/{california.Id}/edit");
            var html = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("Edit State", html);
            Assert.Contains(california.Name, html);
            Assert.Contains(california.Abbreviation, html);
            Assert.Contains(california.TimeZone, html);
        }

        [Fact]
        public async Task Update_SavesChangesToState()
        {
            // Arrange
            var context = GetDbContext();
            var client = _factory.CreateClient();

            State california = new State { Name = "California", Abbreviation = "CA", TimeZone = "Pacific" };
            State ohio = new State { Name = "Ohio", Abbreviation = "OH", TimeZone = "Mountain" };
            context.States.Add(california);
            context.States.Add(ohio);
            context.SaveChanges();

            var formData = new Dictionary<string, string>
            {
                { "Name", "Ohio" },
                { "Abbreviation", "OH" },
                { "TimeZone", "Eastern" }
            };

            // Act
            var response = await client.PostAsync($"/states/{ohio.Id}", new FormUrlEncodedContent(formData));
            var html = await response.Content.ReadAsStringAsync();

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Contains("Ohio", html);
            Assert.Contains("OH", html);
            Assert.DoesNotContain("Eastern", html);
        }

        // FILTER Tests
        [Fact]
        public async Task Index_ContainsFilterForEachTimeZone()
        {
            var context = GetDbContext();
            State california = new State { Name = "California", Abbreviation = "CA", TimeZone = "Pacific" };
            //State wyoming = new State { Name = "Wyoming", Abbreviation = "WY", TimeZone = "Mountain" };
            State illinois = new State { Name = "Illinois", Abbreviation = "IL", TimeZone = "Central" };
            State ohio = new State { Name = "Ohio", Abbreviation = "OH", TimeZone = "Eastern" };
            context.States.Add(california);
            //context.States.Add(wyoming);
            context.States.Add(illinois);
            context.States.Add(ohio);
            context.SaveChanges();

            var client = _factory.CreateClient();
            var response = await client.GetAsync("/States");
            var html = await response.Content.ReadAsStringAsync();

            Assert.Contains("<a href=\"/states?time_zone=Pacific\">Pacific</a>", html);
            //Assert.Contains("<a href=\"/states?time_zone=Mountain\">Mountain</a>", html);
            Assert.Contains("<a href=\"/states?time_zone=Central\">Central</a>", html);
            Assert.Contains("<a href=\"/states?time_zone=Eastern\">Eastern</a>", html);
            Assert.Contains("<a href=\"/states\">All States</a>", html);
            Assert.DoesNotContain("<a href=\"/states?time_zone=Mountain\">Mountain</a>", html);
            Assert.DoesNotContain("<a href=\"/states?name=California\">California</a>", html);
        }

        [Fact]
        public async Task Index_UsesTimeZoneQueryStringParamToFilter()
        {
            var context = GetDbContext();
            State california = new State { Name = "California", Abbreviation = "CA", TimeZone = "Pacific" };
            //State wyoming = new State { Name = "Wyoming", Abbreviation = "WY", TimeZone = "Mountain" };
            State illinois = new State { Name = "Illinois", Abbreviation = "IL", TimeZone = "Central" };
            State ohio = new State { Name = "Ohio", Abbreviation = "OH", TimeZone = "Eastern" };
            context.States.Add(california);
            //context.States.Add(wyoming);
            context.States.Add(illinois);
            context.States.Add(ohio);
            context.SaveChanges();

            var client = _factory.CreateClient();
            var response = await client.GetAsync("/states?time_zone=Pacific");
            var html = await response.Content.ReadAsStringAsync();

            Assert.Contains("California", html);
            Assert.DoesNotContain("Illinois", html);
            Assert.DoesNotContain("Ohio", html);
        }

        // DELETE tests
        [Fact]
        public async Task Delete_RemovesStateFromIndexPage()
        {
            // Arrange
            var context = GetDbContext();
            var client = _factory.CreateClient();

            State california = new State { Name = "California", Abbreviation = "CA", TimeZone = "Pacific" };
            State ohio = new State { Name = "Ohio", Abbreviation = "OH", TimeZone = "Mountain" };
            context.States.Add(california);
            context.States.Add(ohio);
            context.SaveChanges();

            // Act
            var response = await client.PostAsync($"/states/delete/{ohio.Id}", null);
            var html = await response.Content.ReadAsStringAsync();

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.DoesNotContain("Ohio", html);
        }

        [Fact]
        public async Task Delete_OnlyDeletesOneState()
        {
            // Arrange
            var context = GetDbContext();
            var client = _factory.CreateClient();

            State california = new State { Name = "California", Abbreviation = "CA", TimeZone = "Pacific" };
            State ohio = new State { Name = "Ohio", Abbreviation = "OH", TimeZone = "Mountain" };
            context.States.Add(california);
            context.States.Add(ohio);
            context.SaveChanges();

            // Act
            var response = await client.PostAsync($"/states/delete/{ohio.Id}", null);
            var html = await response.Content.ReadAsStringAsync();

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Contains("California", html);
        }

        [Fact]
        public async Task Delete_RemovesAllDeletedCitiesFromState()
        {
            // Arrange
            var context = GetDbContext();
            var client = _factory.CreateClient();

            State california = new State { Name = "California", Abbreviation = "CA", TimeZone = "Pacific" };
            City losAngeles = new City { Name = "Los Angeles", State = california };
            City sacramento = new City { Name = "Sacramento", State = california };

            california.Cities.Add(losAngeles);
            california.Cities.Add(sacramento);
            context.States.Add(california);

            context.SaveChanges();

            // Act
            var response = await client.PostAsync($"/states/delete/{california.Id}", null);

            // Assert
            var savedState = context.Cities.FirstOrDefault(c => c.State == california);
            Assert.Null(savedState);
        }
    }
}
