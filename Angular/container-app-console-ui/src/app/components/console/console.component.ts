import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { WebsocketService } from '../../services/websocket.service';
import { Message, MessageVerb } from '../../models/message';
import { ApiService } from '../../services/api.service';
import { Container } from 'src/app/models/element';

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
  private set _selectedContainerId(id: string) {
    this.selectedContainerId = id;
    this._websocketService.resetWebSocket();
    this._websocketService.setPodAndContainer(this.selectedPodId, id);
  }
  selectedContainerId: string = "";
  selectedPodId: string = "12";
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

    this._apiService.getContainers().subscribe(containers => {
      this.containers = containers;

      // this.selectedContainerId = containers[0].Pid;
      this._selectedContainerId = containers[0].Pid;
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
  }

  private updatePrefix(prefix: string) {
    this.prefix = `${this.selectedContainerId}@${prefix}`;
    this.consoleText = this.consoleText + this.prefix + "# ";
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
        this.scrollToBottom();
        break;
      case MessageVerb.prefix:
        const prefix = message.prefix;
        this.updatePrefix(prefix);
        break;
    }
  }

  private scrollToBottom() {
    const scrollHeight = this.consoleComponent.nativeElement.scrollHeight;
    this.consoleComponent.nativeElement.scrollTop = scrollHeight + 400;
  }


  private focusToInput() {
    const ele = document.getElementById("input-command");
    if (ele) ele.focus();
  }


  selectContainer(e: { value: string }) {
    const containerId = e.value;
    this._selectedContainerId = containerId;

    if(this.consoleText.length > 0) {
      this.consoleText = this.consoleText + "<br><br>";
    }
  }

  ngOnDestroy() {
    this._websocketService.close();
  }
}