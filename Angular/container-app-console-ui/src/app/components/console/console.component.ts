import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { WebsocketService } from '../../services/websocket.service';
import { Message, MessageVerb } from '../../models/Message';

@Component({
  selector: 'app-console',
  templateUrl: './console.component.html',
  styleUrls: ['./console.component.scss']
})
export class ConsoleComponent implements OnDestroy, OnInit {
  title = 'container-app-console-ui';
  prefix: string = "D:/";
  defaultConsoleText: string = `Thank you for Using Container App Console`;
  consoleText: string = this.defaultConsoleText;
  command: string = "cmd";
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
  }

  onCommandEnter() {
    if (this.command === "") return;
    if(this.command.toLowerCase() === "cls" || this.command.toLowerCase() === "clear") {
      this.consoleText = this.defaultConsoleText;
      this.command = "";
    }
    const message = "run " + this.command;
    this._websocketService.sendMessage(message);
    this.command = "";
  }

  private updateConsoleText(text: string) {
    this.consoleText = this.consoleText + "<br>" + `${this.prefix} ${this.command}` + "<br>" + `${text}` + "<br>";
  }

  private updatePrefix(prefix: string) {
    this.prefix = prefix;
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
    this.consoleComponent.nativeElement.scrollTop = scrollHeight + 200;
  }

  //Ctrl + C to stop running command
  terminate() {
    // this._websocketService.sendMessage("signal SIGINT");
  }

  ngOnDestroy() {
    this._websocketService.close();
  }
}