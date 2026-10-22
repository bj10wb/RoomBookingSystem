using System;
using System.IO;
using System.Linq;
using UniversityRoomBookingSystem.Models;
using UniversityRoomBookingSystem.Repositories;
using UniversityRoomBookingSystem.Services;

namespace UniversityRoomBookingSystem.UI
{
    public class ConsoleApplication
    {
        private readonly ConsoleView _view;
        private readonly IRoomRepository _roomRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly BookingService _bookingService;
        private readonly DataManager _dataManager;
        private readonly FileLogger _logger;

        public ConsoleApplication(
            ConsoleView view,
            IRoomRepository roomRepository,
            IBookingRepository bookingRepository,
            BookingService bookingService,
            DataManager dataManager,
            FileLogger logger)
        {
            _view = view;
            _roomRepository = roomRepository;
            _bookingRepository = bookingRepository;
            _bookingService = bookingService;
            _dataManager = dataManager;
            _logger = logger;
        }

        public void Run()
        {
            _view.Configure();
            _logger.Log("System started.");
            _view.ShowMessages(_dataManager.Initialize());

            try
            {
                RunMenuLoop();
            }
            catch (EndOfStreamException)
            {
                _view.PrintBlankLine();
                _view.PrintWarning("Input closed. Saving and exiting.");
            }

            _view.ShowMessages(_dataManager.Save());
            _logger.Log("System shutdown.");
            _view.PrintBlankLine();
            _view.PrintSuccess("Goodbye!");
        }

        private void RunMenuLoop()
        {
            while (true)
            {
                _view.ShowMainMenu();
                string choice = _view.ReadLine("  Select an option [0-9]: ").Trim();
                _view.PrintBlankLine();

                switch (choice)
                {
                    case "1": _view.ShowRooms(_roomRepository.GetAll(), "All rooms"); break;
                    case "2": CheckSuitability(); break;
                    case "3": CreateBooking(); break;
                    case "4": SearchRooms(); break;
                    case "5": SortByCapacity(); break;
                    case "6": SortByName(); break;
                    case "7": _view.ShowBookings(_bookingRepository.GetAll()); break;
                    case "8": _view.ShowMessages(_dataManager.Save()); break;
                    case "9": _view.ShowMessages(_dataManager.Reload()); break;
                    case "0": return;
                    default: _view.PrintError("Invalid option. Please enter a number between 0 and 9."); break;
                }
            }
        }


        private void CheckSuitability()
        {
            _view.PrintSectionTitle("Check room suitability");

            int studentCount = _view.ReadInt("  Number of students: ", 1, int.MaxValue, "Invalid student count. Please enter a positive whole number.");
            var equipment = _view.ReadEquipment();

            DateTime? date = _view.ReadDate("  Date to check availability (yyyy-MM-dd, blank to skip): ", allowBlank: true);
            DateTime? start = null, end = null;
            if (date.HasValue)
            {
                var startTime = _view.ReadTime("  Start time (HH:mm): ");
                var endTime = _view.ReadEndTime("  End time (HH:mm): ", startTime);
                start = date.Value + startTime;
                end = date.Value + endTime;
            }

            var suitableRooms = _bookingService.FindSuitableRooms(studentCount, equipment, start, end);
            string needs = equipment.Count == 0 ? "no special equipment" : string.Join(", ", equipment);
            string when = start.HasValue ? $", free {start:yyyy-MM-dd HH:mm}–{end:HH:mm}" : string.Empty;

            _view.ShowRooms(suitableRooms, $"Suitable rooms for {studentCount} students ({needs}{when})");
            if (suitableRooms.Count > 0)
            {
                _view.PrintNote("Smallest suitable room first.");
            }
            _logger.Log($"Suitability check: {studentCount} students, equipment [{needs}]{when} -> {suitableRooms.Count} room(s).");
        }

        private void SearchRooms()
        {
            string query = _view.ReadLine("  Search by ID, name, room type or equipment: ").Trim();
            if (string.IsNullOrWhiteSpace(query))
            {
                _view.PrintWarning("Search term cannot be empty.");
                return;
            }

            var matches = ManualAlgorithms.SearchRooms(_roomRepository.GetAll().ToList(), query);
            _view.ShowRooms(matches, $"Search results for \"{query}\"");
        }

        private void SortByCapacity()
        {
            var rooms = _roomRepository.GetAll().ToArray();
            ManualAlgorithms.QuickSortByCapacity(rooms, 0, rooms.Length - 1, ascending: true);
            _view.ShowRooms(rooms, "Rooms sorted by capacity (ascending, QuickSort)");
        }

        private void SortByName()
        {
            var rooms = _roomRepository.GetAll().ToArray();
            ManualAlgorithms.InsertionSortByName(rooms);
            _view.ShowRooms(rooms, "Rooms sorted by name (Insertion Sort)");
        }


        private void CreateBooking()
        {
            _view.PrintSectionTitle("New teaching booking");
            _view.ShowRooms(_roomRepository.GetAll(), "Available rooms");
            _view.PrintBlankLine();

            string roomIdInput = _view.ReadLine("  Room ID: ").Trim();
            if (string.IsNullOrWhiteSpace(roomIdInput))
            {
                _view.PrintError("Room ID cannot be empty.");
                return;
            }

            var room = _roomRepository.GetById(roomIdInput);
            if (room is null)
            {
                _view.PrintError($"Room '{roomIdInput}' was not found. Use an ID from the table above (e.g. SR-101).");
                _logger.Log($"Booking rejected: Room {roomIdInput}, Reason: Room not found.", "WARN");
                return;
            }

            string moduleName = _view.ReadLine("  Module name: ").Trim();
            if (string.IsNullOrWhiteSpace(moduleName))
            {
                _view.PrintError("Module name cannot be empty.");
                return;
            }

            int studentCount = _view.ReadInt("  Student count (positive whole number): ", 1, int.MaxValue, "Invalid student count. Please enter a positive whole number.");
            DateTime date = _view.ReadDate("  Date (yyyy-MM-dd, blank for today): ", allowBlank: true) ?? DateTime.Today;
            var startTime = _view.ReadTime("  Start time (HH:mm): ");
            var endTime = _view.ReadEndTime("  End time (HH:mm): ", startTime);
            var equipment = _view.ReadEquipment();

            DateTime start = date + startTime;
            DateTime end = date + endTime;

            TeachingSession session;
            try
            {
                session = new TeachingSession(moduleName, studentCount, equipment, start, end);
            }
            catch (Exception ex)
            {
                _view.PrintError($"Session error: {ex.Message}");
                _logger.Log($"Booking session creation failed: {ex.Message}", "WARN");
                return;
            }

            var result = _bookingService.TryCreateBooking(room.Id, session);
            _view.PrintBlankLine();
            if (result.Success)
            {
                _view.PrintSuccess($"Booking confirmed: {room.Name} ({room.Id}), {start:yyyy-MM-dd HH:mm}–{end:HH:mm}");
                _view.PrintNote($"Reference: {ConsoleView.ShortReference(result.Booking!)}");
                _logger.Log($"Booking created: ID {result.Booking!.BookingId} for Room {room.Id} ({moduleName})");
            }
            else
            {
                _view.PrintError($"Booking rejected: {result.Error}");
                _logger.Log($"Booking rejected: Room {room.Id}, Reason: {result.Error}", "WARN");
            }
        }
    }
}