using System;
using System.IO;
using UniversityRoomBookingSystem.Repositories;
using UniversityRoomBookingSystem.Services;
using UniversityRoomBookingSystem.UI;

namespace UniversityRoomBookingSystem
{
    public static class Program
    {
        /// <summary>Composition root: builds the object graph and hands control to the console application.</summary>
        public static void Main()
        {
            IRoomRepository roomRepository = new InMemoryRoomRepository();
            IBookingRepository bookingRepository = new InMemoryBookingRepository();
            var logger = new FileLogger();

            var dataManager = new DataManager(
                roomRepository,
                bookingRepository,
                new JsonStorageService(),
                logger,
                Path.Combine(AppContext.BaseDirectory, "rooms.json"),
                Path.Combine(AppContext.BaseDirectory, "bookings.json"));

            var application = new ConsoleApplication(
                new ConsoleView(),
                roomRepository,
                bookingRepository,
                new BookingService(roomRepository, bookingRepository),
                dataManager,
                logger);

            application.Run();
        }
    }
}