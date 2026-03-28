export interface PublicResponse<T> {
  isSuccess: boolean;
  value: T;
  error?: {
    errorCode: string;
    errorMessage: string;
  };
  statusCode: number;
}