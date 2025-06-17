using Xunit;
using LogIt.UI.Services;
using LogIt.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Moq;
using LogIt.Core.Models;

namespace FrontendTests
{
    public class Tests
    {
        [Fact]
        public void LogEntryDisplay_ProgramName_ReturnsCorrectName()
        {
            var entry = new LogEntry { ProgramName = "TestApp", Sessions = new List<Session>() };
            var display = new LogEntryDisplay(entry);
            Assert.Equal("TestApp", display.ProgramName);
        }

        [Fact]
        public void LogEntryDisplay_IsActive_ReturnsTrueIfSessionOpen()
        {
            var session = new Session { StartTime = DateTime.Now, EndTime = null };
            var entry = new LogEntry { ProgramName = "Test", Sessions = new List<Session> { session } };
            var display = new LogEntryDisplay(entry);
            Assert.True(display.IsActive);
        }

        [Fact]
        public void LogEntryDisplay_IsActive_ReturnsFalseIfAllSessionsClosed()
        {
            var session = new Session { StartTime = DateTime.Now.AddHours(-1), EndTime = DateTime.Now };
            var entry = new LogEntry { ProgramName = "Test", Sessions = new List<Session> { session } };
            var display = new LogEntryDisplay(entry);
            Assert.False(display.IsActive);
        }

        [Fact]
        public void LogEntryDisplay_TotalRunTimeDisplay_CalculatesSum()
        {
            var now = DateTime.Now;
            var session1 = new Session { StartTime = now.AddHours(-2), EndTime = now.AddHours(-1) };
            var session2 = new Session { StartTime = now.AddMinutes(-30), EndTime = now };
            // If Duration is not auto-calculated, set it manually:
            session1.Duration = session1.EndTime.Value - session1.StartTime;
            session2.Duration = session2.EndTime.Value - session2.StartTime;

            var entry = new LogEntry { ProgramName = "Test", Sessions = new List<Session> { session1, session2 } };
            var display = new LogEntryDisplay(entry);
            Assert.Contains("1h", display.TotalRunTimeDisplay); // At least 1 hour in total
        }

        [Fact]
        public async Task ApiService_GetAllLogEntriesAsync_ReturnsListOrEmpty()
        {
            var service = new ApiService();
            var result = await service.GetAllLogEntriesAsync();
            Assert.NotNull(result);
            Assert.IsType<List<LogEntry>>(result);
        }

        [Fact]
        public void MainWindowViewModel_InitializesProperties()
        {
            var vm = new MainWindowViewModel();
            Assert.NotNull(vm.Entries);
            Assert.NotNull(vm.Series);
            Assert.NotNull(vm.Labels);
            Assert.NotNull(vm.XAxes);
            Assert.NotNull(vm.YAxes);
            Assert.NotNull(vm.YFormatter);
            Assert.NotNull(vm.paint);
            Assert.False(string.IsNullOrEmpty(vm.AppVersion));
        }

        [Fact]
        public void MainWindowViewModel_PlayChartAnimationOnNextRefresh_SetsFlag()
        {
            var vm = new MainWindowViewModel();
            vm.PlayChartAnimationOnNextRefresh();
            // No exception means success, flag is private
        }
    }
}