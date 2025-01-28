import { Component, OnInit,EventEmitter, Output , Input } from '@angular/core';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';


interface RoomRate {
  type: string;
  oneGuest: number;
  twoGuests: number;
}

@Component({
  selector: 'app-booking',
  templateUrl: './booking.component.html',
  styleUrl: './booking.component.scss'
})
export class BookingComponent {
  @Output() availabilityChecked = new EventEmitter<any[]>();
  availableRooms: any[] = [];
  @Input() rooms: any[] = [];

  onAvailabilityChecked(rooms: any[]) {
    this.availableRooms = rooms;
  }

  checkInDate: string = '';
  checkOutDate: string = '';
  duration: number = 0;
  // availableRooms: any[] = [];

  updateDuration() {
    if (this.checkInDate && this.checkOutDate) {
      const startDate = new Date(this.checkInDate);
      const endDate = new Date(this.checkOutDate);
      const timeDiff = endDate.getTime() - startDate.getTime();
      this.duration = Math.ceil(timeDiff / (1000 * 3600 * 24));
    } else {
      this.duration = 0;
    }
  }

  checkAvailability() {
    // Simulate checking availability and populate available rooms
    this.availableRooms = [
      {
        type: 'Standard En-suite Guestrooms',
        description: 'King or Twin en-suite rooms. Please specify bed preference in the request section. This twin/double room features air conditioning, a bathrobe, and cable TV.',
        price: 2900,
        maxGuests: 2
      }
      // Add more rooms as needed
    ];
    console.log('Available rooms:', this.availableRooms);
  }

  viewCalendar() {
    // Implement the logic to view the calendar
    console.log('Viewing calendar');
  }
}