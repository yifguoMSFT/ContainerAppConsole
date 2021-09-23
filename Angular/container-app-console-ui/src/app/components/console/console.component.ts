import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { WebsocketService } from '../../services/websocket.service';

@Component({
  selector: 'app-console',
  templateUrl: './console.component.html',
  styleUrls: ['./console.component.scss']
})
export class ConsoleComponent implements OnDestroy, OnInit {
  title = 'container-app-console-ui';
  currentPath: string = "D:/";
  consoleText: string = `Thank you for Using Container App Console`;
  command: string = "cmd";
  constructor(private _websocketService: WebsocketService) { }

  @ViewChild("console") consoleComponent: any;

  ngOnInit() {
    this._websocketService.webSocketSubject.subscribe(
      (msg => {
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

    //prefix with "run" to for command
    const message = `run ${this.command}`;
    this._websocketService.sendMessage(message);
  }

  private updateConsoleText(message: string) {
    this.consoleText = this.consoleText + "<br>" + `${this.currentPath} ${this.command}` + "<br>" + `${message}` + "<br>";
    this.command = "";
  }

  private processSocketMessage(message: any) {
    //Todo, if it is text 
    console.log(message);
    this.updateConsoleText(message);
    this.scrollToBottom();
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
