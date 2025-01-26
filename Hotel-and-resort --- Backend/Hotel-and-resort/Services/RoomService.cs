using hotel_and_resort.Models;

namespace Hotel_and_resort.Services
{
    public class RoomService
    {
        private readonly Repository _repository;

        public RoomService(Repository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<Room>> GetAvailableRooms(DateTime checkInDate, DateTime checkOutDate)
        {
            // Check room availability based on check-in and check-out dates
            var rooms = await _repository.GetRooms();
            return rooms.Where(r => IsRoomAvailable(r, checkInDate, checkOutDate));
        }

        public decimal CalculateRoomPrice(Room room, int nights)
        {
            // Calculate the total price for the room based on the number of nights
            return room.Price * nights;
        }

        private bool IsRoomAvailable(Room room, DateTime checkInDate, DateTime checkOutDate)
        {
            // Get all reservations for the room
            var reservations = _repository.GetReservationsForRoom(room.ID);

            // Check if the room is available for the given dates
            foreach (var reservation in reservations)
            {
                if (checkInDate < reservation.CheckOut && checkOutDate > reservation.CheckIn)
                {
                    // The room is booked during the requested dates
                    return false;
                }
            }

            // The room is available for the requested dates
            return true;
        }
    }
}
