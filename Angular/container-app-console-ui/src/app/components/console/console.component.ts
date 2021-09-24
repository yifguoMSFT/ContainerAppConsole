import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { WebsocketService } from '../../services/websocket.service';
import { Message, MessageVerb } from '../../models/message';
import { ApiService } from '../../services/api.service';
import { Container, Pod } from 'src/app/models/element';

@Component({
  selector: 'app-console',
  templateUrl: './console.component.html',
  styleUrls: ['./console.component.scss']
})
export class ConsoleComponent implements OnDestroy, OnInit {
  prefix: string = "";
  defaultConsoleText: string = "";
  consoleText: string = this.defaultConsoleText;
  command: string = "";

  containers: Container[] = [];
  private _selectedContainer: Container;
  set selectedContainer(container: Container) {
    this._selectedContainer = container;
    this._websocketService.resetWebSocket();
    //Need node ip and container id
    this._websocketService.setNodeAndContainer(this.selectedPod.ip, container.id);
  }

  disableSelectContainer: boolean = true;

  pods: Pod[] = [];
  selectedPod: Pod;
  constructor(private _websocketService: WebsocketService, private _apiService: ApiService) { }

  @ViewChild("console") consoleComponent: any;

  ngOnInit() {
    this._websocketService.webSocketSubject.subscribe(
      ((msg: Message) => {
        this.processSocketMessage(msg);
      }),
      (err => {
        console.error(err);
      }),
      () => {
        console.log("complete");
        this._websocketService.close();
      }
    );

    this._apiService.getPods().subscribe(pods => {
      this.pods = pods;
    })

    this.focusToInput();
  }

  onCommandEnter() {
    // if (this.command === "") return;
    if (this.command.toLowerCase() === "cls" || this.command.toLowerCase() === "clear") {
      this.consoleText = this.defaultConsoleText + this.prefix + "# ";
    } else if (this.command.toLowerCase() === "reset") {
      this._websocketService.sendMessage("reset");
      this.updateConsoleText("reset<br>");
    }
    else {
      this.updateConsoleText(this.command);
      const message = "run " + this.command;
      this._websocketService.sendMessage(message);
    }
    this.command = "";
  }

  private updateConsoleText(text: string) {
    this.consoleText = this.consoleText + `${text}` + "<br>";
    this.scrollToBottom();
  }

  private updatePrefix(prefix: string) {
    this.prefix = `${this._selectedContainer.name}@${prefix}`;
    this.consoleText = this.consoleText + this.prefix + "# ";
    this.scrollToBottom();
  }

  private processSocketMessage(message: Message) {
    const key: string = Object.keys(message)[0];
    switch (key) {
      case MessageVerb.error:
        console.log("------------Error----------------");
        console.log(message.error);
        break;
      case MessageVerb.text:
        const text = message.text;
        this.updateConsoleText(text);
        break;
      case MessageVerb.prefix:
        const prefix = message.prefix;
        this.updatePrefix(prefix);
        break;
    }
  }

  private scrollToBottom() {
    setTimeout(() => {
      const scrollHeight = this.consoleComponent.nativeElement.scrollHeight;
      this.consoleComponent.nativeElement.scrollTop = scrollHeight;
    }, 10);
  }


  private focusToInput() {
    const ele = document.getElementById("input-command");
    if (ele) ele.focus();
  }


  selectContainer(e: { value: string }) {
    if (this.consoleText.length > 0) {
      this.consoleText = this.consoleText + "<br><br>";
    }
  }

  selectPod(e: { value: Pod }) {
    const pod = e.value;
    this.disableSelectContainer = true;
    this._apiService.getContainers(pod.id, pod.ip).subscribe(containers => {
      this.containers = containers;
      this.disableSelectContainer = false;
    });
  }

  ngOnDestroy() {
    this._websocketService.close();
  }
}