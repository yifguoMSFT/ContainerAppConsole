export interface Container {
    Pid: string;
    Name: string;
    Uid: string;
}

export interface Pod {
    name: string;
    uid: string;
    ip:string;
}