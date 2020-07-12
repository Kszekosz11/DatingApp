import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { User } from '../_models/user';

// const httpOptions = {
//   headers: new HttpHeaders({
//     // tslint:disable-next-line: object-literal-key-quotes
//     'Authorization':  'Bearer ' + localStorage.getItem('token')
//   })
// }; // dodanie tokenu, tak jak w Postman'ie

// usuwamy to ponieważ zrobiliśmy nowy sposób wysyłania tokenu w app.module.ts


@Injectable({
  providedIn: 'root'
})
export class UserService {
  baseUrl = environment.apiUrl;

constructor(private http: HttpClient) { }


  getUsers(): Observable<User[]> {
    // return this.http.get<User[]>(this.baseUrl + 'users', httpOptions);
    return this.http.get<User[]>(this.baseUrl + 'users');
  } // zwracanie userów

  getUser(id: string): Observable<User> {
    // return this.http.get<User>(this.baseUrl + 'users/' + id, httpOptions);
    return this.http.get<User>(this.baseUrl + 'users/' + id);
  }
}
