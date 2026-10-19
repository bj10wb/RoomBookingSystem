using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using UniversityRoomBookingSystem.Models;
using UniversityRoomBookingSystem.Repositories;
using UniversityRoomBookingSystem.Services;
using Xunit;

namespace UniversityRoomBookingSystem.Tests
{
    public class SearchPersistenceAndOverlapTests
    {
        private static List<Room> SeedRooms() => new()
        {
            new StandardRoom("SR-101", "Seminar Room 101", 30, new[] { Equipment.Projector, Equipment.Whiteboard }),
            new StandardRoom("SR-102", "Small Tutorial Room", 15, new[] { Equipment.Whiteboard }),
            new ComputerLab("CL-201", "Software Engineering Lab", 25, 25, new[] { Equipment.Computers, Equipment.Projector }),
            new ComputerLab("CL-202", "Data Science Lab", 40, 35, new[] { Equipment.Computers }),
            new LectureTheatre("LT-A", "Main Lecture Theatre", 150, new[] { Equipment.Projector, Equipment.Microphone }),
            new LectureTheatre("LT-B", "Faculty Auditorium", 80, new[] { Equipment.Microphone })
        };

        private static (BookingService Service, InMemoryBookingRepository Bookings, InMemoryRoomRepository Rooms) CreateService()
        {
            var rooms = new InMemoryRoomRepository();
            foreach (var r in SeedRooms()) rooms.Add(r);
            var bookings = new InMemoryBookingRepository();
            return (new BookingService(rooms, bookings), bookings, rooms);
        }

        private static readonly DateTime Nine = new(2026, 10, 20, 9, 0, 0);

        [Theory]
        [InlineData("ComputerLab", 2)]
        [InlineData("computer lab", 2)]
        [InlineData("LECTURETHEATRE", 2)]
        [InlineData("standard", 2)]
        public void SearchRooms_ByRoomType_FindsEveryRoomOfThatType(string term, int expected)
        {
            var results = ManualAlgorithms.SearchRooms(SeedRooms(), term);
            Assert.Equal(expected, results.Count);
        }

        [Fact]
        public void SearchRooms_ByRoomType_FindsRoomWhoseNameDoesNotContainTheType()
        {
            // "Faculty Auditorium" is a LectureTheatre but its name never says "theatre".
            var results = ManualAlgorithms.SearchRooms(SeedRooms(), "theatre");
            Assert.Contains(results, r => r.Id == "LT-B");
            Assert.Equal(2, results.Count);
        }

        [Fact]
        public void SearchRooms_ByEquipment_ReturnsOnlyRoomsWithThatEquipment()
        {
            var results = ManualAlgorithms.SearchRooms(SeedRooms(), "microphone");
            Assert.Equal(new[] { "LT-A", "LT-B" }, results.Select(r => r.Id).OrderBy(x => x).ToArray());
        }

        [Fact]
        public void SearchRooms_ById_IsCaseInsensitive()
        {
            var results = ManualAlgorithms.SearchRooms(SeedRooms(), "sr-101");
            Assert.Single(results);
            Assert.Equal("SR-101", results[0].Id);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("no-such-room")]
        public void SearchRooms_EmptyOrUnmatched_ReturnsEmptyList(string term)
        {
            Assert.Empty(ManualAlgorithms.SearchRooms(SeedRooms(), term));
        }

        [Fact]
        public void SearchRooms_NullOrEmptyCollection_ReturnsEmptyList()
        {
            Assert.Empty(ManualAlgorithms.SearchRooms(SeedRooms(), null));
            Assert.Empty(ManualAlgorithms.SearchRooms(null, "lab"));
            Assert.Empty(ManualAlgorithms.SearchRooms(new List<Room>(), "lab"));
        }

        [Fact]
        public void RoomType_IsNotWrittenToJson()
        {
            string json = JsonSerializer.Serialize<Room>(SeedRooms()[2]);
            Assert.DoesNotContain("RoomType", json);
        }

        [Fact]
        public void InsertionSortByName_SortsAlphabetically_IgnoringCase()
        {
            var array = SeedRooms().ToArray();
            ManualAlgorithms.InsertionSortByName(array);
            Assert.Equal(
                new[] { "Data Science Lab", "Faculty Auditorium", "Main Lecture Theatre", "Seminar Room 101", "Small Tutorial Room", "Software Engineering Lab" },
                array.Select(r => r.Name).ToArray());
        }

        [Fact]
        public void QuickSortByCapacity_Descending_SortsLargestFirst()
        {
            var array = SeedRooms().ToArray();
            ManualAlgorithms.QuickSortByCapacity(array, 0, array.Length - 1, ascending: false);
            Assert.Equal(new[] { 150, 80, 40, 30, 25, 15 }, array.Select(r => r.Capacity).ToArray());
        }

        [Fact]
        public void Sorting_EmptyAndSingleItemArrays_DoNotThrow()
        {
            var empty = Array.Empty<Room>();
            var single = new Room[] { SeedRooms()[0] };

            ManualAlgorithms.QuickSortByCapacity(empty, 0, -1);
            ManualAlgorithms.InsertionSortByName(empty);
            ManualAlgorithms.QuickSortByCapacity(single, 0, 0);
            ManualAlgorithms.InsertionSortByName(single);

            Assert.Single(single);
        }

