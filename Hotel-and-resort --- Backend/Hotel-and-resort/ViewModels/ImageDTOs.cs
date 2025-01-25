namespace hotel_and_resort.DTOs
{
    // DTO for getting image details (read-only)
    public class ImageReadDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ImagePath { get; set; }
        public int RoomID { get; set; }
    }

  

    // DTO for adding an image (write-only)
    public class ImageCreateDTO
    {
        public string Name { get; set; }
        public string ImagePath { get; set; }
        public int RoomID { get; set; }
    }

 


    // DTO for updating an image (write-only)
    public class ImageUpdateDTO
    {
        public string Name { get; set; }
        public string ImagePath { get; set; }
        public int RoomID { get; set; }
    }
}
