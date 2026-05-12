using System.Net;

namespace ST.Shared.Exceptions;

public class BusinessException : Exception
{
	public int StatusCode { get; }
	public string? ErrorCode { get; }

	public BusinessException(string message, int statusCode = (int)HttpStatusCode.BadRequest, string? errorCode = null) : base(message)
	{
		StatusCode = statusCode;
		ErrorCode = errorCode;
	}
}
