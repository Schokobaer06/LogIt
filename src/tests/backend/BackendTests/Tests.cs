using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogIt.Core.Controllers;
using LogIt.Core.Data;
using LogIt.Core.Models;
using LogIt.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace BackendTests
{
    [TestClass]
    public sealed class Tests
    {
        // Fix: Mark as nullable to satisfy CS8618
        private DbContextOptions<LogItDbContext>? _dbOptions;

        [TestInitialize]
        public void Setup()
        {
            // Fix: UseInMemoryDatabase requires Microsoft.EntityFrameworkCore.InMemory
            _dbOptions = new DbContextOptionsBuilder<LogItDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        [TestMethod]
        public async Task UsersController_CanAddAndGetUser()
        {
            using var db = new LogItDbContext(_dbOptions!);
            var controller = new UsersController(db);

            var user = new User { Role = UserRole.Backend };
            var result = await controller.Post(user);

            Assert.IsInstanceOfType(result.Result, typeof(CreatedAtActionResult));
            Assert.AreEqual(1, db.Users.Count());
            Assert.AreEqual(UserRole.Backend, db.Users.First().Role);

            var users = await controller.Get();
            Assert.AreEqual(1, users.Count());
        }

        [TestMethod]
        public async Task LogEntriesController_CanAddAndGetLogEntry()
        {
            using var db = new LogItDbContext(_dbOptions!);
            db.Users.Add(new User { UserId = 1, Role = UserRole.System });
            db.SaveChanges();

            var controller = new LogEntriesController(db);

            var log = new LogEntry { ProgramName = "TestApp", UserId = 1 };
            var result = await controller.Post(log);

            Assert.IsInstanceOfType(result.Result, typeof(CreatedAtActionResult));
            Assert.AreEqual(1, db.LogEntries.Count());
            Assert.AreEqual("TestApp", db.LogEntries.First().ProgramName);

            var all = await controller.GetAll();
            Assert.AreEqual(1, all.Count());
        }

        [TestMethod]
        public async Task LogEntriesController_GetActive_ReturnsOnlyActive()
        {
            using var db = new LogItDbContext(_dbOptions!);
            db.Users.Add(new User { UserId = 1, Role = UserRole.System });
            var log = new LogEntry { ProgramName = "ActiveApp", UserId = 1 };
            db.LogEntries.Add(log);
            db.SaveChanges();

            db.Sessions.Add(new Session
            {
                LogEntryId = log.LogEntryId,
                StartTime = DateTime.Now,
                EndTime = null,
                Duration = TimeSpan.Zero,
                SessionNumber = 1
            });
            db.SaveChanges();

            var controller = new LogEntriesController(db);
            var active = await controller.GetActive();
            Assert.AreEqual(1, active.Count());
        }

        [TestMethod]
        public async Task SessionsController_CanAddSession()
        {
            using var db = new LogItDbContext(_dbOptions!);
            db.Users.Add(new User { UserId = 1, Role = UserRole.System });
            var log = new LogEntry { LogEntryId = 1, ProgramName = "SessionApp", UserId = 1 };
            db.LogEntries.Add(log);
            db.SaveChanges();

            var controller = new SessionsController(db);
            var session = new Session
            {
                StartTime = DateTime.Now,
                Duration = TimeSpan.FromMinutes(5)
            };
            var result = await controller.Post(log.LogEntryId, session);

            Assert.IsInstanceOfType(result.Result, typeof(CreatedAtActionResult));
            Assert.AreEqual(1, db.Sessions.Count());
            Assert.AreEqual(log.LogEntryId, db.Sessions.First().LogEntryId);
        }

        [TestMethod]
        public async Task SessionsController_Post_ReturnsNotFound_IfLogEntryMissing()
        {
            using var db = new LogItDbContext(_dbOptions!);
            var controller = new SessionsController(db);
            var session = new Session { StartTime = DateTime.Now };
            var result = await controller.Post(999, session);

            Assert.IsInstanceOfType(result.Result, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task ProcessMonitorService_CleansUpOpenSessions()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            // Fix: UseInMemoryDatabase requires Microsoft.EntityFrameworkCore.InMemory
            services.AddDbContext<LogItDbContext>(opt => opt.UseInMemoryDatabase("ProcessMonitorServiceTestDb"));
            var provider = services.BuildServiceProvider();

            using (var scope = provider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<LogItDbContext>();
                db.Users.Add(new User { UserId = 1, Role = UserRole.System });
                var log = new LogEntry { LogEntryId = 1, ProgramName = "ProcApp", UserId = 1 };
                db.LogEntries.Add(log);
                db.Sessions.Add(new Session
                {
                    LogEntryId = 1,
                    StartTime = DateTime.Now.AddHours(-1),
                    EndTime = null,
                    Duration = TimeSpan.FromMinutes(30),
                    SessionNumber = 1
                });
                db.SaveChanges();
            }

            var logger = new Mock<ILogger<ProcessMonitorService>>();
            var service = new ProcessMonitorService(provider, logger.Object);

            // Do not cancel immediately, allow ExecuteAsync to run
            var cts = new CancellationTokenSource();
            // Wait a short time before canceling to allow cleanup
            var method = typeof(ProcessMonitorService).GetMethod("ExecuteAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(method, "Could not find ExecuteAsync method via reflection.");
            var taskObj = method.Invoke(service, new object[] { cts.Token });
            Assert.IsNotNull(taskObj, "Invoke returned null.");
            var task = taskObj as Task;
            Assert.IsNotNull(task, "Invoke did not return a Task.");

            // Wait for the task to complete or timeout
            await Task.WhenAny(task!, Task.Delay(1000));
            cts.Cancel();

            using (var scope = provider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<LogItDbContext>();
                var session = db.Sessions.First();
                Assert.IsNotNull(session.EndTime);
            }
        }
    }
}
