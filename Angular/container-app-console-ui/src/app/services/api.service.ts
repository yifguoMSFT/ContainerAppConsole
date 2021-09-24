import { HttpClient, HttpParams } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "../../environments/environment";
import { Container, Pod } from "../models/element";

@Injectable({ providedIn: "root" })
export class ApiService {
    private readonly endpoint = `http://${environment.endpoint}`;
    constructor(private _httpClient: HttpClient) { }

    public getContainers(id: string, ip: string): Observable<Container[]> {
        let httpParams = new HttpParams().appendAll({
            "id":id,
            "ip":ip
        })
        return this._httpClient.get<Container[]>(`${this.endpoint}/api/containers`, { params:  httpParams});
    }

    public getPods(): Observable<Pod[]> {
        return this._httpClient.get<Pod[]>(`${this.endpoint}/api/pods`);
    }
}