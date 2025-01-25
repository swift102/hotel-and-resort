namespace hotel_and_resort.DTOs
{
    // DTO for getting room details (read-only)
    public class RoomReadDTO
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Price { get; set; }
        public int Capacity { get; set; }
        public string Features { get; set; }
        public bool IsAvailable { get; set; }
    }

    // DTO for adding a room (write-only)
    public class RoomCreateDTO
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Price { get; set; }
        public int Capacity { get; set; }
        public string Features { get; set; }
        public bool IsAvailable { get; set; }
    }

    // DTO for updating room details (write-only)
    public class RoomUpdateDTO
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Price { get; set; }
        public int Capacity { get; set; }
        public string Features { get; set; }
        public bool IsAvailable { get; set; }
    }
}
