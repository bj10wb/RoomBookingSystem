# University Room Booking System

CET2007 Systems Design and Development. A C# (.NET 10) console application that helps staff find suitable teaching rooms and manage room bookings.

## Features

- Six rooms of three types: 2 Standard Rooms, 2 Computer Labs and 2 Lecture Theatres, plus three sample bookings on first run
- List rooms, with ID, type, name, capacity and equipment
- **Check room suitability**: enter the number of students, required equipment and (optionally) a date and time, and see every room that fits, smallest first
- **Create bookings**: capacity, required equipment, computer-lab workstations and overlapping bookings are all validated; back-to-back bookings are allowed
- Search rooms by ID, name, room type or equipment (manual linear search)
- Sort rooms by capacity (manual QuickSort) and by name (manual Insertion Sort)
- View all bookings
- Save and load rooms and bookings as JSON; activity is written to a text log
- Invalid input, missing files and malformed or corrupt data are handled without crashing
- JSON files are human-readable (equipment is stored by name, for example `"Projector"`)

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Visual Studio 2026 (used for development)

## Run

```
git clone https://github.com/bj10wb/RoomBookingSystem.git
cd RoomBookingSystem
dotnet build
dotnet run --project UniversityRoomBookingSystem
```

Or open `UniversityRoomBookingSystem.slnx` in Visual Studio and press F5.

## Test

```
dotnet test
```

## Using the application

| Option | Action |
| ------ | ------ |
| 1 | List all rooms |
| 2 | Check room suitability |
| 3 | Create a teaching booking |
| 4 | Search rooms by ID, name, type or equipment |
| 5 | Sort rooms by capacity |
| 6 | Sort rooms by name |
| 7 | View all bookings |
| 8 | Save data to JSON |
| 9 | Reload data from JSON |
| 0 | Save and exit |

Dates use `yyyy-MM-dd` and times use 24-hour `HH:mm`. Equipment can be chosen by number or name, for example `1,4` or `Projector,Microphone`.

## Data and log files

`rooms.json`, `bookings.json` and `activity_log.txt` are created next to the executable, for example `UniversityRoomBookingSystem/bin/Debug/net10.0/`. Debug and Release builds use separate folders, so they have separate data. Deleting the folder, or running `dotnet clean`, resets the data to the defaults. Starter copies of these files live in Data/ and are copied next to the executable when you build.

If `rooms.json` or `bookings.json` cannot be read, the application copies it to `<name>.corrupt-<timestamp>` before continuing, so the original is never silently overwritten.

## Sample data

On the first run the application creates the six rooms and three sample bookings (2026-11-02 and 2026-11-03), for example `CL-201` is booked 09:00–11:00 on 2026-11-02. Booking an overlapping time in that room is rejected.

## Design

Class and sequence diagrams are in [docs/diagrams.md](docs/diagrams.md). GitHub renders them automatically.

| Folder | Contents |
| ------ | -------- |
| `Models` | `Room` (abstract), `StandardRoom`, `ComputerLab`, `LectureTheatre`, `TeachingSession`, `Booking`, `Equipment` |
| `Repositories` | `IRoomRepository`, `IBookingRepository` and their in-memory implementations |
| `Services` | `BookingService` (business rules), `DataManager` (start-up, save, reload, recovery), `JsonStorageService`, `FileLogger`, `ManualAlgorithms` |
| `UI` | `ConsoleApplication` (menu controller) and `ConsoleView` (all console input and output) |
| `Program.cs` | Composition root only: creates the objects and starts the application |

### Design choices

- **Inheritance and polymorphism:** `Room` is abstract. `ComputerLab` overrides `IsSuitable` to also check workstations, and every subclass overrides `RoomType`. `BookingService.FindSuitableRooms` calls `IsSuitable` on each room, so the correct rule is chosen at run time.
- **Repository pattern:** `IRoomRepository` and `IBookingRepository` hide how records are stored. Services depend on the interfaces, not the in-memory classes, so tests can substitute their own.
- **Dependency injection:** objects receive their dependencies through constructors. `Program.cs` is the single place where concrete classes are created.
- **Separation of concerns:** the UI never contains business rules, and the services never write to the console. Services return results (`StatusMessage`, tuples) and the UI decides how to show them.
- **Error handling:** expected failures such as a rejected booking return a result rather than throwing. File problems are caught, reported and logged.

## AI use

(Complete this section to match the declaration your module requires.)
