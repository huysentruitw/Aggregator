import { Component, Input } from "@angular/core";
import { FormBuilder, FormControl, Validators, FormGroup } from "@angular/forms";
import { AbstractControl } from "@angular/forms/src/model";
import { User } from "./user";
import { UserService } from "./userservice";

@Component({
    selector: '[user-list-new-row]',
    templateUrl: './user.list.new.row.component.html'
})
export class UserListNewRowComponent {
    public userForm: FormGroup;

    constructor(private userService: UserService, formBuilder: FormBuilder) {
        this.userForm = formBuilder.group({
            givenName: [ '', Validators.required ],
            surname: [ '', Validators.required ],
            emailAddress: [ '', Validators.email ]
        });
    }

    public isInvalid(controlName: string): boolean {
        var control = this.userForm.controls[controlName];
        return !control.valid && (control.dirty || control.touched);
    }

    public submit() {
        this.userService.addUser({
            id: '',
            givenName: this.userForm.value.givenName,
            surname: this.userForm.value.surname,
            emailAddress: this.userForm.value.emailAddress
        });
    }
}
