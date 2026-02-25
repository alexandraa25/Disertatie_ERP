namespace ERPSystem.Utils.Response
{
    public class PublicResponse
    {
        public bool IsSuccess { get; set; }
        public object? Value { get; set; }
        public Error? Error { get; set; }
        public int StatusCode { get; set; } = 200; // default OK

        public PublicResponse(bool isSuccess)
        {
            IsSuccess = isSuccess;
        }

        public PublicResponse SetSuccess(object? value = null)
        {
            IsSuccess = true;
            Value = value;
            Error = null;
            StatusCode = 200;
            return this;
        }

        public PublicResponse SetCreated(object? value = null)
        {
            IsSuccess = true;
            Value = value;
            Error = null;
            StatusCode = 201;
            return this;
        }

        public PublicResponse SetError(string errorCode, string errorMessage)
        {
            IsSuccess = false;
            Value = null;
            Error = new Error
            {
                ErrorCode = errorCode,
                ErrorMessage = errorMessage
            };
            StatusCode = 400; // default bad request or custom later
            return this;
        }

        public PublicResponse BadRequest(string message, string? code = "BadRequest")
        {
            IsSuccess = false;
            Value = null;
            Error = new Error
            {
                ErrorCode = code ?? "BadRequest",
                ErrorMessage = message
            };
            StatusCode = 400;
            return this;
        }
    }
}
