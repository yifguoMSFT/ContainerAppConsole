import { Injectable } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { BehaviorSubject, Observable, of, Subject } from "rxjs";
import { webSocket, WebSocketSubject } from "rxjs/webSocket";
import { switchMap } from "rxjs/operators";
import { Message } from "../models/message";
import { environment } from "../../environments/environment";

@Injectable({ providedIn: "root" })
export class WebsocketService {
    // private endpoint: string = "console-api-v2.westus2.cloudapp.azure.com/console";
    private endpoint: string = environment.endpoint
    public webSocketSubject: Observable<string | Message>;
    public resetSocketSubject: Subject<boolean> = new Subject();

    private websocket: WebSocketSubject<string | Message> = null;

    constructor(private _activatedRoute: ActivatedRoute) {
        const endpoint: string = this._activatedRoute.snapshot.queryParams["endpoint"];
        if (endpoint && endpoint.length > 0) {
            this.endpoint = endpoint;
        }

        this.webSocketSubject = this.resetSocketSubject.pipe(
            switchMap((_: boolean) => {
                if (this.websocket) this.websocket.complete();

                const url = `ws://${this.endpoint}/console`;
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


    public setNode(nodeIp: string) {
        const command = "set-node " + nodeIp;
        this.sendMessage(command);
        console.log("set-node", nodeIp);
    }

    public setNodeAndContainer(nodeIp: string, containerId: string) {
        this.setNode(nodeIp);
        this.setContainer(containerId);
    }

    public resetWebSocket() {
        this.resetSocketSubject.next(true);
    }
}

