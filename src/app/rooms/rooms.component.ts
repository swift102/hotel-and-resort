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
          image: 'assets/images/lux_room.jpg'
        },
        // Other room details...
      ]
    },
    // Other sections...
  ];
}
