import { ErrorHandler, NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { AppComponent } from './app.component';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { WebsocketService } from './services/websocket.service';
import { UnHandleExceptionHandler } from './services/unhandle-exception-handler';
import { RouterModule } from '@angular/router';
import { ConsoleComponent } from './components/console/console.component';

export const MainModuleRoutes = RouterModule.forRoot([
  {
    path: '',
    component: ConsoleComponent
  }
])

@NgModule({
  declarations: [
    AppComponent,
    ConsoleComponent
  ],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    FormsModule,
    HttpClientModule,
    MainModuleRoutes,
    // RouterModule
  ],
  providers: [
    {
      provide: ErrorHandler,
      useClass: UnHandleExceptionHandler
    }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
