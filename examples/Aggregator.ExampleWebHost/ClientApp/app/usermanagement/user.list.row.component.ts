import { Component, Input, Inject } from '@angular/core'
import { User } from './user';
import { UserService } from './userservice';

@Component({
    selector: '[user-list-row]',
    templateUrl: './user.list.row.component.html'
})
export class UserListRowComponent {
    @Input('user') user: User;

    constructor(private userService: UserService) {
    }

    public delete() {
        if (confirm("Are you sure you want to remove " + this.user.givenName + " " + this.user.surname + "?") === true)
            this.userService.removeUser(this.user.id);
    }
}
