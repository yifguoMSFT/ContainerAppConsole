import { Injectable } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { webSocket, WebSocketSubject } from "rxjs/webSocket";

@Injectable({ providedIn: "root" })
export class WebsocketService {
    private endpoint: string = "console-api.westus2.cloudapp.azure.com/console";
    public webSocketSubject: WebSocketSubject<any>;

    constructor(private _activatedRoute: ActivatedRoute) {
        const endpoint: string = this._activatedRoute.snapshot.queryParams["endpoint"];
        if (endpoint && endpoint.length > 0) {
            this.endpoint = endpoint;
        }
        const url = `ws://${this.endpoint}`;
        this.webSocketSubject = webSocket(url);
    }

    public close() {
        this.webSocketSubject.complete();
    }

    public sendMessage(command: string) {
        this.webSocketSubject.next(command);
    }

    public setContainer(containerId: string) {
        const command = "set-container " + containerId;
        this.sendMessage(command);
    }
}

