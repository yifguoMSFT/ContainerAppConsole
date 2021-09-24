export type Message = Partial<Record<MessageVerb,string>>;

export enum MessageVerb {
    prefix = "prefix",
    error = "error",
    text = "text",
}