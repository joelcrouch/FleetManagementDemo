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
    public class MaintenanceControllerTests
    {
        private FleetContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<FleetContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new FleetContext(options);
        }

        private MaintenanceController CreateController(FleetContext context)
        {
            var mockLogger = new Mock<ILogger<MaintenanceController>>();
            var telemetry = new TelemetryClient(); // ✅ use real instance

            return new MaintenanceController(context, mockLogger.Object, telemetry);
        }

        [Fact]
        public async Task GetMaintenanceRecords_ReturnsAllRecords_HappyPath()
        {
            var context = GetInMemoryContext();

            var vehicle = new Vehicle { VIN = "12345678901234567", Make = "Ford", Model = "F-150", Year = 2020, Mileage = 50000, Status = "Active" };
            context.Vehicles.Add(vehicle);
            await context.SaveChangesAsync();

            context.MaintenanceRecords.AddRange(
                new MaintenanceRecord { VehicleId = vehicle.Id, ServiceDate = DateTime.Now, ServiceType = "Oil Change", Cost = 50.00m, MileageAtService = 50000 },
                new MaintenanceRecord { VehicleId = vehicle.Id, ServiceDate = DateTime.Now.AddDays(-30), ServiceType = "Tire Rotation", Cost = 75.00m, MileageAtService = 48000 }
            );
            await context.SaveChangesAsync();

            var controller = CreateController(context);

            var result = await controller.GetMaintenanceRecords();

            var actionResult = Assert.IsType<ActionResult<IEnumerable<MaintenanceRecord>>>(result);
            var records = Assert.IsAssignableFrom<IEnumerable<MaintenanceRecord>>(actionResult.Value);
            Assert.Equal(2, records.Count());
        }

        [Fact]
        public async Task GetMaintenanceRecords_ReturnsEmpty_WhenNoRecords()
        {
            var context = GetInMemoryContext();
            var controller = CreateController(context);

            var result = await controller.GetMaintenanceRecords();

            var actionResult = Assert.IsType<ActionResult<IEnumerable<MaintenanceRecord>>>(result);
            var records = Assert.IsAssignableFrom<IEnumerable<MaintenanceRecord>>(actionResult.Value);
            Assert.Empty(records);
        }

        [Fact]
        public async Task GetMaintenanceRecord_ReturnsRecord_WhenRecordExists()
        {
            var context = GetInMemoryContext();

            var vehicle = new Vehicle { VIN = "12345678901234567", Make = "Ford", Model = "F-150", Year = 2020, Mileage = 50000, Status = "Active" };
            context.Vehicles.Add(vehicle);
            await context.SaveChangesAsync();

            var record = new MaintenanceRecord
            {
                VehicleId = vehicle.Id,
                ServiceDate = DateTime.Now,
                ServiceType = "Oil Change",
                Cost = 50.00m,
                MileageAtService = 50000,
                PerformedBy = "John Doe"
            };
            context.MaintenanceRecords.Add(record);
            await context.SaveChangesAsync();

            var controller = CreateController(context);

            var result = await controller.GetMaintenanceRecord(record.Id);

            var actionResult = Assert.IsType<ActionResult<MaintenanceRecord>>(result);
            var returnedRecord = Assert.IsType<MaintenanceRecord>(actionResult.Value);
            Assert.Equal("Oil Change", returnedRecord.ServiceType);
            Assert.Equal(50.00m, returnedRecord.Cost);
        }

        [Fact]
        public async Task GetMaintenanceRecord_ReturnsNotFound_WhenRecordDoesNotExist()
        {
            var context = GetInMemoryContext();
            var controller = CreateController(context);

            var result = await controller.GetMaintenanceRecord(999);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetMaintenanceByVehicle_ReturnsRecordsForSpecificVehicle()
        {
            var context = GetInMemoryContext();

            var vehicle1 = new Vehicle { VIN = "12345678901234567", Make = "Ford", Model = "F-150", Year = 2020, Mileage = 50000, Status = "Active" };
            var vehicle2 = new Vehicle { VIN = "23456789012345678", Make = "Chevy", Model = "Silverado", Year = 2021, Mileage = 30000, Status = "Active" };
            context.Vehicles.AddRange(vehicle1, vehicle2);
            await context.SaveChangesAsync();

            context.MaintenanceRecords.AddRange(
                new MaintenanceRecord { VehicleId = vehicle1.Id, ServiceDate = DateTime.Now, ServiceType = "Oil Change", Cost = 50.00m, MileageAtService = 50000 },
                new MaintenanceRecord { VehicleId = vehicle1.Id, ServiceDate = DateTime.Now.AddDays(-30), ServiceType = "Tire Rotation", Cost = 75.00m, MileageAtService = 48000 },
                new MaintenanceRecord { VehicleId = vehicle2.Id, ServiceDate = DateTime.Now, ServiceType = "Brake Service", Cost = 200.00m, MileageAtService = 30000 }
            );
            await context.SaveChangesAsync();

            var controller = CreateController(context);

            var result = await controller.GetMaintenanceByVehicle(vehicle1.Id);

            var actionResult = Assert.IsType<ActionResult<IEnumerable<MaintenanceRecord>>>(result);
            var records = Assert.IsAssignableFrom<IEnumerable<MaintenanceRecord>>(actionResult.Value);
            Assert.Equal(2, records.Count());
            Assert.All(records, r => Assert.Equal(vehicle1.Id, r.VehicleId));
        }

        [Fact]
        public async Task GetMaintenanceByVehicle_ReturnsEmpty_WhenVehicleHasNoMaintenance()
        {
            var context = GetInMemoryContext();

            var vehicle = new Vehicle { VIN = "12345678901234567", Make = "Ford", Model = "F-150", Year = 2020, Mileage = 50000, Status = "Active" };
            context.Vehicles.Add(vehicle);
            await context.SaveChangesAsync();

            var controller = CreateController(context);

            var result = await controller.GetMaintenanceByVehicle(vehicle.Id);

            var actionResult = Assert.IsType<ActionResult<IEnumerable<MaintenanceRecord>>>(result);
            var records = Assert.IsAssignableFrom<IEnumerable<MaintenanceRecord>>(actionResult.Value);
            Assert.Empty(records);
        }

        [Fact]
        public async Task PostMaintenanceRecord_CreatesNewRecord_HappyPath()
        {
            var context = GetInMemoryContext();
            var vehicle = new Vehicle { VIN = "12345678901234567", Make = "Ford", Model = "F-150", Year = 2020, Mileage = 50000, Status = "Active" };
            context.Vehicles.Add(vehicle);
            await context.SaveChangesAsync();

            var controller = CreateController(context);

            var newRecord = new MaintenanceRecord
            {
                VehicleId = vehicle.Id,
                ServiceDate = DateTime.Now,
                ServiceType = "Engine Repair",
                Cost = 1500.00m,
                MileageAtService = 50000,
                PerformedBy = "Jane Smith"
            };

            var result = await controller.PostMaintenanceRecord(newRecord);

            var actionResult = Assert.IsType<ActionResult<MaintenanceRecord>>(result);
            var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
            var record = Assert.IsType<MaintenanceRecord>(createdResult.Value);
            Assert.Equal("Engine Repair", record.ServiceType);
            Assert.True(record.Id > 0);
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
//    public class MaintenanceControllerTests
//    {
//        private FleetContext GetInMemoryContext()
//        {
//            var options = new DbContextOptionsBuilder<FleetContext>()
//                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
//                .Options;

//            return new FleetContext(options);
//        }

//        private MaintenanceController CreateController(FleetContext context)
//        {
//            var mockLogger = new Mock<ILogger<MaintenanceController>>();
//            var mockTelemetry = new Mock<TelemetryClient>();
//            return new MaintenanceController(context, mockLogger.Object, mockTelemetry.Object);
//        }

//        [Fact]
//        public async Task GetMaintenanceRecords_ReturnsAllRecords_HappyPath()
//        {
//            var context = GetInMemoryContext();

//            var vehicle = new Vehicle { VIN = "12345678901234567", Make = "Ford", Model = "F-150", Year = 2020, Mileage = 50000, Status = "Active" };
//            context.Vehicles.Add(vehicle);
//            await context.SaveChangesAsync();

//            context.MaintenanceRecords.AddRange(
//                new MaintenanceRecord { VehicleId = vehicle.Id, ServiceDate = DateTime.Now, ServiceType = "Oil Change", Cost = 50.00m, MileageAtService = 50000 },
//                new MaintenanceRecord { VehicleId = vehicle.Id, ServiceDate = DateTime.Now.AddDays(-30), ServiceType = "Tire Rotation", Cost = 75.00m, MileageAtService = 48000 }
//            );
//            await context.SaveChangesAsync();

//            var controller = CreateController(context);

//            var result = await controller.GetMaintenanceRecords();

//            var actionResult = Assert.IsType<ActionResult<IEnumerable<MaintenanceRecord>>>(result);
//            var records = Assert.IsAssignableFrom<IEnumerable<MaintenanceRecord>>(actionResult.Value);
//            Assert.Equal(2, records.Count());
//        }

//        [Fact]
//        public async Task GetMaintenanceRecords_ReturnsEmpty_WhenNoRecords()
//        {
//            var context = GetInMemoryContext();
//            var controller = CreateController(context);

//            var result = await controller.GetMaintenanceRecords();

//            var actionResult = Assert.IsType<ActionResult<IEnumerable<MaintenanceRecord>>>(result);
//            var records = Assert.IsAssignableFrom<IEnumerable<MaintenanceRecord>>(actionResult.Value);
//            Assert.Empty(records);
//        }

//        [Fact]
//        public async Task GetMaintenanceRecord_ReturnsRecord_WhenRecordExists()
//        {
//            var context = GetInMemoryContext();

//            var vehicle = new Vehicle { VIN = "12345678901234567", Make = "Ford", Model = "F-150", Year = 2020, Mileage = 50000, Status = "Active" };
//            context.Vehicles.Add(vehicle);
//            await context.SaveChangesAsync();

//            var record = new MaintenanceRecord
//            {
//                VehicleId = vehicle.Id,
//                ServiceDate = DateTime.Now,
//                ServiceType = "Oil Change",
//                Cost = 50.00m,
//                MileageAtService = 50000,
//                PerformedBy = "John Doe"
//            };
//            context.MaintenanceRecords.Add(record);
//            await context.SaveChangesAsync();

//            var controller = CreateController(context);

//            var result = await controller.GetMaintenanceRecord(record.Id);

//            var actionResult = Assert.IsType<ActionResult<MaintenanceRecord>>(result);
//            var returnedRecord = Assert.IsType<MaintenanceRecord>(actionResult.Value);
//            Assert.Equal("Oil Change", returnedRecord.ServiceType);
//            Assert.Equal(50.00m, returnedRecord.Cost);
//        }

//        [Fact]
//        public async Task GetMaintenanceRecord_ReturnsNotFound_WhenRecordDoesNotExist()
//        {
//            var context = GetInMemoryContext();
//            var controller = CreateController(context);

//            var result = await controller.GetMaintenanceRecord(999);

//            Assert.IsType<NotFoundResult>(result.Result);
//        }

//        [Fact]
//        public async Task GetMaintenanceByVehicle_ReturnsRecordsForSpecificVehicle()
//        {
//            var context = GetInMemoryContext();

//            var vehicle1 = new Vehicle { VIN = "12345678901234567", Make = "Ford", Model = "F-150", Year = 2020, Mileage = 50000, Status = "Active" };
//            var vehicle2 = new Vehicle { VIN = "23456789012345678", Make = "Chevy", Model = "Silverado", Year = 2021, Mileage = 30000, Status = "Active" };
//            context.Vehicles.AddRange(vehicle1, vehicle2);
//            await context.SaveChangesAsync();

//            context.MaintenanceRecords.AddRange(
//                new MaintenanceRecord { VehicleId = vehicle1.Id, ServiceDate = DateTime.Now, ServiceType = "Oil Change", Cost = 50.00m, MileageAtService = 50000 },
//                new MaintenanceRecord { VehicleId = vehicle1.Id, ServiceDate = DateTime.Now.AddDays(-30), ServiceType = "Tire Rotation", Cost = 75.00m, MileageAtService = 48000 },
//                new MaintenanceRecord { VehicleId = vehicle2.Id, ServiceDate = DateTime.Now, ServiceType = "Brake Service", Cost = 200.00m, MileageAtService = 30000 }
//            );
//            await context.SaveChangesAsync();

//            var controller = CreateController(context);

//            var result = await controller.GetMaintenanceByVehicle(vehicle1.Id);

//            var actionResult = Assert.IsType<ActionResult<IEnumerable<MaintenanceRecord>>>(result);
//            var records = Assert.IsAssignableFrom<IEnumerable<MaintenanceRecord>>(actionResult.Value);
//            Assert.Equal(2, records.Count());
//            Assert.All(records, r => Assert.Equal(vehicle1.Id, r.VehicleId));
//        }

//        [Fact]
//        public async Task GetMaintenanceByVehicle_ReturnsEmpty_WhenVehicleHasNoMaintenance()
//        {
//            var context = GetInMemoryContext();

//            var vehicle = new Vehicle { VIN = "12345678901234567", Make = "Ford", Model = "F-150", Year = 2020, Mileage = 50000, Status = "Active" };
//            context.Vehicles.Add(vehicle);
//            await context.SaveChangesAsync();

//            var controller = CreateController(context);

//            var result = await controller.GetMaintenanceByVehicle(vehicle.Id);

//            var actionResult = Assert.IsType<ActionResult<IEnumerable<MaintenanceRecord>>>(result);
//            var records = Assert.IsAssignableFrom<IEnumerable<MaintenanceRecord>>(actionResult.Value);
//            Assert.Empty(records);
//        }

//        // --- Repeat same pattern for Post, Put, Delete tests ---
//        // Replace every `new MaintenanceController(context, mockLogger.Object)` with `CreateController(context)`

//        // For example:
//        [Fact]
//        public async Task PostMaintenanceRecord_CreatesNewRecord_HappyPath()
//        {
//            var context = GetInMemoryContext();
//            var vehicle = new Vehicle { VIN = "12345678901234567", Make = "Ford", Model = "F-150", Year = 2020, Mileage = 50000, Status = "Active" };
//            context.Vehicles.Add(vehicle);
//            await context.SaveChangesAsync();

//            var controller = CreateController(context);

//            var newRecord = new MaintenanceRecord
//            {
//                VehicleId = vehicle.Id,
//                ServiceDate = DateTime.Now,
//                ServiceType = "Engine Repair",
//                Cost = 1500.00m,
//                MileageAtService = 50000,
//                PerformedBy = "Jane Smith"
//            };

//            var result = await controller.PostMaintenanceRecord(newRecord);

//            var actionResult = Assert.IsType<ActionResult<MaintenanceRecord>>(result);
//            var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
//            var record = Assert.IsType<MaintenanceRecord>(createdResult.Value);
//            Assert.Equal("Engine Repair", record.ServiceType);
//            Assert.True(record.Id > 0);
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
//    public class MaintenanceControllerTests
//    {
//        private FleetContext GetInMemoryContext()
//        {
//            var options = new DbContextOptionsBuilder<FleetContext>()
//                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
//                .Options;

//            return new FleetContext(options);
//        }

//        [Fact]
//        public async Task GetMaintenanceRecords_ReturnsAllRecords_HappyPath()
//        {
//            // Arrange
//            var context = GetInMemoryContext();
//            var mockLogger = new Mock<ILogger<MaintenanceController>>();

//            var vehicle = new Vehicle { VIN = "12345678901234567", Make = "Ford", Model = "F-150", Year = 2020, Mileage = 50000, Status = "Active" };
//            context.Vehicles.Add(vehicle);
//            await context.SaveChangesAsync();

//            context.MaintenanceRecords.AddRange(
//                new MaintenanceRecord { VehicleId = vehicle.Id, ServiceDate = DateTime.Now, ServiceType = "Oil Change", Cost = 50.00m, MileageAtService = 50000 },
//                new MaintenanceRecord { VehicleId = vehicle.Id, ServiceDate = DateTime.Now.AddDays(-30), ServiceType = "Tire Rotation", Cost = 75.00m, MileageAtService = 48000 }
//            );
//            await context.SaveChangesAsync();

//            var controller = new MaintenanceController(context, mockLogger.Object);

//            // Act
//            var result = await controller.GetMaintenanceRecords();

//            // Assert
//            var actionResult = Assert.IsType<ActionResult<IEnumerable<MaintenanceRecord>>>(result);
//            var records = Assert.IsAssignableFrom<IEnumerable<MaintenanceRecord>>(actionResult.Value);
//            Assert.Equal(2, records.Count());
//        }

//        [Fact]
//        public async Task GetMaintenanceRecords_ReturnsEmpty_WhenNoRecords()
//        {
//            // Arrange
//            var context = GetInMemoryContext();
//            var mockLogger = new Mock<ILogger<MaintenanceController>>();
//            var controller = new MaintenanceController(context, mockLogger.Object);

//            // Act
//            var result = await controller.GetMaintenanceRecords();

//            // Assert
//            var actionResult = Assert.IsType<ActionResult<IEnumerable<MaintenanceRecord>>>(result);
//            var records = Assert.IsAssignableFrom<IEnumerable<MaintenanceRecord>>(actionResult.Value);
//            Assert.Empty(records);
//        }

//        [Fact]
//        public async Task GetMaintenanceRecord_ReturnsRecord_WhenRecordExists()
//        {
//            // Arrange
//            var context = GetInMemoryContext();
//            var mockLogger = new Mock<ILogger<MaintenanceController>>();

//            var vehicle = new Vehicle { VIN = "12345678901234567", Make = "Ford", Model = "F-150", Year = 2020, Mileage = 50000, Status = "Active" };
//            context.Vehicles.Add(vehicle);
//            await context.SaveChangesAsync();

//            var record = new MaintenanceRecord
//            {
//                VehicleId = vehicle.Id,
//                ServiceDate = DateTime.Now,
//                ServiceType = "Oil Change",
//                Cost = 50.00m,
//                MileageAtService = 50000,
//                PerformedBy = "John Doe"
//            };
//            context.MaintenanceRecords.Add(record);
//            await context.SaveChangesAsync();

//            var controller = new MaintenanceController(context, mockLogger.Object);

//            // Act
//            var result = await controller.GetMaintenanceRecord(record.Id);

//            // Assert
//            var actionResult = Assert.IsType<ActionResult<MaintenanceRecord>>(result);
//            var returnedRecord = Assert.IsType<MaintenanceRecord>(actionResult.Value);
//            Assert.Equal("Oil Change", returnedRecord.ServiceType);
//            Assert.Equal(50.00m, returnedRecord.Cost);
//        }

//        [Fact]
//        public async Task GetMaintenanceRecord_ReturnsNotFound_WhenRecordDoesNotExist()
//        {
//            // Arrange
//            var context = GetInMemoryContext();
//            var mockLogger = new Mock<ILogger<MaintenanceController>>();
//            var controller = new MaintenanceController(context, mockLogger.Object);

//            // Act
//            var result = await controller.GetMaintenanceRecord(999);

//            // Assert
//            Assert.IsType<NotFoundResult>(result.Result);
//        }

//        [Fact]
//        public async Task GetMaintenanceByVehicle_ReturnsRecordsForSpecificVehicle()
//        {
//            // Arrange
//            var context = GetInMemoryContext();
//            var mockLogger = new Mock<ILogger<MaintenanceController>>();

//            var vehicle1 = new Vehicle { VIN = "12345678901234567", Make = "Ford", Model = "F-150", Year = 2020, Mileage = 50000, Status = "Active" };
//            var vehicle2 = new Vehicle { VIN = "23456789012345678", Make = "Chevy", Model = "Silverado", Year = 2021, Mileage = 30000, Status = "Active" };
//            context.Vehicles.AddRange(vehicle1, vehicle2);
//            await context.SaveChangesAsync();

//            context.MaintenanceRecords.AddRange(
//                new MaintenanceRecord { VehicleId = vehicle1.Id, ServiceDate = DateTime.Now, ServiceType = "Oil Change", Cost = 50.00m, MileageAtService = 50000 },
//                new MaintenanceRecord { VehicleId = vehicle1.Id, ServiceDate = DateTime.Now.AddDays(-30), ServiceType = "Tire Rotation", Cost = 75.00m, MileageAtService = 48000 },
//                new MaintenanceRecord { VehicleId = vehicle2.Id, ServiceDate = DateTime.Now, ServiceType = "Brake Service", Cost = 200.00m, MileageAtService = 30000 }
//            );
//            await context.SaveChangesAsync();

//            var controller = new MaintenanceController(context, mockLogger.Object);

//            // Act
//            var result = await controller.GetMaintenanceByVehicle(vehicle1.Id);

//            // Assert
//            var actionResult = Assert.IsType<ActionResult<IEnumerable<MaintenanceRecord>>>(result);
//            var records = Assert.IsAssignableFrom<IEnumerable<MaintenanceRecord>>(actionResult.Value);
//            Assert.Equal(2, records.Count());
//            Assert.All(records, r => Assert.Equal(vehicle1.Id, r.VehicleId));
//        }

//        [Fact]
//        public async Task GetMaintenanceByVehicle_ReturnsEmpty_WhenVehicleHasNoMaintenance()
//        {
//            // Arrange
//            var context = GetInMemoryContext();
//            var mockLogger = new Mock<ILogger<MaintenanceController>>();

//            var vehicle = new Vehicle { VIN = "12345678901234567", Make = "Ford", Model = "F-150", Year = 2020, Mileage = 50000, Status = "Active" };
//            context.Vehicles.Add(vehicle);
//            await context.SaveChangesAsync();

//            var controller = new MaintenanceController(context, mockLogger.Object);

//            // Act
//            var result = await controller.GetMaintenanceByVehicle(vehicle.Id);

//            // Assert
//            var actionResult = Assert.IsType<ActionResult<IEnumerable<MaintenanceRecord>>>(result);
//            var records = Assert.IsAssignableFrom<IEnumerable<MaintenanceRecord>>(actionResult.Value);
//            Assert.Empty(records);
//        }

//        [Fact]
//        public async Task PostMaintenanceRecord_CreatesNewRecord_HappyPath()
//        {
//            // Arrange
//            var context = GetInMemoryContext();
//            var mockLogger = new Mock<ILogger<MaintenanceController>>();

//            var vehicle = new Vehicle { VIN = "12345678901234567", Make = "Ford", Model = "F-150", Year = 2020, Mileage = 50000, Status = "Active" };
//            context.Vehicles.Add(vehicle);
//            await context.SaveChangesAsync();

//            var controller = new MaintenanceController(context, mockLogger.Object);

//            var newRecord = new MaintenanceRecord
//            {
//                VehicleId = vehicle.Id,
//                ServiceDate = DateTime.Now,
//                ServiceType = "Engine Repair",
//                Cost = 1500.00m,
//                MileageAtService = 50000,
//                PerformedBy = "Jane Smith"
//            };

//            // Act
//            var result = await controller.PostMaintenanceRecord(newRecord);

//            // Assert
//            var actionResult = Assert.IsType<ActionResult<MaintenanceRecord>>(result);
//            var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
//            var record = Assert.IsType<MaintenanceRecord>(createdResult.Value);
//            Assert.Equal("Engine Repair", record.ServiceType);
//            Assert.True(record.Id > 0);
//        }

//        [Fact]
//        public async Task PutMaintenanceRecord_UpdatesRecord_HappyPath()
//        {
//            // Arrange
//            var context = GetInMemoryContext();
//            var mockLogger = new Mock<ILogger<MaintenanceController>>();

//            var vehicle = new Vehicle { VIN = "12345678901234567", Make = "Ford", Model = "F-150", Year = 2020, Mileage = 50000, Status = "Active" };
//            context.Vehicles.Add(vehicle);
//            await context.SaveChangesAsync();

//            var record = new MaintenanceRecord
//            {
//                VehicleId = vehicle.Id,
//                ServiceDate = DateTime.Now,
//                ServiceType = "Oil Change",
//                Cost = 50.00m,
//                MileageAtService = 50000
//            };
//            context.MaintenanceRecords.Add(record);
//            await context.SaveChangesAsync();

//            var controller = new MaintenanceController(context, mockLogger.Object);

//            // Act
//            record.Cost = 75.00m;
//            record.Notes = "Updated notes";
//            var result = await controller.PutMaintenanceRecord(record.Id, record);

//            // Assert
//            Assert.IsType<NoContentResult>(result);

//            var updatedRecord = await context.MaintenanceRecords.FindAsync(record.Id);
//            Assert.Equal(75.00m, updatedRecord.Cost);
//            Assert.Equal("Updated notes", updatedRecord.Notes);
//        }

//        [Fact]
//        public async Task PutMaintenanceRecord_ReturnsBadRequest_WhenIdMismatch()
//        {
//            // Arrange
//            var context = GetInMemoryContext();
//            var mockLogger = new Mock<ILogger<MaintenanceController>>();
//            var controller = new MaintenanceController(context, mockLogger.Object);

//            var record = new MaintenanceRecord { Id = 1, VehicleId = 1, ServiceDate = DateTime.Now, ServiceType = "Oil Change", Cost = 50.00m };

//            // Act
//            var result = await controller.PutMaintenanceRecord(999, record);

//            // Assert
//            Assert.IsType<BadRequestResult>(result);
//        }

//        [Fact]
//        public async Task DeleteMaintenanceRecord_RemovesRecord_HappyPath()
//        {
//            // Arrange
//            var context = GetInMemoryContext();
//            var mockLogger = new Mock<ILogger<MaintenanceController>>();

//            var vehicle = new Vehicle { VIN = "12345678901234567", Make = "Ford", Model = "F-150", Year = 2020, Mileage = 50000, Status = "Active" };
//            context.Vehicles.Add(vehicle);
//            await context.SaveChangesAsync();

//            var record = new MaintenanceRecord
//            {
//                VehicleId = vehicle.Id,
//                ServiceDate = DateTime.Now,
//                ServiceType = "Oil Change",
//                Cost = 50.00m,
//                MileageAtService = 50000
//            };
//            context.MaintenanceRecords.Add(record);
//            await context.SaveChangesAsync();

//            var controller = new MaintenanceController(context, mockLogger.Object);

//            // Act
//            var result = await controller.DeleteMaintenanceRecord(record.Id);

//            // Assert
//            Assert.IsType<NoContentResult>(result);
//            Assert.Null(await context.MaintenanceRecords.FindAsync(record.Id));
//        }

//        [Fact]
//        public async Task DeleteMaintenanceRecord_ReturnsNotFound_WhenRecordDoesNotExist()
//        {
//            // Arrange
//            var context = GetInMemoryContext();
//            var mockLogger = new Mock<ILogger<MaintenanceController>>();
//            var controller = new MaintenanceController(context, mockLogger.Object);

//            // Act
//            var result = await controller.DeleteMaintenanceRecord(999);

//            // Assert
//            Assert.IsType<NotFoundResult>(result);
//        }

//        [Fact]
//        public async Task PostMaintenanceRecord_CreatesRecordWithHighCost_EdgeCase()
//        {
//            // Arrange
//            var context = GetInMemoryContext();
//            var mockLogger = new Mock<ILogger<MaintenanceController>>();

//            var vehicle = new Vehicle { VIN = "12345678901234567", Make = "Ford", Model = "F-150", Year = 2020, Mileage = 50000, Status = "Active" };
//            context.Vehicles.Add(vehicle);
//            await context.SaveChangesAsync();

//            var controller = new MaintenanceController(context, mockLogger.Object);

//            var newRecord = new MaintenanceRecord
//            {
//                VehicleId = vehicle.Id,
//                ServiceDate = DateTime.Now,
//                ServiceType = "Complete Overhaul",
//                Cost = 25000.00m, // Very high cost
//                MileageAtService = 150000,
//                IsWarrantyCovered = false
//            };

//            // Act
//            var result = await controller.PostMaintenanceRecord(newRecord);

//            // Assert
//            var actionResult = Assert.IsType<ActionResult<MaintenanceRecord>>(result);
//            var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
//            var record = Assert.IsType<MaintenanceRecord>(createdResult.Value);
//            Assert.Equal(25000.00m, record.Cost);
//        }
//    }
//}