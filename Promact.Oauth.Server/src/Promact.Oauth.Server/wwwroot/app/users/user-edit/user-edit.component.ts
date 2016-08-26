﻿import {Component} from "@angular/core";
import {Router, ActivatedRoute, ROUTER_DIRECTIVES} from "@angular/router";



import { UserService }   from '../user.service';
import {UserModel} from '../user.model';
import {Md2Toast} from 'md2/toast';

@Component({
    templateUrl: './app/users/user-edit/user-edit.html',

    providers: [Md2Toast]
})

export class UserEditComponent {
    user: UserModel;
    id: any;
    errorMessage: string;
    isSlackUserNameExist: boolean;

    constructor(private userService: UserService, private route: ActivatedRoute, private redirectionRoute: Router, private toast: Md2Toast) {
        this.user = new UserModel();
    }

    ngOnInit() {
        this.id = this.route.params.subscribe(params => {
            let id = this.route.snapshot.params['id'];

            this.userService.getUserById(id)
                .subscribe(
                user => this.user = user,
                error => this.errorMessage = <any>error)
        });
    }


    editUser(user: UserModel) {
        //if (this.isSlackUserNameExist == true) {
        this.userService.editUser(user).subscribe((result) => {
            if (result == true) {
                this.toast.show('User updated successfully.');
                this.redirectionRoute.navigate(['/user/']);
            }
            else if (result == false) {
                this.toast.show('User Name or Slack User Name already exists.');
            }
            
        }, err => {
                });
        //}
        //else {
        //    this.toast.show('Slack User Name  already exists.');
        //}
    }

    //checkSlackUserName(slackUserName) {
    //    this.isSlackUserNameExist = false;
    //    this.userService.findUserBySlackUserName(slackUserName).subscribe((isSlackUserNameExist) => {
    //        if (isSlackUserNameExist) {
    //            this.isSlackUserNameExist = true;
    //        }
    //        else {
    //            this.isSlackUserNameExist = false;
    //        }
    //    }, err => {
    //    });
    //}


    goBack() {
        this.redirectionRoute.navigate(['/user/']);
    }
}

