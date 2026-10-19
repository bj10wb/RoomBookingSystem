using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UniversityRoomBookingSystem.Models;
using UniversityRoomBookingSystem.Repositories;
using UniversityRoomBookingSystem.Services;

namespace UniversityRoomBookingSystem
{
    public class Program
    {
        private static readonly IRoomRepository RoomRepo = new InMemoryRoomRepository();
        private static readonly IBookingRepository BookingRepo = new InMemoryBookingRepository();
        private static readonly BookingService BookingSvc = new(RoomRepo, BookingRepo);
        private static readonly FileLogger Logger = new();
        private static readonly JsonStorageService Storage = new();

        public static readonly string RoomsFile = Path.Combine(AppContext.BaseDirectory, "rooms.json");
        public static readonly string BookingsFile = Path.Combine(AppContext.BaseDirectory, "bookings.json");

        private const int WId = 8, WType = 15, WName = 26, WCap = 5, WInfo = 44;

        public static void Main(string[] args)
        {
            try { Console.OutputEncoding = Encoding.UTF8; } catch { /* non-fatal: fall back to default encoding */ }
            try { Console.Title = "University Room Booking System"; } catch { /* no console title when redirected */ }

            Logger.Log("System started.");
            InitializeData();

            try
            {
                RunMenu();
            }
            catch (EndOfStreamException)
            {
                // Input was closed (Ctrl+Z / Ctrl+D / piped input ended). Exit cleanly instead of looping forever.
                Console.WriteLine();
                PrintWarning("Input closed. Saving and exiting.");
            }

            SaveData();
            Logger.Log("System shutdown.");
            Console.WriteLine();
            PrintSuccess("Goodbye!");
        }

        private static void RunMenu()
        {
            while (true)
            {
                DrawMainMenu();
                string choice = ReadLineOrThrow("  Select an option [0-8]: ").Trim();
                Console.WriteLine();

                switch (choice)
                {
                    case "1": DisplayRooms(RoomRepo.GetAll(), "All rooms"); break;
                    case "2": CreateBookingPrompt(); break;
                    case "3": SearchRoomsPrompt(); break;
                    case "4": SortRoomsByCapacity(); break;
                    case "5": SortRoomsByName(); break;
                    case "6": DisplayBookings(); break;
                    case "7": SaveData(); break;
                    case "8": LoadData(); break;
                    case "0": return;
                    default: PrintError("Invalid option. Please enter a number between 0 and 8."); break;
                }
            }
        }

        private static void DrawMainMenu()
        {
            DrawHeader("UNIVERSITY ROOM BOOKING SYSTEM", "CET2007 · Systems Design and Development");

            DrawMenuGroup("ROOMS");
            DrawMenuItem("1", "List all rooms");
            DrawMenuItem("3", "Search rooms", "ID, name, type or equipment");
            DrawMenuItem("4", "Sort by capacity", "manual QuickSort");
            DrawMenuItem("5", "Sort by name", "manual Insertion Sort");

            DrawMenuGroup("BOOKINGS");
            DrawMenuItem("2", "Create a teaching booking");
            DrawMenuItem("6", "View all bookings");

            DrawMenuGroup("DATA");
            DrawMenuItem("7", "Save data to JSON");
            DrawMenuItem("8", "Reload data from JSON");

            DrawMenuGroup("SYSTEM");
            DrawMenuItem("0", "Save and exit");
            Console.WriteLine();
        }

        private static void InitializeData()
        {
            if (!File.Exists(RoomsFile))
            {
                SeedDefaultRooms();
                SaveData();
                return;
            }

            var (roomsOk, bookingsOk) = LoadData();

            // A corrupt or unreadable file must never be silently overwritten on exit.
            if (!roomsOk)
            {
                BackupUnreadableFile(RoomsFile);
                SeedDefaultRooms();
                PrintWarning("Default rooms restored. The unreadable rooms file was backed up.");
            }
            if (!bookingsOk && File.Exists(BookingsFile))
            {
                BackupUnreadableFile(BookingsFile);
            }
        }

