import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

import { UserManagementComponent } from './usermanagement.component'
import { UserListComponent } from './user.list.component'
import { UserListRowComponent } from './user.list.row.component'
import { UserListNewRowComponent } from './user.list.new.row.component'
import { UserService } from './userservice';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule
    ],
    declarations: [
        UserManagementComponent,
        UserListComponent,
        UserListRowComponent,
        UserListNewRowComponent
    ],
    providers: [
        UserService
    ]
})
export class UserManagementModule {
}
