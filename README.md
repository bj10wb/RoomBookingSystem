# University Room Booking System

CET2007 Systems. Development: a C# (.NET 10) console application for booking university teaching rooms.

## Features

- Six pre‑loaded rooms: 2 rooms, 2 computer labs, 2 lecture theatres

- Create and view bookings

- Search rooms by ID, name, room type or equipment (manual linear search)

- Sort rooms by capacity (manual QuickSort) and by name (manual Insertion Sort)

- Validation of student count against room capacity, required equipment and computer‑lab workstations

- Overlapping bookings in the same room are rejected; back‑to‑back bookings are allowed

- Rooms and bookings are saved to JSON and reloaded on start‑up

- Activity logging to a text file and graceful handling of invalid or corrupt input and data files

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

- Visual Studio 2026 or any editor/terminal

## Run

```

git clone https://github.com/bj10wb/UniversityRoomBookingSystem.git

cd UniversityRoomBookingSystem

dotnet build

dotnet run --project UniversityRoomBookingSystem

```

Or open `UniversityRoomBookingSystem.slnx` in Visual Studio. Press F5.

## Test

```

dotnet test

```

## Using the application

| Option | Action |

| ------ | ------ |

| 1 | List all rooms |

| 2 | Create a teaching booking |

| 3 | Search rooms by ID, name, type or equipment

| 4 | Sort rooms by capacity |

| 5 | Sort rooms by name |

| 6 | View all bookings |

| 7 | Save data to JSON |

| 8 | Reload data from JSON |

| 0 | Save and exit |

Dates use `yyyy‑MM‑dd`. Equipment can be chosen by number or name for example `1,4` or `Projector,Microphone`.

##. Log files

`rooms.json` `bookings.json` and `activity_log.txt` are created next to the executable for example `UniversityRoomBookingSystem/bin/Debug/net10.0/`. Debug and Release builds use folders so they have separate data. Deleting the folder or running `dotnet clean` resets the data.

If `rooms.json` or `bookings.json` cannot be read the application copies it to `<name>.corrupt‑<timestamp>` before continuing. It is never silently overwritten.

## Design

Class and sequence diagrams are, in [docs/diagrams.md](docs/diagrams.md).

- `Models`: `Room` (abstract) `StandardRoom` `ComputerLab` `LectureTheatre` `TeachingSession` `Booking` `Equipment`

- `Repositories`: `IRoomRepository` `IBookingRepository` and in‑memory implementations

- `Services`: `BookingService` (business rules) `JsonStorageService` (persistence) `FileLogger` `ManualAlgorithms`

- `Program.cs`: console menu and display

## AI use



