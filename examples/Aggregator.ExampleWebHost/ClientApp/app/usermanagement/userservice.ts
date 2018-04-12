import { Inject, Injectable, EventEmitter } from "@angular/core";
import { Http } from "@angular/http";
import { User } from "./user";

@Injectable()
export class UserService {
    private apiUrl: string;

    private onUserAdded = new EventEmitter<User>();
    private onUserRemoved = new EventEmitter<string>();

    public userAdded = this.onUserAdded.asObservable();
    public userRemoved = this.onUserRemoved.asObservable();

    constructor(private http: Http, @Inject('BASE_URL') baseUrl: string) {
        this.apiUrl = baseUrl + 'api/user'
    }

    public fetchUsers(success: (users: User[]) => void): void {
        this.http.get(this.apiUrl).subscribe(result => {
            success(result.json() as User[]);
        }, error => console.error(error));
    }

    public addUser(user: User, success: (user: User) => void = _ => { }): void {
        this.http.post(this.apiUrl, user).subscribe(result => {
            var user = result.json() as User;
            this.onUserAdded.emit(user);
            success(user);
        }, error => console.error(error));
    }

    public removeUser(id: string, success: (id: string) => void = _ => { }): void {
        this.http.delete(this.apiUrl + '/' + id).subscribe(result => {
            this.onUserRemoved.emit(id);
            success(id);
        }, error => console.error(error));
    }
}
