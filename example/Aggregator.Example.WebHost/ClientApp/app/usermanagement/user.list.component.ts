import { Component, Inject } from '@angular/core'
import { Http } from '@angular/http';
import { User } from './user';
import { UserService } from './userservice';

@Component({
    selector: 'user-list',
    templateUrl: './user.list.component.html'
})
export class UserListComponent {
    public users: User[]

    constructor(private userService: UserService) {
        this.fetchUsers();

        userService.userAdded.subscribe(user => {
            this.users = this.users.concat([user]);
        });

        userService.userRemoved.subscribe(id => {
            this.users = this.users.filter(x => x.id !== id);
        });
    }

    private fetchUsers(): void {
        this.userService.fetchUsers(users => this.users = users);
    }
}
