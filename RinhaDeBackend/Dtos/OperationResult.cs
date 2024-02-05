using Microsoft.AspNetCore.Mvc;

namespace ProjeteMais.Shared;

public class OperationResult<T>
{
    public bool IsSuccess { get; }
    public string Message { get; }
    public T? Data { get; }
    public int HttpStatusCode { get; }

    public OperationResult(bool isSuccess, string message, T? data, int httpStatusCode)
    {
        IsSuccess = isSuccess;
        Message = message;
        Data = data;
        HttpStatusCode = httpStatusCode;
    }

    public IActionResult GetResponseWithStatusCode()
    {
        if (IsSuccess)
        {
            return new OkObjectResult(new { Message, Data, HttpStatusCode })
            {
                StatusCode = HttpStatusCode
            };
        }

        return new ObjectResult(new { Message, Data, HttpStatusCode })
        {
            StatusCode = HttpStatusCode
        };
    }
}