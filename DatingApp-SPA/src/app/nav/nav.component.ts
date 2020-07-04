import { Component, OnInit } from '@angular/core';
import { AuthService } from '../_services/auth.service';
import { AlertifyService } from '../_services/alertify.service';

@Component({
  selector: 'app-nav',
  templateUrl: './nav.component.html',
  styleUrls: ['./nav.component.css']
})
export class NavComponent implements OnInit {

  model: any = {};  // tutaj będziemy zapisywać naszego użytkownika i hasło

  constructor(public authService: AuthService, private alertify: AlertifyService) { }
  // wstrzyknięcie serwisu autoryzującego, serwisu powiadomień

  ngOnInit() {
  }

  login() {
    this.authService.login(this.model).subscribe(next => {
      this.alertify.success('Logged is successfully'); // zastąpienie powiadomieniem zamiast wyrzuceniem na konsole
    }, error => {
      this.alertify.error(error); // zastąpienie powiadomieniem zamiast wyrzuceniem na konsole
    });
  } // logowanie

  loggedIn() {
    // const token = localStorage.getItem('token');
    // return !!token; jeśli coś jest w tym to zwróci true, jeśli jest pusty to zwróci false

    // zastąpienie kodu powyżej, poprzez wstyknięcie z serwisu autoryzującego, przeniesienie metody właśnie tam
    return this.authService.loggedIn();
  }

  logout() {
    localStorage.removeItem('token');
    this.alertify.message('Logged out'); // zastąpienie powiadomieniem zamiast wyrzuceniem na konsole
  }
}
