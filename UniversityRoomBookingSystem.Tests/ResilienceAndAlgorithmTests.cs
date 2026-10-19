using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversityRoomBookingSystem.Models;
using UniversityRoomBookingSystem.Repositories;
using UniversityRoomBookingSystem.Services;
using Xunit;

namespace UniversityRoomBookingSystem.Tests
{
    public class ResilienceAndAlgorithmTests
    {
        [Fact]
        public void LoadRooms_MalformedJson_FailsGracefullyAndPreservesState()
        {
            var storage = new JsonStorageService();
            string tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "{ this is definitely not valid json }}}");

            try
            {
                var (success, rooms, msg) = storage.LoadRooms(tempFile);
                Assert.False(success);
                Assert.Null(rooms);
                Assert.Contains("Malformed JSON", msg);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void LoadRooms_EmptyFile_ReturnsFailure()
        {
            var storage = new JsonStorageService();
            string tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "   ");

            try
            {
                var (success, rooms, msg) = storage.LoadRooms(tempFile);
                Assert.False(success);
                Assert.Null(rooms);
                Assert.Contains("empty", msg, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void RepeatedReload_PreservesConsistentState()
        {
            var repo = new InMemoryRoomRepository();
            var storage = new JsonStorageService();
            string tempFile = Path.GetTempFileName();

            var originalRooms = new List<Room>
            {
                new StandardRoom("R1", "Room 1", 20),
                new ComputerLab("R2", "Lab 2", 30, 25)
            };

            try
            {
                storage.SaveRooms(tempFile, originalRooms);

                var (s1, r1, _) = storage.LoadRooms(tempFile);
                Assert.True(s1);
                repo.ReplaceAll(r1!);
                Assert.Equal(2, repo.GetAll().Count());

                var (s2, r2, _) = storage.LoadRooms(tempFile);
                Assert.True(s2);
                repo.ReplaceAll(r2!);
                Assert.Equal(2, repo.GetAll().Count());
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void FileLogger_InvalidPath_RecordsObservableErrorWithoutCrashing()
        {
            var logger = new FileLogger("??::invalid_path/<>\\log.txt");
            bool logged = logger.Log("Test message");

            Assert.False(logged);
            Assert.NotNull(logger.LastLoggingError);
        }

        [Fact]
        public void QuickSort_EdgeCases_HandledGracefully()
        {
            Room[] empty = Array.Empty<Room>();
            ManualAlgorithms.QuickSortByCapacity(empty, 0, -1);
            Assert.Empty(empty);

            Room[] single = new Room[] { new StandardRoom("S1", "Single", 50) };
            ManualAlgorithms.QuickSortByCapacity(single, 0, 0);
            Assert.Single(single);

            Room[] dupes = new Room[]
            {
                new StandardRoom("D1", "A", 30),
                new StandardRoom("D2", "B", 10),
                new StandardRoom("D3", "C", 30),
                new StandardRoom("D4", "D", 5)
            };
            ManualAlgorithms.QuickSortByCapacity(dupes, 0, dupes.Length - 1, ascending: true);
            Assert.Equal(5, dupes[0].Capacity);
            Assert.Equal(10, dupes[1].Capacity);
            Assert.Equal(30, dupes[2].Capacity);
            Assert.Equal(30, dupes[3].Capacity);
        }

        [Fact]
        public void BinarySearch_EmptyAndMissing_ReturnsNull()
        {
            Assert.Null(ManualAlgorithms.BinarySearchRoomById(null, "ID1"));
            Assert.Null(ManualAlgorithms.BinarySearchRoomById(new List<Room>(), "ID1"));

            var sorted = new List<Room>
            {
                new StandardRoom("A1", "Room A1", 20),
                new StandardRoom("B2", "Room B2", 30),
                new StandardRoom("C3", "Room C3", 40)
            };
            Assert.NotNull(ManualAlgorithms.BinarySearchRoomById(sorted, "B2"));
            Assert.Null(ManualAlgorithms.BinarySearchRoomById(sorted, "Z9"));
        }

        [Fact]
        public void EquipmentSelection_RejectedWhenRoomLacksRequired()
        {
            var roomRepo = new InMemoryRoomRepository();
            var bookingRepo = new InMemoryBookingRepository();
            var service = new BookingService(roomRepo, bookingRepo);

            var room = new StandardRoom("R10", "Room Without Projector", 30, new[] { Equipment.Whiteboard });
            roomRepo.Add(room);

            var start = DateTime.Today.AddHours(10);
            var session = new TeachingSession("CS101", 15, new[] { Equipment.Projector }, start, start.AddHours(1));

            var result = service.TryCreateBooking("R10", session);
            Assert.False(result.Success);
            Assert.Contains("missing required equipment", result.Error, StringComparison.OrdinalIgnoreCase);
        }
    }
}