        private static void SeedDefaultRooms()
        {
            RoomRepo.ReplaceAll(new Room[]
            {
                new StandardRoom("SR-101", "Seminar Room 101", 30, new[] { Equipment.Projector, Equipment.Whiteboard }),
                new StandardRoom("SR-102", "Small Tutorial Room", 15, new[] { Equipment.Whiteboard }),
                new ComputerLab("CL-201", "Software Engineering Lab", 25, 25, new[] { Equipment.Computers, Equipment.Projector }),
                new ComputerLab("CL-202", "Data Science Lab", 40, 35, new[] { Equipment.Computers, Equipment.Projector, Equipment.Whiteboard }),
                new LectureTheatre("LT-A", "Main Lecture Theatre", 150, new[] { Equipment.Projector, Equipment.Microphone, Equipment.LectureRecording }),
                new LectureTheatre("LT-B", "Faculty Auditorium", 80, new[] { Equipment.Projector, Equipment.Microphone, Equipment.VideoConferencing })
            });
            Logger.Log("Default rooms seeded.");
        }

        private static void BackupUnreadableFile(string path)
        {
            try
            {
                if (!File.Exists(path)) return;
                string backup = $"{path}.corrupt-{DateTime.Now:yyyyMMdd-HHmmss}";
                File.Copy(path, backup, overwrite: true);
                Logger.Log($"Unreadable file backed up to '{backup}'.", "WARN");
                PrintWarning($"Backed up unreadable file to {Path.GetFileName(backup)}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Could not back up '{path}': {ex.Message}", "ERROR");
            }
        }

        private static void DisplayRooms(IEnumerable<Room> rooms, string title)
        {
            var displayedRooms = rooms.ToList();
            PrintSectionTitle(title);

            if (displayedRooms.Count == 0)
            {
                PrintWarning("No rooms to display.");
                return;
            }

            PrintRule();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  {"ID",-WId} │ {"Type",-WType} │ {"Name",-WName} │ {"Cap",-WCap} │ {"Details",-WInfo}");
            Console.ResetColor();
            PrintRule();

            foreach (var room in displayedRooms)
            {
                string equipment = room.Equipment.Count == 0 ? "no equipment" : string.Join(", ", room.Equipment);
                string details = room is ComputerLab lab ? $"{lab.Workstations} PCs · {equipment}" : equipment;
                Console.WriteLine($"  {Clip(room.Id, WId),-WId} │ {Clip(room.RoomType, WType),-WType} │ {Clip(room.Name, WName),-WName} │ {room.Capacity,-WCap} │ {Clip(details, WInfo),-WInfo}");
            }
            PrintRule();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  {displayedRooms.Count} room(s)");
            Console.ResetColor();
        }

        private static void SearchRoomsPrompt()
        {
            string query = ReadLineOrThrow("  Search by ID, name, room type or equipment: ").Trim();
            if (string.IsNullOrWhiteSpace(query))
            {
                PrintWarning("Search term cannot be empty.");
                return;
            }

            var matches = ManualAlgorithms.SearchRooms(RoomRepo.GetAll().ToList(), query);
            DisplayRooms(matches, $"Search results for \"{query}\"");
        }

        private static void SortRoomsByCapacity()
        {
            var roomArray = RoomRepo.GetAll().ToArray();
            ManualAlgorithms.QuickSortByCapacity(roomArray, 0, roomArray.Length - 1, ascending: true);
            DisplayRooms(roomArray, "Rooms sorted by capacity (ascending, QuickSort)");
        }

        private static void SortRoomsByName()
        {
            var roomArray = RoomRepo.GetAll().ToArray();
            ManualAlgorithms.InsertionSortByName(roomArray);
            DisplayRooms(roomArray, "Rooms sorted by name (Insertion Sort)");
        }

