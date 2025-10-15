using FleetManagement.API.Controllers;
using FleetManagement.API.Data;
using FleetManagement.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights;
using Moq;

namespace FleetManagement.Tests.Controllers
{
    public class VehiclesControllerTests
    {
        private FleetContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<FleetContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new FleetContext(options);
        }

        private VehiclesController CreateController(FleetContext context)
        {
            // Use real TelemetryClient instead of Moq (Moq can't mock sealed classes)
            var mockLogger = new Mock<ILogger<VehiclesController>>();
            var telemetry = new TelemetryClient();

            return new VehiclesController(context, mockLogger.Object, telemetry);
        }

        [Fact]
        public async Task GetVehicles_ReturnsAllVehicles_HappyPath()
        {
            var context = GetInMemoryContext();
            context.Vehicles.AddRange(
                new Vehicle { VIN = "12345678901234567", Make = "Ford", Model = "F-150", Year = 2020, Mileage = 50000, Status = "Active" },
                new Vehicle { VIN = "23456789012345678", Make = "Chevy", Model = "Silverado", Year = 2021, Mileage = 30000, Status = "Active" }
            );
            await context.SaveChangesAsync();

            var controller = CreateController(context);

            var result = await controller.GetVehicles();

            var actionResult = Assert.IsType<ActionResult<IEnumerable<Vehicle>>>(result);
            var vehicles = Assert.IsAssignableFrom<IEnumerable<Vehicle>>(actionResult.Value);
            Assert.Equal(2, vehicles.Count());
        }

        [Fact]
        public async Task GetVehicle_ReturnsVehicle_WhenVehicleExists()
        {
            var context = GetInMemoryContext();
            var vehicle = new Vehicle
            {
                VIN = "12345678901234567",
                Make = "Ford",
                Model = "F-150",
                Year = 2020,
                Mileage = 50000,
                Status = "Active"
            };
            context.Vehicles.Add(vehicle);
            await context.SaveChangesAsync();

            var controller = CreateController(context);

            var result = await controller.GetVehicle(vehicle.Id);

            var actionResult = Assert.IsType<ActionResult<Vehicle>>(result);
            var returnedVehicle = Assert.IsType<Vehicle>(actionResult.Value);
            Assert.Equal("Ford", returnedVehicle.Make);
            Assert.Equal("F-150", returnedVehicle.Model);
        }

