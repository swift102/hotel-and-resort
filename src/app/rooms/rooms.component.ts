import { Component } from '@angular/core';

@Component({
  selector: 'app-rooms',
  templateUrl: './rooms.component.html',
  styleUrl: './rooms.component.scss'
})
export class RoomsComponent {
  roomSections = [
    {
      rooms: [
        {
          name: 'Luxury Suite',
          features: ['2 Bed', 'Complimentary liquor', 'Hot tub', 'Outdoor dining area'],
          price: 'R 2 350',
          image: 'assets/images/lux room.jpg'
        
        },
        {
          name: 'Deluxe Suite',
          features: ['2 Bed', 'Luxurious interior', 'Free Wifi', 'Minibar'],
          price: 'R 1 650',
          image: 'assets/images/Delux room.jpg'
        },
        {
          name: 'Premium Suite',
          features: ['2 Bed', 'Scenic View', 'Free Wifi', 'Balcony'],
          price: 'R 2 050',
          image: 'assets/images/pre room.jpg'
        }
      ]
    },
    {
      rooms: [
        {
          name: 'Luxury Room',
          features: ['1 Bed', 'Terrace', 'Bathroom amenities'],
          price: 'R 1 500',
          image: 'assets/images/deluxroom.jpg'
        },
        {
          name: 'Deluxe Room',
          features: ['1 Bed', 'Cleaning service', 'Safe'],
          price: 'R 950',
          image: 'assets/images/double room.jpg'
        },
        {
          name: 'Premium Room',
          features: ['1 Bed', 'Hypoallergenic pillow', 'Lake view'],
          price: 'R 1 250',
          image: 'assets/images/prem room.jpg'
        }
      ]
    }
  ];
room: any;
}