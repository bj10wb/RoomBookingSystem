using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using UniversityRoomBookingSystem.Models;
using UniversityRoomBookingSystem.Repositories;
using UniversityRoomBookingSystem.Services;
using Xunit;

namespace UniversityRoomBookingSystem.Tests
{
    public class SuitabilityAndDataManagerTests : IDisposable
    {
        private readonly string _folder = Path.Combine(Path.GetTempPath(), "urbs-" + Guid.NewGuid().ToString("N"));
        private string RoomsFile => Path.Combine(_folder, "rooms.json");
        private string BookingsFile => Path.Combine(_folder, "bookings.json");

        public SuitabilityAndDataManagerTests()
        {
            Directory.CreateDirectory(_folder);
        }

        public void Dispose()
        {
            if (Directory.Exists(_folder)) Directory.Delete(_folder, recursive: true);
        }

        private (DataManager Manager, InMemoryRoomRepository Rooms, InMemoryBookingRepository Bookings) CreateManager()
        {
            var rooms = new InMemoryRoomRepository();
            var bookings = new InMemoryBookingRepository();
            var logger = new FileLogger(Path.Combine(_folder, "log.txt"));
            var manager = new DataManager(rooms, bookings, new JsonStorageService(), logger, RoomsFile, BookingsFile);
            return (manager, rooms, bookings);
        }

        private static (BookingService Service, InMemoryBookingRepository Bookings) CreateService()
        {
            var rooms = new InMemoryRoomRepository();
            rooms.Add(new StandardRoom("SR-101", "Seminar Room 101", 30, new[] { Equipment.Projector, Equipment.Whiteboard }));
            rooms.Add(new StandardRoom("SR-102", "Small Tutorial Room", 15, new[] { Equipment.Whiteboard }));
            rooms.Add(new ComputerLab("CL-202", "Data Science Lab", 40, 35, new[] { Equipment.Computers }));
            rooms.Add(new LectureTheatre("LT-A", "Main Lecture Theatre", 150, new[] { Equipment.Projector, Equipment.Microphone }));
            var bookings = new InMemoryBookingRepository();
            return (new BookingService(rooms, bookings), bookings);
        }

        private static readonly DateTime Nine = new(2026, 11, 2, 9, 0, 0);


        [Fact]
        public void FindSuitableRooms_ReturnsSmallestSuitableRoomFirst()
        {
            var (service, _) = CreateService();
            var result = service.FindSuitableRooms(12, null);
            Assert.Equal(new[] { "SR-102", "SR-101", "CL-202", "LT-A" }, result.Select(r => r.Id).ToArray());
        }

        [Fact]
        public void FindSuitableRooms_FiltersByRequiredEquipment()
        {
            var (service, _) = CreateService();
            var result = service.FindSuitableRooms(10, new[] { Equipment.Projector, Equipment.Microphone });
            Assert.Equal(new[] { "LT-A" }, result.Select(r => r.Id).ToArray());
        }

        [Fact]
        public void FindSuitableRooms_UsesComputerLabWorkstationLimit_NotJustCapacity()
        {
            var (service, _) = CreateService();
            var result = service.FindSuitableRooms(36, null);
            Assert.DoesNotContain("CL-202", result.Select(r => r.Id));
            Assert.Contains(result, r => r.Id == "LT-A");
        }

        [Fact]
        public void FindSuitableRooms_WithTimeWindow_ExcludesBookedRoomsButAllowsAdjacent()
        {
            var (service, _) = CreateService();
            Assert.True(service.TryCreateBooking("SR-101", new TeachingSession("CS101", 10, null, Nine, Nine.AddHours(2))).Success);

            var overlapping = service.FindSuitableRooms(10, null, Nine.AddHours(1), Nine.AddHours(3));
            var adjacent = service.FindSuitableRooms(10, null, Nine.AddHours(2), Nine.AddHours(3));

            Assert.DoesNotContain("SR-101", overlapping.Select(r => r.Id));
            Assert.Contains(adjacent, r => r.Id == "SR-101");
        }

        [Fact]
        public void FindSuitableRooms_WhenNothingFits_ReturnsEmptyList()
        {
            var (service, _) = CreateService();
            Assert.Empty(service.FindSuitableRooms(1000, null));
        }


