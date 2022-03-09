import { AfterViewInit, Component, OnDestroy, OnInit, ViewChild, ViewEncapsulation } from '@angular/core';
import { WebsocketService } from '../../services/websocket.service';
import { Message, MessageVerb } from '../../models/message';
import { ApiService } from '../../services/api.service';
import { Container, Pod } from 'src/app/models/element';
import { NgTerminal } from 'ng-terminal';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-console',
  templateUrl: './console.component.html',
  styleUrls: ['./console.component.scss'],
  encapsulation: ViewEncapsulation.None
})
export class ConsoleComponent implements OnDestroy, OnInit, AfterViewInit {
  @ViewChild('term', { static: true }) term: NgTerminal;
  prefix: string = "";
  defaultConsoleText: string = "";
  consoleText: string = this.defaultConsoleText;
  command: string = "";
  webSocket: WebSocket;

  pods: string[] = null;
  private _selectedPod: string;
  set selectedPod(pod: string) {
    this._selectedPod = pod;

    this._httpClient
      .get(`http://${environment.endpoint}/api/containerApps/${this.selectedCapp}/consoleWebsocketUrl?podName=${pod}`, { responseType: 'text' })
      .toPromise()
      .then((url: string) => {
        if (this.webSocket != null) {
          this.webSocket.close();
          this.term.underlying.reset();
        }
        this.webSocket = new WebSocket(url);
        this.webSocket.onmessage = async (ev: MessageEvent) => {
          if (ev.data instanceof Blob) {
            this.processMessageBlob(ev.data);
          } else {
            this.updateConsoleText(ev.data + "\r\n");
          }
        };
        this.webSocket.onerror = (ev: MessageEvent) => {
          console.log("ws error: " + ev.data);
        };
      });
  }

  disableSelectPod: boolean = true;

  capps: string[] = [];
  selectedCapp: string;
  constructor(private _websocketService: WebsocketService, private _apiService: ApiService, private _httpClient: HttpClient) { }

  @ViewChild("console") consoleComponent: any;

  ngOnInit() {
    this._apiService.getCapps().subscribe(capps => {
      this.capps = capps;
    })
  }

  ngAfterViewInit() {
    this.term.underlying.options.cursorBlink = true;
    this.term.underlying.reset();
    this.term.keyEventInput.subscribe(e => {
      const ev = e.domEvent;
      const printable = !ev.altKey && !ev.ctrlKey && !ev.metaKey;

      this.sendStdin(e.key);
      /*if (e.key.charCodeAt(0) === 13) {
        //this.term.write('\r\n$ ');
        this.sendStdin("\r\n");
      } else if (e.key === '\x7F') {
        if (this.term.underlying.buffer.active.cursorX > 2) {
          this.term.write('\b \b');
        }
      } else if (e.key == '\x03') {
        this.term.write("^C");
      }
      else if (printable) {
        this.term.write(e.key);
      }//*/
    })
  }

  async processMessageBlob(data: Blob) {
    var arrayBuffer = await data.arrayBuffer();
    var arr = new Uint8Array(arrayBuffer);
    var decoder = new TextDecoder();
    switch (arr[0]) {
      case 0: // forwarded from k8s cluster exec endpoint
        switch (arr[1]) {
          case 1: //stdout
          case 2: //stderr
          case 3: //k8s api server error
            this.updateConsoleText(decoder.decode(arr.slice(2)));
            break;
          case 4: //terminal resize
            break;
          default:
            throw new Error(`unknown channel ${arr[1]}`);
        }
        break;
      case 1: // Info from Proxy API
        this.updateConsoleText("INFO: " + decoder.decode(arr.slice(1)));
        break;
      case 2: // Error from Proxy API
        this.updateConsoleText("ERROR: " + decoder.decode(arr.slice(1)));
        break;
      default:
        throw new Error(`unknown Proxy API exec signal ${arr[0]}`);
    }
  }

  onCommandEnter() {
    // if (this.command === "") return;
    if (this.command.toLowerCase() === "cls" || this.command.toLowerCase() === "clear") {
      this.consoleText = this.defaultConsoleText + this.prefix + "# ";
    } else if (this.command.toLowerCase() === "reset") {
      //this._websocketService.sendMessage("reset");
      //this.updateConsoleText("reset<br>");
    }
    else {
      //this.updateConsoleText(this.command);
      //const message = "run " + this.command;
      //this._websocketService.sendMessage(message);
      this.sendStdin(this.command);
    }
    this.command = "";
  }

  sendStdin(text: string) {
    if (this.webSocket && this.webSocket.readyState === this.webSocket.OPEN) {
      var encoder = new TextEncoder();
      var arr = encoder.encode(text);
      this.webSocket.send(new Blob([new Uint8Array([0, 0]), arr]));
    }
  }

  private updateConsoleText(text: string) {
    this.term.write(text);
  }



  selectPod(e: { value: string }) {
    if (this.consoleText.length > 0) {
      this.consoleText = this.consoleText + "<br><br>";
    }
  }

  selectCapp(e: { value: string }) {
    const capp = e.value;
    this.disableSelectPod = true;
    this._apiService.getPods(capp).subscribe(pods => {
      this.pods = pods.length > 0 ? pods : null;
      this.disableSelectPod = false;
    });
  }

  ngOnDestroy() {
    this._websocketService.close();
  }

  onClick(value: string) {
    var url = value;
    if (this.webSocket != null) {
      this.webSocket.close();
      this.term.underlying.reset();
    }
    try {
      this.webSocket = new WebSocket(url);
    } catch (e) {
      console.log(e);
    }
    this.webSocket.onmessage = async (ev: MessageEvent) => {
      if (ev.data instanceof Blob) {
        this.processMessageBlob(ev.data);
      } else {
        this.updateConsoleText(ev.data + "\r\n");
      }
    };


  }
}