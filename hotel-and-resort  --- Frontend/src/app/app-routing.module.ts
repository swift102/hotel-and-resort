import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';


import { HomeComponent } from './home/home.component';
import { RoomComponent } from './room/room.component';
import { ContactComponent } from './contact/contact.component';

const routes: Routes = [

  {path: 'home', component: HomeComponent},
  {path: 'rooms', component: RoomComponent},
  {path:  'contact', component: ContactComponent},
  { path: '', redirectTo: 'home', pathMatch: 'full' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
