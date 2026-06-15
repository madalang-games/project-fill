using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjectFill.Application.Tutorial;
using ProjectFill.Infrastructure.Generated;
using Xunit;

namespace ProjectFill.API.Tests
{
    public sealed class TutorialServiceTests
    {
        private AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task GetCompletedTutorialIdsReturnsEmptyInitially()
        {
            using var db = CreateDbContext();
            var service = new TutorialService(db);

            var result = await service.GetCompletedTutorialIdsAsync(123, default);
            Assert.Empty(result);
        }

        [Fact]
        public async Task CompleteTutorialAddsIdAndReturnsList()
        {
            using var db = CreateDbContext();
            var service = new TutorialService(db);

            var result = await service.CompleteTutorialAsync(123, 101, default);
            Assert.Single(result);
            Assert.Contains(101, result);

            var result2 = await service.CompleteTutorialAsync(123, 102, default);
            Assert.Equal(2, result2.Count);
            Assert.Contains(101, result2);
            Assert.Contains(102, result2);
        }
    }
}
