using FleetManagement.API.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Moq;
using Xunit;
using System.Threading.Tasks;
using System;

namespace FleetManagement.Tests.Controllers
{
    public class AdminControllerTests
    {
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<ILogger<AdminController>> _mockLogger;
        private readonly TelemetryClient _telemetry;
        private readonly AdminController _controller;

        // Mock objects for the configuration sections
        private readonly Mock<IConfigurationSection> _mockConnectionStringsSection;
        private readonly Mock<IConfigurationSection> _mockDefaultConnectionSection;


        public AdminControllerTests()
        {
            _mockConfig = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<AdminController>>();
            _telemetry = new TelemetryClient(new TelemetryConfiguration());

            // 1. Mock the specific section that holds the actual connection string value
            _mockDefaultConnectionSection = new Mock<IConfigurationSection>();
            // This is the actual value returned by GetConnectionString
            _mockDefaultConnectionSection.Setup(s => s.Value).Returns("Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;");

            // 2. Mock the parent "ConnectionStrings" section
            _mockConnectionStringsSection = new Mock<IConfigurationSection>();
            // This setup makes GetSection("ConnectionStrings") return the parent section mock
            _mockConfig.Setup(c => c.GetSection("ConnectionStrings"))
                       .Returns(_mockConnectionStringsSection.Object);

            // 3. Set up the hierarchy: Mock the parent to return the child when asked for "DefaultConnection"
            // This is the crucial step to prevent the NRE inside GetConnectionString
            _mockConnectionStringsSection.Setup(c => c.GetSection("DefaultConnection"))
                                         .Returns(_mockDefaultConnectionSection.Object);

            _controller = new AdminController(_mockConfig.Object, _mockLogger.Object, _telemetry);
        }

        [Fact]
        public void Constructor_InitializesDependencies()
        {
            Assert.NotNull(_controller);
        }

       // [Fact]
        //public async Task GetDatabaseHealth_ReturnsOkResult_WhenDatabaseSucceeds()
        //{
        //    // ARRANGE (Setup is in the constructor)

        //    // ACT
        //    var result = await _controller.GetDatabaseHealth();

        //    // ASSERT
        //    //var okResult = Assert.IsType<OkObjectResult>(result);
        //    //var data = Assert.IsType<System.Collections.Generic.Dictionary<string, object>>(okResult.Value);

        //    // NEW LINES:
        //    // Assert the result is an ObjectResult (which covers OkObjectResult and StatusCode results)
        //    var objectResult = Assert.IsType<ObjectResult>(result);

        //    // Assert the status code is 200 (OK)
        //    Assert.Equal(200, objectResult.StatusCode);

        //    // Then assert the value type
        //    var data = Assert.IsType<System.Collections.Generic.Dictionary<string, object>>(objectResult.Value);

        //    Assert.True(data.ContainsKey("CurrentlyExecuting"));
        //    Assert.True(data.ContainsKey("DatabaseSize"));
        //    Assert.True(data.ContainsKey("ActiveConnections"));
        //    // ... (Add your telemetry and logging verifications here)
        //}


        [Fact]
        public async Task GetDatabaseHealth_Returns500_WhenConnectionFails()
        {
            // ARRANGE

            // Re-setup the innermost mock to return a bad connection string
            _mockDefaultConnectionSection.Setup(s => s.Value).Returns("InvalidConnectionStringThatFails");

            // ACT
            var result = await _controller.GetDatabaseHealth();

            // ASSERT
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);

            // Verify LogError was called
            _mockLogger.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }
    }
}







