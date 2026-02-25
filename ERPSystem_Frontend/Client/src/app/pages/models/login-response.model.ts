export interface LoginResponseModel {
  token: string;
  user: {
    id: number;
    email: string;
    firstName: string;
    lastName: string;
    phoneNumber: string | null;
    address: string | null;
    role: {
      name: string;
    }
  };
}