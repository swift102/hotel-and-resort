import { NgModule } from '@angular/core';
import { BrowserModule, provideClientHydration } from '@angular/platform-browser';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { HomeComponent } from './home/home.component';
import { NavbarComponent } from './navbar/navbar.component';
import { RoomComponent } from './room/room.component';
import { ContactComponent } from './contact/contact.component';
import { BookingComponent } from './Booking/booking/booking.component';
import { RoomCardComponent } from './Booking/room-card/room-card.component';
import { PersonalInfoComponent } from './Booking/personal-info/personal-info.component';
// import { PaymentComponent } from './Booking/payment/payment.component';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

@NgModule({
  declarations: [
    AppComponent,
    HomeComponent,
    NavbarComponent,
    RoomComponent,
    ContactComponent,
    BookingComponent,
    RoomCardComponent,
    PersonalInfoComponent,
    // PaymentComponent,
     NavbarComponent,
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    FormsModule,
    ReactiveFormsModule,
    CommonModule
  ],
  providers: [
    provideClientHydration(),
    provideAnimationsAsync()
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