        private static void CreateBookingPrompt()
        {
            PrintSectionTitle("New teaching booking");
            DisplayRooms(RoomRepo.GetAll(), "Available rooms");
            Console.WriteLine();

            string roomIdInput = ReadLineOrThrow("  Room ID: ").Trim();
            if (string.IsNullOrWhiteSpace(roomIdInput))
            {
                PrintError("Room ID cannot be empty.");
                return;
            }

            var room = RoomRepo.GetById(roomIdInput);
            if (room is null)
            {
                PrintError($"Room '{roomIdInput}' was not found. Use an ID from the table above (e.g. SR-101).");
                Logger.Log($"Booking rejected: Room {roomIdInput}, Reason: Room not found.", "WARN");
                return;
            }

            string moduleName = ReadLineOrThrow("  Module name: ").Trim();
            if (string.IsNullOrWhiteSpace(moduleName))
            {
                PrintError("Module name cannot be empty.");
                return;
            }

            int studentCount = ReadInt("  Student count (positive whole number): ", 1, int.MaxValue, "Invalid student count. Please enter a positive whole number.");

            DateTime date;
            while (true)
            {
                string input = ReadLineOrThrow("  Date (yyyy-MM-dd, blank for today): ").Trim();
                if (string.IsNullOrEmpty(input)) { date = DateTime.Today; break; }
                if (DateTime.TryParseExact(input, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date)) break;
                PrintError("Invalid date format. Use yyyy-MM-dd (e.g. 2026-10-20).");
            }

            int startHour = ReadInt("  Start hour (0-23): ", 0, 23, "Invalid start hour. Please enter a number between 0 and 23.");
            int duration = ReadInt("  Duration in hours (1-12): ", 1, 12, "Invalid duration. Please enter between 1 and 12 hours.");

            var selectedEquipment = SelectEquipmentPrompt();

            DateTime start = date.AddHours(startHour);
            DateTime end = start.AddHours(duration);

            TeachingSession session;
            try
            {
                session = new TeachingSession(moduleName, studentCount, selectedEquipment, start, end);
            }
            catch (Exception ex)
            {
                PrintError($"Session error: {ex.Message}");
                Logger.Log($"Booking session creation failed: {ex.Message}", "WARN");
                return;
            }

            // Pass the canonical room ID so stored bookings always match the room's own ID casing.
            var bookingResult = BookingSvc.TryCreateBooking(room.Id, session);
            Console.WriteLine();
            if (bookingResult.Success)
            {
                PrintSuccess($"Booking confirmed: {room.Name} ({room.Id}), {start:yyyy-MM-dd HH:mm}–{end:HH:mm}");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"    Reference: {bookingResult.Booking!.BookingId.ToString("N")[..8].ToUpperInvariant()}");
                Console.ResetColor();
                Logger.Log($"Booking created: ID {bookingResult.Booking.BookingId} for Room {room.Id} ({moduleName})");
            }
            else
            {
                PrintError($"Booking rejected: {bookingResult.Error}");
                Logger.Log($"Booking rejected: Room {room.Id}, Reason: {bookingResult.Error}", "WARN");
            }
        }

        private static HashSet<Equipment> SelectEquipmentPrompt()
        {
            var equipmentOptions = (Equipment[])Enum.GetValues(typeof(Equipment));

            while (true)
            {
                var selectedEquipment = new HashSet<Equipment>();

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("  Required equipment (numbers or names, comma-separated; Enter for none)");
                Console.ResetColor();
                for (int i = 0; i < equipmentOptions.Length; i++)
                {
                    DrawMenuItem((i + 1).ToString(), equipmentOptions[i].ToString());
                }

                string input = ReadLineOrThrow("  Choice(s): ");
                if (string.IsNullOrWhiteSpace(input))
                {
                    return selectedEquipment;
                }

                var choices = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var invalidChoices = new List<string>();

                foreach (var choice in choices)
                {
                    if (int.TryParse(choice, out int optionNumber))
                    {
                        if (optionNumber >= 1 && optionNumber <= equipmentOptions.Length) selectedEquipment.Add(equipmentOptions[optionNumber - 1]);
                        else invalidChoices.Add(choice);
                        continue;
                    }

                    // Enum.TryParse also accepts numeric strings; only accept real names here.
                    if (Enum.GetNames<Equipment>().Any(name => string.Equals(name, choice, StringComparison.OrdinalIgnoreCase)) &&
                        Enum.TryParse<Equipment>(choice, ignoreCase: true, out var equipment))
                    {
                        selectedEquipment.Add(equipment);
                        continue;
                    }

                    invalidChoices.Add(choice);
                }

                if (invalidChoices.Count > 0)
                {
                    PrintError($"Invalid equipment choices: {string.Join(", ", invalidChoices)}. Use numbers (e.g. 1,4) or names (e.g. Projector,Microphone).");
                    continue;
                }

                return selectedEquipment;
            }
        }

        private static void DisplayBookings()
        {
            var bookings = BookingRepo.GetAll().OrderBy(booking => booking.Session.Start).ToList();
            PrintSectionTitle("All bookings");

            if (bookings.Count == 0)
            {
                PrintWarning("No bookings registered yet. Choose option 2 to create one.");
                return;
            }

            const int wRef = 8, wRoom = 8, wModule = 20, wStudents = 8, wWindow = 24;
            PrintRule();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  {"Ref",-wRef} │ {"Room",-wRoom} │ {"Module",-wModule} │ {"Students",-wStudents} │ {"Time window",-wWindow}");
            Console.ResetColor();
            PrintRule();

            foreach (var booking in bookings)
            {
                string reference = booking.BookingId.ToString("N")[..8].ToUpperInvariant();
                string window = $"{booking.Session.Start:yyyy-MM-dd HH:mm}–{booking.Session.End:HH:mm}";
                Console.WriteLine($"  {reference,-wRef} │ {Clip(booking.RoomId, wRoom),-wRoom} │ {Clip(booking.Session.ModuleName, wModule),-wModule} │ {booking.Session.StudentCount,-wStudents} │ {Clip(window, wWindow),-wWindow}");
            }
            PrintRule();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  {bookings.Count} booking(s)");
            Console.ResetColor();
        }

