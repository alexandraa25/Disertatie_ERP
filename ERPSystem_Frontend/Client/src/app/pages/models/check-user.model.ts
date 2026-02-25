export class CheckUserModel {
  emailAddress: string;
  phoneNumber: string;

  constructor(emailAddress: string = '', phoneNumber: string = '') {
    this.emailAddress = emailAddress;
    this.phoneNumber = phoneNumber;
  }
  
}
