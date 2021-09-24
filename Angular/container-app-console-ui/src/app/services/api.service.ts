import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Observable, of } from "rxjs";
import { delay } from "rxjs/operators";
import { Container, Pod } from "../models/element";

@Injectable({ providedIn: "root" })
export class ApiService {
    private readonly endpoint = "http://console-api.westus2.cloudapp.azure.com/api";
    constructor(private _httpClient: HttpClient) { }

    public getContainers(uid: string, ip: string): Observable<Container[]> {
        //Todo, query string
        return this._httpClient.get<Container[]>(`${this.endpoint}/containers`);
    }

    public getPods(): Observable<Pod[]> {
        //For test
        return this._httpClient.get<Pod[]>("http://console-api-v2.westus2.cloudapp.azure.com/api/pods");

        return this._httpClient.get<Pod[]>(`${this.endpoint}/pods`);
    }
}