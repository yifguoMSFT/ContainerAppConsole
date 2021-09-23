//Message only with one key-value pair
export type Message = {
    [key in MessageVerb]: string
}

export enum MessageVerb {
    prefix = "prefix",
    error = "error",
    text = "text",
}