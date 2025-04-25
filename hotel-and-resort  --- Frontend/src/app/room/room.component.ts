import { Component } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-room',
  templateUrl: './room.component.html',
  styleUrls: ['./room.component.scss']
})
export class RoomComponent {

  constructor(private router: Router) {}

  navigateToRoom(roomType: string) {
    // Navigate to the appropriate room details page
    this.router.navigate(['/rooms', roomType]);
  }

  bookRoom() {
    // Navigate to the booking page
    this.router.navigate(['/book']);
  }

  contactUs() {
    // Navigate to the contact page
    this.router.navigate(['/contact']);
  }

  showPopup: string | null = null;

  showRoomDetails(roomType: string) {
    this.showPopup = roomType;
  }

  selectedRoom: string | null = null; // Tracks the selected room
  currentImage = 0; // Tracks the currently displayed image
   
    // Opens the popup with details for a selected room
    openRoomDetails(roomType: string) {
      this.selectedRoom = roomType; // Set selected room
      this.currentImage = 0; // Reset the slideshow to the first image
    }

     // Dynamically fetch details of the selected room
     get roomDetails() {
      return this.roomData[this.selectedRoom!]; // Access room data safely
    }
  
    // Closes the popup
    closeRoomDetails() {
      this.selectedRoom = null; // Reset selected room
    }
  
 
  
    // Changes the slideshow image based on direction
    changeSlide(direction: number) {
      const images = this.roomDetails.images;
      this.currentImage = (this.currentImage + direction + images.length) % images.length; // Loop back to the start or end
    }

  roomData: { [key: string]: { images: { src: string; alt: string; }[]; description: string; amenities: { bathroom: string[]; bedroom: string[]; other: string[]; }; } } = {
    "luxury-suite": {
      images: [
        { src: 'assets/inside 20.jpg', alt: 'Luxury Suite - View 1' },
        { src: 'assets/inside 21.jpg', alt: 'Luxury Suite - View 2' },
        { src: 'assets/inside 22.jpg', alt: 'Luxury Suite - View 3' },
        { src: 'assets/inside 23.jpg', alt: 'Luxury Suite - View 4' },
        // { src: 'assets/inside 26.jpg', alt: 'Luxury Suite - View 5' },
      ],
      description: "40% larger room with a sitting area and an 8 m² sunny private balcony, providing stunning views and a luxurious stay experience.",
      amenities: {
        bathroom: ["Towels", "Toiletries", "Hairdryer", "En-suite shower room"],
        bedroom: [
          "King or Queen size bed",
          "Soft cotton linen",
          "Dressing gowns",
          "Linens and pillows",
          "Television (Satellite TV)",
          "Desk",
          "Mini fridge",
          "Stylishly furnished",
        ],
        other: [
          "Large private balcony",
          "Air conditioning (cooling/heating)",
          "Wi-Fi",
          "Iron",
          "Safe",
          "Kettle",
          "Wardrobe",
          "Tea/coffee making facilities",
          "iPod dock",
          "Telephone / alarm clock",
          "Minibar",
        ],
      },
    },
    "premium-suite": {
    images: [
      { src: 'assets/pre room.jpg', alt: 'Image 1' },
      { src: 'assets/inside 2.jpg', alt: 'Image 1' },
      { src: 'assets/inside 3.jpg', alt: 'Image 1' },
      { src: 'assets/inside 4.jpg', alt: 'Image 1' },
    ],
    description: "Spacious and elegant suite with a mountain view, bathrobe, minibar, and luxurious bathroom amenities.",
    amenities: {
      bathroom: [
        "Separate tub and shower",
        "Towels",
        "Toiletries",
        "Hairdryer",
        "Bathrobe",
        "Stylishly furnished bathroom",
      ],
      bedroom: [
        "King or Twin beds",
        "Soft cotton linen",
        "Pillows",
        "Television (DSTV/Satellite TV)",
        "Desk",
        "Mini fridge",
        "Coffee/Tea making facilities",
        "Mountain view",
      ],
      other: [
        "Air conditioning (cooling/heating)",
        "Wi-Fi",
        "Safe",
        "Telephone",
        "Voltage adaptors",
        "Minibar",
      ],
    },
  }, "deluxe-suite": {
    images: [
      { src: 'assets/inside 10.jpg', alt: 'Deluxe Suite - View 1' },
      { src: 'assets/inside 11.jpg', alt: 'Deluxe Suite - View 2' },
      { src: 'assets/inside 12.jpg', alt: 'Deluxe Suite - View 3' },
      { src: 'assets/inside 13.jpg', alt: 'Deluxe Suite - View 4' },
      { src: 'assets/inside 14.jpg', alt: 'Deluxe Suite - View 5' },
    ],
    description: "Bright and sunny en-suite rooms with a private sunny balcony, ideal for relaxation and enjoying the scenic views.",
    amenities: {
      bathroom: ["Towels", "Toiletries", "Hairdryer", "Stylish en-suite bathroom"],
      bedroom: [
        "Queen size bed",
        "Soft cotton linen",
        "Dressing gowns",
        "Pillows",
        "Television (Satellite TV)",
        "Desk",
        "Mini fridge",
        "Stylishly furnished",
      ],
      other: [
        "Private sunny balcony",
        "Air conditioning (cooling/heating)",
        "Wi-Fi",
        "Iron",
        "Safe",
        "Kettle",
        "Wardrobe",
        "Tea/coffee making facilities",
        "Telephone / alarm clock",
      ],
    },
  },
};
  
 
}
