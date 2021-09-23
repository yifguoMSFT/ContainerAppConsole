import { ErrorHandler, Injectable } from "@angular/core";

@Injectable()
export class UnHandleExceptionHandler extends ErrorHandler {
    handleError(error : any) {
        console.error(error);
    }
}