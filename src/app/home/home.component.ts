import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent implements OnInit {
  slides = [
    { image: 'assets/images/slide1.jpg' },
    { image: 'assets/images/slide2.jpg' },
    { image: 'assets/images/slide3.jpg' },
    { image: 'assets/images/slide4.jpg' }
  ];

  rooms = [
    { image: 'assets/images/deluxe.jpg', name: 'Deluxe Room' },
    { image: 'assets/images/luxury.jpg', name: 'Luxury Room' },
    { image: 'assets/images/premium.jpg', name: 'Premium Room' }
  ];

  serviceGroups = [
    [
      { image: 'assets/images/room_service.png', title: 'Room Service', description: 'Enjoy excellent and timely room service.' },
      { image: 'assets/images/breakfast.png', title: 'Free Breakfast', description: 'Enjoy free breakfast every morning.' }
    ],
    [
      { image: 'assets/images/wifi.png', title: 'Free WiFi', description: 'Enjoy free WiFi.' },
      { image: 'assets/images/wheelchair.png', title: 'Wheelchair', description: 'Wheelchair accessible and elevator.' }
    ],
    [
      { image: 'assets/images/parking.png', title: 'Free Parking', description: 'No extra charges for parking.' },
      { image: 'assets/images/spa.png', title: 'Free Spa', description: 'Relax at the in-house spa once every day of your stay.' }
    ]
  ];

  currentSlideIndex = 0;
services: any;

  constructor() {}

  ngOnInit(): void {
    this.startSlideshow();
  }

  startSlideshow(): void {
    setInterval(() => {
      this.nextSlide();
    }, 5000); // Automatically change slide every 5 seconds
  }

  nextSlide(): void {
    this.currentSlideIndex = (this.currentSlideIndex + 1) % this.slides.length;
  }

  prevSlide(): void {
    this.currentSlideIndex =
      (this.currentSlideIndex - 1 + this.slides.length) % this.slides.length;
  }

  setSlide(index: number): void {
    this.currentSlideIndex = index;
  }
}