using System.Collections.Generic;
using UniversityRoomBookingSystem.Models;

namespace UniversityRoomBookingSystem.Repositories
{
    public interface IRoomRepository
    {
        IEnumerable<Room> GetAll();
        Room? GetById(string id);
        void Add(Room room);
        bool Remove(string id);
        void Clear();
        void ReplaceAll(IEnumerable<Room> rooms);
    }
}