        [Fact]
        public async Task GetVehicle_ReturnsNotFound_WhenVehicleDoesNotExist()
        {
            var context = GetInMemoryContext();
            var controller = CreateController(context);

            var result = await controller.GetVehicle(999);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task PostVehicle_CreatesNewVehicle_HappyPath()
        {
            var context = GetInMemoryContext();
            var controller = CreateController(context);

            var newVehicle = new Vehicle
            {
                VIN = "98765432109876543",
                Make = "Toyota",
                Model = "Tacoma",
                Year = 2022,
                Mileage = 10000,
                Status = "Active"
            };

            var result = await controller.PostVehicle(newVehicle);

            var actionResult = Assert.IsType<ActionResult<Vehicle>>(result);
            var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
            var vehicle = Assert.IsType<Vehicle>(createdResult.Value);
            Assert.Equal("Toyota", vehicle.Make);
            Assert.True(vehicle.Id > 0);
        }

        [Fact]
        public async Task PutVehicle_UpdatesVehicle_HappyPath()
        {
            var context = GetInMemoryContext();
            var vehicle = new Vehicle
            {
                VIN = "12345678901234567",
                Make = "Ford",
                Model = "F-150",
                Year = 2020,
                Mileage = 50000,
                Status = "Active"
            };
            context.Vehicles.Add(vehicle);
            await context.SaveChangesAsync();

            var controller = CreateController(context);

            vehicle.Mileage = 60000;
            vehicle.Status = "Maintenance";
            var result = await controller.PutVehicle(vehicle.Id, vehicle);

            Assert.IsType<NoContentResult>(result);

            var updatedVehicle = await context.Vehicles.FindAsync(vehicle.Id);
            Assert.Equal(60000, updatedVehicle.Mileage);
            Assert.Equal("Maintenance", updatedVehicle.Status);
        }

        [Fact]
        public async Task PutVehicle_ReturnsBadRequest_WhenIdMismatch()
        {
            var context = GetInMemoryContext();
            var controller = CreateController(context);

            var vehicle = new Vehicle { Id = 1, VIN = "12345678901234567", Make = "Ford", Model = "F-150", Year = 2020, Mileage = 50000 };

            var result = await controller.PutVehicle(999, vehicle);

            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task DeleteVehicle_RemovesVehicle_HappyPath()
        {
            var context = GetInMemoryContext();
            var vehicle = new Vehicle
            {
                VIN = "12345678901234567",
                Make = "Ford",
                Model = "F-150",
                Year = 2020,
                Mileage = 50000,
                Status = "Active"
            };
            context.Vehicles.Add(vehicle);
            await context.SaveChangesAsync();

            var controller = CreateController(context);

            var result = await controller.DeleteVehicle(vehicle.Id);

            Assert.IsType<NoContentResult>(result);
            Assert.Null(await context.Vehicles.FindAsync(vehicle.Id));
        }

        [Fact]
        public async Task DeleteVehicle_ReturnsNotFound_WhenVehicleDoesNotExist()
        {
            var context = GetInMemoryContext();
            var controller = CreateController(context);

            var result = await controller.DeleteVehicle(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetVehiclesSlow_TakesAtLeast3Seconds()
        {
            var context = GetInMemoryContext();
            var controller = CreateController(context);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await controller.GetVehiclesSlow();
            stopwatch.Stop();

            Assert.True(stopwatch.ElapsedMilliseconds >= 3000, "Slow endpoint should take at least 3 seconds");
        }
    }
}





//using FleetManagement.API.Controllers;
//using FleetManagement.API.Data;
//using FleetManagement.API.Models;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Logging;
//using Microsoft.ApplicationInsights;
//using Moq;

//namespace FleetManagement.Tests.Controllers
//{
//    public class VehiclesControllerTests
//    {
//        private FleetContext GetInMemoryContext()
//        {
//            var options = new DbContextOptionsBuilder<FleetContext>()
//                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
//                .Options;

//            return new FleetContext(options);
//        }

//        private VehiclesController CreateController(FleetContext context)
//        {
//            var mockLogger = new Mock<ILogger<VehiclesController>>();
//            var mockTelemetry = new Mock<TelemetryClient>();
//            return new VehiclesController(context, mockLogger.Object, mockTelemetry.Object);
//        }

//        [Fact]
//        public async Task GetVehicles_ReturnsAllVehicles_HappyPath()
//        {
//            var context = GetInMemoryContext();
//            context.Vehicles.AddRange(
//                new Vehicle { VIN = "12345678901234567", Make = "Ford", Model = "F-150", Year = 2020, Mileage = 50000, Status = "Active" },
//                new Vehicle { VIN = "23456789012345678", Make = "Chevy", Model = "Silverado", Year = 2021, Mileage = 30000, Status = "Active" }
//            );
//            await context.SaveChangesAsync();

//            var controller = CreateController(context);

//            var result = await controller.GetVehicles();

//            var actionResult = Assert.IsType<ActionResult<IEnumerable<Vehicle>>>(result);
//            var vehicles = Assert.IsAssignableFrom<IEnumerable<Vehicle>>(actionResult.Value);
//            Assert.Equal(2, vehicles.Count());
//        }

//        [Fact]
//        public async Task GetVehicle_ReturnsVehicle_WhenVehicleExists()
//        {
//            var context = GetInMemoryContext();
//            var vehicle = new Vehicle
//            {
//                VIN = "12345678901234567",
//                Make = "Ford",
//                Model = "F-150",
//                Year = 2020,
//                Mileage = 50000,
//                Status = "Active"
//            };
//            context.Vehicles.Add(vehicle);
//            await context.SaveChangesAsync();

//            var controller = CreateController(context);

//            var result = await controller.GetVehicle(vehicle.Id);

//            var actionResult = Assert.IsType<ActionResult<Vehicle>>(result);
//            var returnedVehicle = Assert.IsType<Vehicle>(actionResult.Value);
//            Assert.Equal("Ford", returnedVehicle.Make);
//            Assert.Equal("F-150", returnedVehicle.Model);
//        }

//        [Fact]
//        public async Task GetVehicle_ReturnsNotFound_WhenVehicleDoesNotExist()
//        {
//            var context = GetInMemoryContext();
//            var controller = CreateController(context);

//            var result = await controller.GetVehicle(999);

//            Assert.IsType<NotFoundResult>(result.Result);
//        }

//        [Fact]
//        public async Task PostVehicle_CreatesNewVehicle_HappyPath()
//        {
//            var context = GetInMemoryContext();
//            var controller = CreateController(context);

//            var newVehicle = new Vehicle
//            {
//                VIN = "98765432109876543",
//                Make = "Toyota",
//                Model = "Tacoma",
//                Year = 2022,
//                Mileage = 10000,
//                Status = "Active"
//            };

//            var result = await controller.PostVehicle(newVehicle);

//            var actionResult = Assert.IsType<ActionResult<Vehicle>>(result);
//            var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
//            var vehicle = Assert.IsType<Vehicle>(createdResult.Value);
//            Assert.Equal("Toyota", vehicle.Make);
//            Assert.True(vehicle.Id > 0);
//        }

//        [Fact]
//        public async Task PutVehicle_UpdatesVehicle_HappyPath()
//        {
//            var context = GetInMemoryContext();
//            var vehicle = new Vehicle
//            {
//                VIN = "12345678901234567",
//                Make = "Ford",
//                Model = "F-150",
//                Year = 2020,
//                Mileage = 50000,
//                Status = "Active"
//            };
//            context.Vehicles.Add(vehicle);
//            await context.SaveChangesAsync();

//            var controller = CreateController(context);

//            vehicle.Mileage = 60000;
//            vehicle.Status = "Maintenance";
//            var result = await controller.PutVehicle(vehicle.Id, vehicle);

//            Assert.IsType<NoContentResult>(result);

//            var updatedVehicle = await context.Vehicles.FindAsync(vehicle.Id);
//            Assert.Equal(60000, updatedVehicle.Mileage);
//            Assert.Equal("Maintenance", updatedVehicle.Status);
//        }

//        [Fact]
//        public async Task PutVehicle_ReturnsBadRequest_WhenIdMismatch()
//        {
//            var context = GetInMemoryContext();
//            var controller = CreateController(context);

//            var vehicle = new Vehicle { Id = 1, VIN = "12345678901234567", Make = "Ford", Model = "F-150", Year = 2020, Mileage = 50000 };

//            var result = await controller.PutVehicle(999, vehicle);

//            Assert.IsType<BadRequestResult>(result);
//        }

//        [Fact]
//        public async Task DeleteVehicle_RemovesVehicle_HappyPath()
//        {
//            var context = GetInMemoryContext();
//            var vehicle = new Vehicle
//            {
//                VIN = "12345678901234567",
//                Make = "Ford",
//                Model = "F-150",
//                Year = 2020,
//                Mileage = 50000,
//                Status = "Active"
//            };
//            context.Vehicles.Add(vehicle);
//            await context.SaveChangesAsync();

//            var controller = CreateController(context);

//            var result = await controller.DeleteVehicle(vehicle.Id);

//            Assert.IsType<NoContentResult>(result);
//            Assert.Null(await context.Vehicles.FindAsync(vehicle.Id));
//        }

//        [Fact]
//        public async Task DeleteVehicle_ReturnsNotFound_WhenVehicleDoesNotExist()
//        {
//            var context = GetInMemoryContext();
//            var controller = CreateController(context);

//            var result = await controller.DeleteVehicle(999);

//            Assert.IsType<NotFoundResult>(result);
//        }

//        [Fact]
//        public async Task GetVehiclesSlow_TakesAtLeast3Seconds()
//        {
//            var context = GetInMemoryContext();
//            var controller = CreateController(context);

//            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
//            var result = await controller.GetVehiclesSlow();
//            stopwatch.Stop();

//            Assert.True(stopwatch.ElapsedMilliseconds >= 3000, "Slow endpoint should take at least 3 seconds");
//        }
//    }
//}


//using FleetManagement.API.Controllers;
//using FleetManagement.API.Data;
//using FleetManagement.API.Models;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Logging;
//using Moq;

//namespace FleetManagement.Tests.Controllers
//{
//    public class VehiclesControllerTests
//    {
//        private FleetContext GetInMemoryContext()
//        {
//            var options = new DbContextOptionsBuilder<FleetContext>()
//                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
//                .Options;

//            return new FleetContext(options);
//        }

//        [Fact]
//        public async Task GetVehicles_ReturnsAllVehicles_HappyPath()
//        {
//            // Arrange
//            var context = GetInMemoryContext();
//            var mockLogger = new Mock<ILogger<VehiclesController>>();

//            context.Vehicles.AddRange(
//                new Vehicle { VIN = "12345678901234567", Make = "Ford", Model = "F-150", Year = 2020, Mileage = 50000, Status = "Active" },
//                new Vehicle { VIN = "23456789012345678", Make = "Chevy", Model = "Silverado", Year = 2021, Mileage = 30000, Status = "Active" }
//            );
//            await context.SaveChangesAsync();

//            var controller = new VehiclesController(context, mockLogger.Object);

//            // Act
//            var result = await controller.GetVehicles();

//            // Assert
//            var actionResult = Assert.IsType<ActionResult<IEnumerable<Vehicle>>>(result);
//            var vehicles = Assert.IsAssignableFrom<IEnumerable<Vehicle>>(actionResult.Value);
//            Assert.Equal(2, vehicles.Count());
//        }

//        [Fact]
//        public async Task GetVehicle_ReturnsVehicle_WhenVehicleExists()
//        {
//            // Arrange
//            var context = GetInMemoryContext();
//            var mockLogger = new Mock<ILogger<VehiclesController>>();

//            var vehicle = new Vehicle
//            {
//                VIN = "12345678901234567",
//                Make = "Ford",
//                Model = "F-150",
//                Year = 2020,
//                Mileage = 50000,
//                Status = "Active"
//            };
//            context.Vehicles.Add(vehicle);
//            await context.SaveChangesAsync();

//            var controller = new VehiclesController(context, mockLogger.Object);

//            // Act
//            var result = await controller.GetVehicle(vehicle.Id);

//            // Assert
//            var actionResult = Assert.IsType<ActionResult<Vehicle>>(result);
//            var returnedVehicle = Assert.IsType<Vehicle>(actionResult.Value);
//            Assert.Equal("Ford", returnedVehicle.Make);
//            Assert.Equal("F-150", returnedVehicle.Model);
//        }

//        [Fact]
//        public async Task GetVehicle_ReturnsNotFound_WhenVehicleDoesNotExist()
//        {
//            // Arrange
//            var context = GetInMemoryContext();
//            var mockLogger = new Mock<ILogger<VehiclesController>>();
//            var controller = new VehiclesController(context, mockLogger.Object);

//            // Act
//            var result = await controller.GetVehicle(999);

//            // Assert
//            Assert.IsType<NotFoundResult>(result.Result);
//        }

//        [Fact]
//        public async Task PostVehicle_CreatesNewVehicle_HappyPath()
//        {
//            // Arrange
//            var context = GetInMemoryContext();
//            var mockLogger = new Mock<ILogger<VehiclesController>>();
//            var controller = new VehiclesController(context, mockLogger.Object);

//            var newVehicle = new Vehicle
//            {
//                VIN = "98765432109876543",
//                Make = "Toyota",
//                Model = "Tacoma",
//                Year = 2022,
//                Mileage = 10000,
//                Status = "Active"
//            };

//            // Act
//            var result = await controller.PostVehicle(newVehicle);

//            // Assert
//            var actionResult = Assert.IsType<ActionResult<Vehicle>>(result);
//            var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
//            var vehicle = Assert.IsType<Vehicle>(createdResult.Value);
//            Assert.Equal("Toyota", vehicle.Make);
//            Assert.True(vehicle.Id > 0);
//        }

//        [Fact]
//        public async Task PutVehicle_UpdatesVehicle_HappyPath()
//        {
//            // Arrange
//            var context = GetInMemoryContext();
//            var mockLogger = new Mock<ILogger<VehiclesController>>();

//            var vehicle = new Vehicle
//            {
//                VIN = "12345678901234567",
//                Make = "Ford",
//                Model = "F-150",
//                Year = 2020,
//                Mileage = 50000,
//                Status = "Active"
//            };
//            context.Vehicles.Add(vehicle);
//            await context.SaveChangesAsync();

//            var controller = new VehiclesController(context, mockLogger.Object);

//            // Act
//            vehicle.Mileage = 60000;
//            vehicle.Status = "Maintenance";
//            var result = await controller.PutVehicle(vehicle.Id, vehicle);

//            // Assert
//            Assert.IsType<NoContentResult>(result);

//            var updatedVehicle = await context.Vehicles.FindAsync(vehicle.Id);
//            Assert.Equal(60000, updatedVehicle.Mileage);
//            Assert.Equal("Maintenance", updatedVehicle.Status);
//        }

//        [Fact]
//        public async Task PutVehicle_ReturnsBadRequest_WhenIdMismatch()
//        {
//            // Arrange
//            var context = GetInMemoryContext();
//            var mockLogger = new Mock<ILogger<VehiclesController>>();
//            var controller = new VehiclesController(context, mockLogger.Object);

//            var vehicle = new Vehicle { Id = 1, VIN = "12345678901234567", Make = "Ford", Model = "F-150", Year = 2020, Mileage = 50000 };

//            // Act
//            var result = await controller.PutVehicle(999, vehicle);

//            // Assert
//            Assert.IsType<BadRequestResult>(result);
//        }

//        [Fact]
//        public async Task DeleteVehicle_RemovesVehicle_HappyPath()
//        {
//            // Arrange
//            var context = GetInMemoryContext();
//            var mockLogger = new Mock<ILogger<VehiclesController>>();

//            var vehicle = new Vehicle
//            {
//                VIN = "12345678901234567",
//                Make = "Ford",
//                Model = "F-150",
//                Year = 2020,
//                Mileage = 50000,
//                Status = "Active"
//            };
//            context.Vehicles.Add(vehicle);
//            await context.SaveChangesAsync();

//            var controller = new VehiclesController(context, mockLogger.Object);

//            // Act
//            var result = await controller.DeleteVehicle(vehicle.Id);

//            // Assert
//            Assert.IsType<NoContentResult>(result);
//            Assert.Null(await context.Vehicles.FindAsync(vehicle.Id));
//        }

//        [Fact]
//        public async Task DeleteVehicle_ReturnsNotFound_WhenVehicleDoesNotExist()
//        {
//            // Arrange
//            var context = GetInMemoryContext();
//            var mockLogger = new Mock<ILogger<VehiclesController>>();
//            var controller = new VehiclesController(context, mockLogger.Object);

//            // Act
//            var result = await controller.DeleteVehicle(999);

//            // Assert
//            Assert.IsType<NotFoundResult>(result);
//        }

//        [Fact]
//        public async Task GetVehiclesSlow_TakesAtLeast3Seconds()
//        {
//            // Arrange
//            var context = GetInMemoryContext();
//            var mockLogger = new Mock<ILogger<VehiclesController>>();
//            var controller = new VehiclesController(context, mockLogger.Object);

//            // Act
//            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
//            var result = await controller.GetVehiclesSlow();
//            stopwatch.Stop();

//            // Assert
//            Assert.True(stopwatch.ElapsedMilliseconds >= 3000, "Slow endpoint should take at least 3 seconds");
//        }
//    }
//}