        private static TeachingSession Session(int startOffsetHours, int durationHours, int students = 10)
            => new("CS101", students, null, Nine.AddHours(startOffsetHours), Nine.AddHours(startOffsetHours + durationHours));

        [Theory]
        [InlineData(0, 2)]   // identical window
        [InlineData(1, 2)]   // starts inside, ends after
        [InlineData(-1, 2)]  // starts before, ends inside
        [InlineData(-1, 4)]  // fully contains the existing booking
        [InlineData(0, 1)]   // contained at the start
        public void Overlapping_BookingInSameRoom_IsRejected(int offset, int duration)
        {
            var (service, bookings, _) = CreateService();
            Assert.True(service.TryCreateBooking("SR-101", Session(0, 2)).Success);

            var result = service.TryCreateBooking("SR-101", Session(offset, duration));

            Assert.False(result.Success);
            Assert.Single(bookings.GetAll());
        }

        [Theory]
        [InlineData(2, 1)]   // starts exactly when the existing one ends
        [InlineData(-1, 1)]  // ends exactly when the existing one starts
        public void AdjacentBookings_InSameRoom_AreAllowed(int offset, int duration)
        {
            var (service, bookings, _) = CreateService();
            Assert.True(service.TryCreateBooking("SR-101", Session(0, 2)).Success);

            var result = service.TryCreateBooking("SR-101", Session(offset, duration));

            Assert.True(result.Success);
            Assert.Equal(2, bookings.GetAll().Count());
        }

        [Fact]
        public void SameTimeSlot_InDifferentRooms_IsAllowed()
        {
            var (service, _, _) = CreateService();
            Assert.True(service.TryCreateBooking("SR-101", Session(0, 2)).Success);
            Assert.True(service.TryCreateBooking("SR-102", Session(0, 2, students: 5)).Success);
        }

        [Fact]
        public void StudentCount_ExactlyEqualToCapacity_IsAccepted_AndOneOverIsRejected()
        {
            var (service, _, _) = CreateService();
            Assert.True(service.TryCreateBooking("SR-101", Session(0, 1, students: 30)).Success);
            Assert.False(service.TryCreateBooking("SR-101", Session(5, 1, students: 31)).Success);
        }

        [Fact]
        public void ComputerLab_StudentsAboveWorkstations_IsRejected_EvenWithinCapacity()
        {
            var (service, _, _) = CreateService();
            // CL-202 seats 40 but has only 35 workstations.
            Assert.False(service.TryCreateBooking("CL-202", Session(0, 1, students: 36)).Success);
            Assert.True(service.TryCreateBooking("CL-202", Session(0, 1, students: 35)).Success);
        }

        [Fact]
        public void Bookings_SurviveSaveAndReload_AndStillBlockOverlaps()
        {
            string file = Path.Combine(Path.GetTempPath(), $"bookings-{Guid.NewGuid():N}.json");
            try
            {
                var storage = new JsonStorageService();
                var (service, bookings, rooms) = CreateService();
                var created = service.TryCreateBooking("SR-101", new TeachingSession("CS101", 20, new[] { Equipment.Projector }, Nine, Nine.AddHours(2)));
                Assert.True(created.Success);
                Assert.True(storage.SaveBookings(file, bookings.GetAll()).Success);

                // Simulate a restart: brand-new repositories and service, data only from disk.
                var (ok, loaded, _) = storage.LoadBookings(file);
                Assert.True(ok);
                Assert.NotNull(loaded);
                var restartedBookings = new InMemoryBookingRepository();
                restartedBookings.ReplaceAll(loaded!);
                var restarted = new BookingService(rooms, restartedBookings);

                var reloaded = Assert.Single(restartedBookings.GetAll());
                Assert.Equal(created.Booking!.BookingId, reloaded.BookingId);
                Assert.Equal("SR-101", reloaded.RoomId);
                Assert.Equal("CS101", reloaded.Session.ModuleName);
                Assert.Equal(Nine, reloaded.Session.Start);
                Assert.Contains(Equipment.Projector, reloaded.Session.RequiredEquipment);

                Assert.False(restarted.TryCreateBooking("SR-101", Session(1, 2)).Success);
                Assert.True(restarted.TryCreateBooking("SR-101", Session(2, 1)).Success);
            }
            finally
            {
                if (File.Exists(file)) File.Delete(file);
            }
        }

        [Theory]
        [InlineData("{ not json }}}")]
        [InlineData("   ")]
        public void LoadBookings_MalformedOrEmptyFile_FailsGracefully(string content)
        {
            string file = Path.Combine(Path.GetTempPath(), $"bookings-{Guid.NewGuid():N}.json");
            try
            {
                File.WriteAllText(file, content);
                var (ok, list, message) = new JsonStorageService().LoadBookings(file);
                Assert.False(ok);
                Assert.Null(list);
                Assert.False(string.IsNullOrWhiteSpace(message));
            }
            finally
            {
                if (File.Exists(file)) File.Delete(file);
            }
        }

        [Fact]
        public void LoadBookings_MissingFile_FailsGracefully()
        {
            string file = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.json");
            var (ok, list, _) = new JsonStorageService().LoadBookings(file);
            Assert.False(ok);
            Assert.Null(list);
        }
    }
}