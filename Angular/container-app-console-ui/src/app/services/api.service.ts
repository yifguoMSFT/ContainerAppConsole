import { HttpClient, HttpParams } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "../../environments/environment";
import { Container, Pod } from "../models/element";

@Injectable({ providedIn: "root" })
export class ApiService {
    private readonly endpoint = `http://${environment.endpoint}`;
    constructor(private _httpClient: HttpClient) { }

    public getPods(capp:string): Observable<string[]> {
        return this._httpClient.get<string[]>(`${this.endpoint}/api/containerApps/${capp}/pods`);
    }

    public getCapps(): Observable<string[]> {
        return this._httpClient.get<string[]>(`${this.endpoint}/api/containerApps`);
    }
}