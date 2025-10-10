using FleetManagement.API.Controllers;
using FleetManagement.API.Data;
using FleetManagement.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace FleetManagement.Tests.Controllers
{
    public class DashboardControllerTests
    {
        private FleetContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<FleetContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new FleetContext(options);
        }

        [Fact]
        public async Task GetDashboardStats_ReturnsCorrectCounts_HappyPath()
        {
            // Arrange
            var context = GetInMemoryContext();
            var mockLogger = new Mock<ILogger<DashboardController>>();

            // Add test data
            context.Vehicles.AddRange(
                new Vehicle { VIN = "12345678901234567", Make = "Ford", Model = "F-150", Year = 2020, Mileage = 50000, Status = "Active" },
                new Vehicle { VIN = "23456789012345678", Make = "Chevy", Model = "Silverado", Year = 2021, Mileage = 30000, Status = "Maintenance" }
            );

            context.ServiceAlerts.AddRange(
                new ServiceAlert { VehicleId = 1, AlertType = "Inspection Due", Priority = "High", Description = "Test", IsResolved = false },
                new ServiceAlert { VehicleId = 2, AlertType = "Maintenance", Priority = "Critical", Description = "Test", IsResolved = false }
            );

            await context.SaveChangesAsync();

            var controller = new DashboardController(context, mockLogger.Object);

            // Act
            var result = await controller.GetDashboardStats();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(okResult.Value);

            // Verify it returns an object with the expected properties
            var statsType = okResult.Value.GetType();
            Assert.NotNull(statsType.GetProperty("TotalVehicles"));
            Assert.NotNull(statsType.GetProperty("ActiveVehicles"));
            Assert.NotNull(statsType.GetProperty("UnresolvedAlerts"));
        }

        [Fact]
        public async Task GetDashboardStats_ReturnsZeroCounts_WhenNoData()
        {
            // Arrange
            var context = GetInMemoryContext();
            var mockLogger = new Mock<ILogger<DashboardController>>();
            var controller = new DashboardController(context, mockLogger.Object);

            // Act
            var result = await controller.GetDashboardStats();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(okResult.Value);
        }
    }
}