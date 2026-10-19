using System;
using System.Linq;
using UniversityRoomBookingSystem.Models;
using UniversityRoomBookingSystem.Repositories;
using UniversityRoomBookingSystem.Services;
using Xunit;

namespace UniversityRoomBookingSystem.Tests
{
    public class BookingServiceTests
    {
        [Fact]
        public void Create_ValidBooking_IsSaved()
        {
            var roomRepo = new InMemoryRoomRepository();
            var bookingRepo = new InMemoryBookingRepository();
            var service = new BookingService(roomRepo, bookingRepo);

            var room = new StandardRoom("R1", "Room 1", 30);
            roomRepo.Add(room);

            var start = DateTime.Today.AddHours(9);
            var end = start.AddHours(2);
            var session = new TeachingSession("CS101", 25, null, start, end);

            var result = service.TryCreateBooking(room.Id, session);

            Assert.True(result.Success);
            Assert.NotNull(result.Booking);
            var saved = bookingRepo.GetAll().ToList();
            Assert.Single(saved);
            Assert.Equal(result.Booking.BookingId, saved[0].BookingId);
        }

        [Fact]
        public void Create_BookingRoomNotFound_IsRejected()
        {
            var roomRepo = new InMemoryRoomRepository();
            var bookingRepo = new InMemoryBookingRepository();
            var service = new BookingService(roomRepo, bookingRepo);

            var start = DateTime.Today.AddHours(9);
            var end = start.AddHours(1);
            var session = new TeachingSession("CS101", 10, null, start, end);

            var result = service.TryCreateBooking("nonexistent", session);

            Assert.False(result.Success);
            Assert.Null(result.Booking);
            Assert.Empty(bookingRepo.GetAll());
        }

        [Fact]
        public void Create_StudentCountExceedsCapacity_IsRejected()
        {
            var roomRepo = new InMemoryRoomRepository();
            var bookingRepo = new InMemoryBookingRepository();
            var service = new BookingService(roomRepo, bookingRepo);

            var room = new StandardRoom("R2", "Small Room", 5);
            roomRepo.Add(room);

            var start = DateTime.Today.AddHours(10);
            var end = start.AddHours(1);
            var session = new TeachingSession("CS102", 10, null, start, end);

            var result = service.TryCreateBooking(room.Id, session);

            Assert.False(result.Success);
            Assert.Null(result.Booking);
            Assert.Empty(bookingRepo.GetAll());
        }

        [Fact]
        public void Create_MissingEquipment_IsRejected()
        {
            var roomRepo = new InMemoryRoomRepository();
            var bookingRepo = new InMemoryBookingRepository();
            var service = new BookingService(roomRepo, bookingRepo);

            var room = new StandardRoom("R3", "Room 3", 20);
            roomRepo.Add(room);

            var start = DateTime.Today.AddHours(13);
            var end = start.AddHours(1);
            var session = new TeachingSession("CS103", 15, new[] { Equipment.Projector }, start, end);

            var result = service.TryCreateBooking(room.Id, session);

            Assert.False(result.Success);
            Assert.Null(result.Booking);
            Assert.Empty(bookingRepo.GetAll());
        }

        [Fact]
        public void Create_ComputerLabInsufficientWorkstations_IsRejected()
        {
            var roomRepo = new InMemoryRoomRepository();
            var bookingRepo = new InMemoryBookingRepository();
            var service = new BookingService(roomRepo, bookingRepo);

            var lab = new ComputerLab("L1", "Lab 1", 30, workstations: 10);
            roomRepo.Add(lab);

            var start = DateTime.Today.AddHours(14);
            var end = start.AddHours(2);
            var session = new TeachingSession("CS104", 12, new Equipment[] { Equipment.Computers }, start, end);

            var result = service.TryCreateBooking(lab.Id, session);

            Assert.False(result.Success);
            Assert.Null(result.Booking);
            Assert.Empty(bookingRepo.GetAll());
        }

        [Fact]
        public void Create_OverlappingBooking_IsRejected()
        {
            var roomRepo = new InMemoryRoomRepository();
            var bookingRepo = new InMemoryBookingRepository();
            var service = new BookingService(roomRepo, bookingRepo);

            var room = new StandardRoom("R4", "Room 4", 50);
            roomRepo.Add(room);

            var existingStart = DateTime.Today.AddHours(9);
            var existingEnd = existingStart.AddHours(2);
            var existingSession = new TeachingSession("CS200", 20, null, existingStart, existingEnd);
            var existingBooking = new Booking(room.Id, existingSession);
            bookingRepo.Add(existingBooking);

            var newStart = existingStart.AddHours(1);
            var newEnd = newStart.AddHours(2);
            var newSession = new TeachingSession("CS201", 10, null, newStart, newEnd);

            var result = service.TryCreateBooking(room.Id, newSession);

            Assert.False(result.Success);
            Assert.Null(result.Booking);
            var saved = bookingRepo.GetAll().ToList();
            Assert.Single(saved);
            Assert.Equal(existingBooking.BookingId, saved[0].BookingId);
        }

        [Fact]
        public void Create_AdjacentBooking_IsAccepted()
        {
            var roomRepo = new InMemoryRoomRepository();
            var bookingRepo = new InMemoryBookingRepository();
            var service = new BookingService(roomRepo, bookingRepo);

            var room = new StandardRoom("R5", "Room 5", 40);
            roomRepo.Add(room);

            var existingStart = DateTime.Today.AddHours(8);
            var existingEnd = existingStart.AddHours(1);
            var existingSession = new TeachingSession("CS300", 10, null, existingStart, existingEnd);
            var existingBooking = new Booking(room.Id, existingSession);
            bookingRepo.Add(existingBooking);

            var newStart = existingEnd;
            var newEnd = newStart.AddHours(1);
            var newSession = new TeachingSession("CS301", 8, null, newStart, newEnd);

            var result = service.TryCreateBooking(room.Id, newSession);

            Assert.True(result.Success);
            Assert.NotNull(result.Booking);
            var saved = bookingRepo.GetByRoomId(room.Id).ToList();
            Assert.Equal(2, saved.Count);
        }

        [Fact]
        public void Create_InvalidTimeRange_IsRejected()
        {
            var roomRepo = new InMemoryRoomRepository();
            var bookingRepo = new InMemoryBookingRepository();
            var service = new BookingService(roomRepo, bookingRepo);

            var room = new StandardRoom("R6", "Room 6", 25);
            roomRepo.Add(room);

            var start = DateTime.Today.AddHours(12);
            var end = start; 
            var session = new TeachingSession("CS400", 10, null, start, end);

            var result = service.TryCreateBooking(room.Id, session);

            Assert.False(result.Success);
            Assert.Null(result.Booking);
            Assert.Empty(bookingRepo.GetAll());
        }

        [Fact]
        public void RejectedBooking_DoesNotModifyRepository()
        {
            var roomRepo = new InMemoryRoomRepository();
            var bookingRepo = new InMemoryBookingRepository();
            var service = new BookingService(roomRepo, bookingRepo);

            var room = new StandardRoom("R7", "Room 7", 15);
            roomRepo.Add(room);

            var start = DateTime.Today.AddHours(15);
            var end = start.AddHours(1);
            var session = new TeachingSession("CS500", 100, null, start, end); 

            var before = bookingRepo.GetAll().ToList();
            var result = service.TryCreateBooking(room.Id, session);
            var after = bookingRepo.GetAll().ToList();

            Assert.False(result.Success);
            Assert.Equal(before.Count, after.Count);
        }
    }
}
