import { Component, OnInit, OnDestroy } from '@angular/core';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent implements OnInit, OnDestroy {
    slides = [
      { image: 'assets/images/slide 4.4.jpg' },
      { image: 'assets/images/lobby.jpg' },
      { image: 'assets/images/slide 1.1.jpg' },
      { image: 'assets/images/slide 3.3.jpg' },
      { image: 'assets/images/slide 5.5.jpg' },
      { image: 'assets/images/slide 6.6.jpg' },
    ];
    rooms = [
      { image: 'assets/images/double room.jpg', name: 'Deluxe Suite' },
      { image: 'assets/images/deluxroom.jpg', name: 'Luxury Suite' },
      { image: 'assets/images/premium room.jpg', name: 'Premium Suite' },
    ];
    serviceGroups = [
      [
        { image: 'assets/images/bell.png', title: 'Room Service', description: 'Enjoy excellent and timely room service.' },
        { image: 'assets/images/coffee.png', title: 'Free Breakfast', description: 'Enjoy free breakfast every morning.' },
      ],
      [
        { image: 'assets/images/wifi.png', title: 'Free WiFi', description: 'Enjoy free WiFi.' },
        { image: 'assets/images/wheelchair.png', title: 'Wheelchair', description: 'Wheelchair accessible and elevator.' },
      ],
      [
        { image: 'assets/images/parking.png', title: 'Free Parking', description: 'No extra charges for parking.' },
        { image: 'assets/images/spa.png', title: 'Free Spa', description: 'Relax at the in-house spa once every day of your stay.' },
      ],
    ];
  
    currentSlideIndex = 0;
    autoSlideInterval: any;
  
    ngOnInit(): void {
      this.startAutoSlide();
    }
  
    ngOnDestroy(): void {
      this.stopAutoSlide();
    }
  
    startAutoSlide(): void {
      this.autoSlideInterval = setInterval(() => {
        this.nextSlide();
      }, 5000); // Change slide every 5 seconds
    }
  
    stopAutoSlide(): void {
      if (this.autoSlideInterval) {
        clearInterval(this.autoSlideInterval);
      }
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