        private static void SaveData()
        {
            var roomRes = Storage.SaveRooms(RoomsFile, RoomRepo.GetAll());
            var bookRes = Storage.SaveBookings(BookingsFile, BookingRepo.GetAll());

            if (roomRes.Success && bookRes.Success)
            {
                PrintSuccess("Data saved to JSON files.");
                Logger.Log("State successfully saved to JSON.");
            }
            else
            {
                PrintError($"Save problem: {roomRes.Message} | {bookRes.Message}");
                Logger.Log($"Save warning: {roomRes.Message} | {bookRes.Message}", "ERROR");
            }
        }

        private static (bool RoomsOk, bool BookingsOk) LoadData()
        {
            var (roomSuccess, loadedRooms, roomMsg) = Storage.LoadRooms(RoomsFile);
            var (bookSuccess, loadedBookings, bookMsg) = Storage.LoadBookings(BookingsFile);

            bool roomsOk = roomSuccess && loadedRooms != null;
            bool bookingsOk = bookSuccess && loadedBookings != null;

            if (roomsOk)
            {
                RoomRepo.ReplaceAll(loadedRooms!);
                PrintSuccess($"Loaded {loadedRooms!.Count} rooms.");
            }
            else
            {
                PrintWarning($"Rooms not loaded: {roomMsg} (keeping current in-memory rooms).");
                Logger.Log($"Room load failed: {roomMsg}", "WARN");
            }

            if (bookingsOk)
            {
                BookingRepo.ReplaceAll(loadedBookings!);
                PrintSuccess($"Loaded {loadedBookings!.Count} bookings.");
            }
            else
            {
                PrintWarning($"Bookings not loaded: {bookMsg} (keeping current in-memory bookings).");
                Logger.Log($"Booking load failed: {bookMsg}", "WARN");
            }

            return (roomsOk, bookingsOk);
        }

        private static string ReadLineOrThrow(string prompt)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(prompt);
            Console.ResetColor();
            string? line = Console.ReadLine();
            if (line is null) throw new EndOfStreamException("Console input was closed.");
            return line;
        }

        private static int ReadInt(string prompt, int min, int max, string errorMessage)
        {
            while (true)
            {
                if (int.TryParse(ReadLineOrThrow(prompt).Trim(), out int value) && value >= min && value <= max)
                    return value;
                PrintError(errorMessage);
            }
        }

        private static void PrintRule()
        {
            int width = WId + WType + WName + WCap + WInfo + 12;
            string rule = new string('─', width);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  {rule}");
            Console.ResetColor();
        }

        private static void DrawHeader(string title, string subtitle)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  ╭────────────────────────────────────────────────────────────────────╮");
            Console.WriteLine($"  │ {title.PadRight(66)} │");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  │ {subtitle.PadRight(66)} │");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  ╰────────────────────────────────────────────────────────────────────╯");
            Console.ResetColor();
        }

        private static void DrawMenuGroup(string title)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"  {title}");
            Console.ResetColor();
        }

        private static void DrawMenuItem(string number, string label, string? hint = null)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"    [{number}] ");
            Console.ResetColor();
            Console.Write(label);
            if (!string.IsNullOrWhiteSpace(hint))
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"  ·  {hint}");
                Console.ResetColor();
            }
            Console.WriteLine();
        }

        private static void PrintSectionTitle(string title)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  {title}");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  {new string('─', Math.Min(Math.Max(title.Length, 18), 80))}");
            Console.ResetColor();
        }

        private static void PrintSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ✓ {message}");
            Console.ResetColor();
        }

        private static void PrintError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ✗ {message}");
            Console.ResetColor();
        }

        private static void PrintWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"  ! {message}");
            Console.ResetColor();
        }

        private static string Clip(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value.Length <= maxLength
                ? value
                : value[..Math.Max(0, maxLength - 1)] + "…";
        }
    }
}