        [Fact]
        public void Initialize_FirstRun_CreatesSixRoomsSampleBookingsAndFiles()
        {
            var (manager, rooms, bookings) = CreateManager();

            manager.Initialize();

            Assert.Equal(6, rooms.GetAll().Count());
            Assert.Equal(3, bookings.GetAll().Count());
            Assert.True(File.Exists(RoomsFile));
            Assert.True(File.Exists(BookingsFile));
        }

        [Fact]
        public void Initialize_AfterRestart_ReloadsBookingsFromDisk()
        {
            var (first, _, firstBookings) = CreateManager();
            first.Initialize();
            firstBookings.Add(new Booking("SR-101", new TeachingSession("CS999", 5, null, Nine, Nine.AddHours(1))));
            first.Save();

            var (second, rooms, bookings) = CreateManager();
            var messages = second.Initialize();

            Assert.Equal(6, rooms.GetAll().Count());
            Assert.Equal(4, bookings.GetAll().Count());
            Assert.Contains(bookings.GetAll(), b => b.Session.ModuleName == "CS999");
            Assert.False(messages.Any(m => m.Level == MessageLevel.Warning || m.Level == MessageLevel.Error));
        }

        [Fact]
        public void Initialize_CorruptRoomsFile_BacksItUpAndRestoresDefaultRooms()
        {
            var (first, _, _) = CreateManager();
            first.Initialize();
            File.WriteAllText(RoomsFile, "{ not valid json }}}");

            var (second, rooms, _) = CreateManager();
            var messages = second.Initialize();

            Assert.Equal(6, rooms.GetAll().Count());
            var backup = Assert.Single(Directory.GetFiles(_folder, "rooms.json.corrupt-*"));
            Assert.Equal("{ not valid json }}}", File.ReadAllText(backup));
            Assert.True(messages.Any(m => m.Level == MessageLevel.Warning));
        }

        [Fact]
        public void Initialize_CorruptBookingsFile_IsBackedUpAndRoomsStillLoad()
        {
            var (first, _, _) = CreateManager();
            first.Initialize();
            File.WriteAllText(BookingsFile, "garbage");

            var (second, rooms, bookings) = CreateManager();
            second.Initialize();

            Assert.Equal(6, rooms.GetAll().Count());
            Assert.Empty(bookings.GetAll());
            Assert.Single(Directory.GetFiles(_folder, "bookings.json.corrupt-*"));
        }

        [Fact]
        public void Reload_RestoresSavedStateAndDiscardsUnsavedChanges()
        {
            var (manager, _, bookings) = CreateManager();
            manager.Initialize();
            bookings.Add(new Booking("SR-101", new TeachingSession("UNSAVED", 5, null, Nine, Nine.AddHours(1))));

            manager.Reload();

            Assert.Equal(3, bookings.GetAll().Count());
            Assert.DoesNotContain("UNSAVED", bookings.GetAll().Select(b => b.Session.ModuleName));
        }


        [Fact]
        public void LoadRooms_NullEquipmentList_IsRejected()
        {
            File.WriteAllText(RoomsFile, "[{\"$type\":\"standard\",\"Id\":\"X\",\"Name\":\"N\",\"Capacity\":5,\"Equipment\":null}]");

            var (success, rooms, message) = new JsonStorageService().LoadRooms(RoomsFile);

            Assert.False(success);
            Assert.Null(rooms);
            Assert.False(string.IsNullOrWhiteSpace(message));
        }

        [Fact]
        public void LoadBookings_NullRequiredEquipment_IsRejected()
        {
            var storage = new JsonStorageService();
            var booking = new Booking("SR-101", new TeachingSession("CS101", 5, null, Nine, Nine.AddHours(1)));
            Assert.True(storage.SaveBookings(BookingsFile, new[] { booking }).Success);

            var array = JsonNode.Parse(File.ReadAllText(BookingsFile))!.AsArray();
            array[0]!["Session"]!["RequiredEquipment"] = null;
            File.WriteAllText(BookingsFile, array.ToJsonString());

            var (success, bookings, _) = storage.LoadBookings(BookingsFile);

            Assert.False(success);
            Assert.Null(bookings);
        }
    }
}