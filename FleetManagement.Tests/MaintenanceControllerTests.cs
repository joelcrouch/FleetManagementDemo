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
    

        
        [Fact]
        public async Task PutMaintenanceRecord_ReturnsNoContent_HappyPath()
        {
            // Arrange
            var context = GetInMemoryContext();
            var vehicle = new Vehicle { VIN = "123", Make = "Ford" };
            context.Vehicles.Add(vehicle);
            await context.SaveChangesAsync();

            var originalRecord = new MaintenanceRecord { VehicleId = vehicle.Id, ServiceType = "Oil Change", Cost = 50.00m };
            context.MaintenanceRecords.Add(originalRecord);
            await context.SaveChangesAsync();

            // *** FIX: Detach the original entity instance from the context tracker ***
            // This allows the controller to attach the new instance with the same ID without conflict.
            context.Entry(originalRecord).State = EntityState.Detached;
            // ***********************************************************************

            var controller = CreateController(context);

            // Modify the record (ensure ID is preserved for the PUT call)
            var updatedRecord = new MaintenanceRecord
            {
                Id = originalRecord.Id,
                VehicleId = originalRecord.VehicleId,
                ServiceType = "Major Service", // CHANGE
                Cost = 500.00m,                // CHANGE
                ServiceDate = originalRecord.ServiceDate,
                MileageAtService = originalRecord.MileageAtService,
            };

            // Act
            var result = await controller.PutMaintenanceRecord(originalRecord.Id, updatedRecord);

            // Assert
            Assert.IsType<NoContentResult>(result);

            // Verify the update in the database by querying a fresh copy
            var recordInDb = await context.MaintenanceRecords.FindAsync(originalRecord.Id);
            Assert.Equal("Major Service", recordInDb.ServiceType);
            Assert.Equal(500.00m, recordInDb.Cost);
        }
        //public async Task PutMaintenanceRecord_ReturnsNoContent_HappyPath()
        //{
        //    // Arrange
        //    var context = GetInMemoryContext();
        //    var vehicle = new Vehicle { VIN = "123", Make = "Ford" };
        //    context.Vehicles.Add(vehicle);
        //    await context.SaveChangesAsync();

        //    var originalRecord = new MaintenanceRecord { VehicleId = vehicle.Id, ServiceType = "Oil Change", Cost = 50.00m };
        //    context.MaintenanceRecords.Add(originalRecord);
        //    await context.SaveChangesAsync();

        //    var controller = CreateController(context);

        //    // Modify the record
        //    var updatedRecord = new MaintenanceRecord
        //    {
        //        Id = originalRecord.Id,
        //        VehicleId = originalRecord.VehicleId,
        //        ServiceType = "Major Service", // CHANGE
        //        Cost = 500.00m,                // CHANGE
        //        ServiceDate = originalRecord.ServiceDate,
        //        MileageAtService = originalRecord.MileageAtService,
        //    };

        //    // Act
        //    var result = await controller.PutMaintenanceRecord(originalRecord.Id, updatedRecord);

        //    // Assert
        //    Assert.IsType<NoContentResult>(result);

        //    // Verify the update in the database
        //    var recordInDb = await context.MaintenanceRecords.FindAsync(originalRecord.Id);
        //    Assert.Equal("Major Service", recordInDb.ServiceType);
        //    Assert.Equal(500.00m, recordInDb.Cost);
        //}

        [Fact]
        public async Task PutMaintenanceRecord_ReturnsBadRequest_WhenIdMismatch()
        {
            // Arrange
            var context = GetInMemoryContext();
            var controller = CreateController(context);

            var record = new MaintenanceRecord { Id = 10, ServiceType = "Test" };

            // Act: Passing ID 5, but record ID is 10
            var result = await controller.PutMaintenanceRecord(5, record);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task PutMaintenanceRecord_ReturnsNotFound_WhenRecordDoesNotExist()
        {
            // Arrange
            var context = GetInMemoryContext();
            var controller = CreateController(context);

            var nonExistentRecord = new MaintenanceRecord { Id = 999, ServiceType = "Missing" };

            // Act
            // Attempt to update a record that does not exist. The DbUpdateConcurrencyException will be caught,
            // and the fallback check will confirm it's not found.
            var result = await controller.PutMaintenanceRecord(999, nonExistentRecord);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteMaintenanceRecord_ReturnsNoContent_HappyPath()
        {
            // Arrange
            var context = GetInMemoryContext();
            var vehicle = new Vehicle { VIN = "123", Make = "Ford" };
            context.Vehicles.Add(vehicle);
            await context.SaveChangesAsync();

            var recordToDelete = new MaintenanceRecord { VehicleId = vehicle.Id, ServiceType = "Tires", Cost = 100.00m };
            context.MaintenanceRecords.Add(recordToDelete);
            await context.SaveChangesAsync();

            var controller = CreateController(context);
            var recordId = recordToDelete.Id;

            // Act
            var result = await controller.DeleteMaintenanceRecord(recordId);

            // Assert
            Assert.IsType<NoContentResult>(result);

            // Verify the record is gone from the database
            Assert.Null(await context.MaintenanceRecords.FindAsync(recordId));
        }

        [Fact]
        public async Task DeleteMaintenanceRecord_ReturnsNotFound_WhenRecordDoesNotExist()
        {
            // Arrange
            var context = GetInMemoryContext();
            var controller = CreateController(context);

            // Act
            var result = await controller.DeleteMaintenanceRecord(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
}
}

