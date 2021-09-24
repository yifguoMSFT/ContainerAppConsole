import { Injectable } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { BehaviorSubject, Observable, of, Subject } from "rxjs";
import { webSocket, WebSocketSubject } from "rxjs/webSocket";
import { catchError, flatMap, map, mergeMap, switchMap, tap } from "rxjs/operators";
import { Message } from "../models/message";

@Injectable({ providedIn: "root" })
export class WebsocketService {
    private endpoint: string = "console-api.westus2.cloudapp.azure.com/console";
    public webSocketSubject: Observable<string | Message>;
    public resetSocketSubject: Subject<boolean> = new BehaviorSubject(false);

    private websocket: WebSocketSubject<string | Message> = null;

    constructor(private _activatedRoute: ActivatedRoute) {
        const endpoint: string = this._activatedRoute.snapshot.queryParams["endpoint"];
        if (endpoint && endpoint.length > 0) {
            this.endpoint = endpoint;
        }
        const url = `ws://${this.endpoint}`;
        this.webSocketSubject = webSocket(url);

        this.webSocketSubject = this.resetSocketSubject.pipe(
            switchMap((closeWebSocket: boolean) => {
                if (this.websocket) this.websocket.complete();

                const url = `ws://${this.endpoint}`;
                this.websocket = webSocket(url);
                return this.websocket;
            })
        );
    }

    public close() {
        this.websocket.complete();
    }

    public sendMessage(command: string) {
        this.websocket.next(command);
    }

    public setContainer(containerId: string) {
        const command = "set-container " + containerId;
        this.sendMessage(command);
        console.log("set-container", containerId);
    }


    public setPod(podId: string) {
        const command = "set-pod " + podId;
        this.sendMessage(command);
        console.log("set-pod", podId);
    }

    public setPodAndContainer(podId: string, containerId: string) {
        this.setPod(podId);
        this.setContainer(containerId);
    }

    public resetWebSocket() {
        this.resetSocketSubject.next(true);
    }
}

