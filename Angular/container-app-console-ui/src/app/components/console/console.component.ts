import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { WebsocketService } from '../../services/websocket.service';
import { Message, MessageVerb } from '../../models/Message';

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
  constructor(private _websocketService: WebsocketService) { }

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
    this.focusToInput();
  }

  onCommandEnter() {
    // if (this.command === "") return;
    if (this.command.toLowerCase() === "cls" || this.command.toLowerCase() === "clear") {
      this.consoleText = this.defaultConsoleText + "<br>" + this.prefix;
    } else if (this.command.toLowerCase() === "reset") {
      this._websocketService.sendMessage("reset");
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
    this.prefix = prefix;
    this.consoleText = this.consoleText + prefix + "# ";
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

  //Ctrl + C to stop running command
  terminate() {
    // this._websocketService.sendMessage("signal SIGINT");
  }

  private focusToInput() {
    const ele = document.getElementById("input-command");
    if (ele) ele.focus();
  }

  ngOnDestroy() {
    this._websocketService.close();
  }
}