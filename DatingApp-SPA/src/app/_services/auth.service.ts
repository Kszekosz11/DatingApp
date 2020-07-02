import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { importExpr } from '@angular/compiler/src/output/output_ast';
import {map} from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  baseUrl = 'http://localhost:5000/api/auth/';

constructor(private http: HttpClient) { }

  login(model: any) {
    return this.http.post(this.baseUrl + 'login', model)
    .pipe(
      map((responce: any) => {
        const user = responce;
        if (user) {
          localStorage.setItem('token', user.token);
        }
      })
    );
  }

  register(model: any) {
    return this.http.post(this.baseUrl + 'register', model);
  }
}