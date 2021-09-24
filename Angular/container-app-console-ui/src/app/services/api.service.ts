import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { Container } from "../models/element";

@Injectable({ providedIn: "root" })
export class ApiService {
    private readonly endpoint = "http://console-api.westus2.cloudapp.azure.com/api";
    constructor(private _httpClient: HttpClient) { }

    public getContainers(): Observable<Container[]> {
        return this._httpClient.get<Container[]>(`http://console-api.westus2.cloudapp.azure.com/api/containers`);
    }
}