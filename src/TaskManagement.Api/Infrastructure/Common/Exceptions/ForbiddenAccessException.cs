namespace TaskManagement.Api.Infrastructure.Common.Exceptions
{
    public class ForbiddenAccessException : Exception
    {
        public ForbiddenAccessException(string message = "Access forbidden.") : base(message) { }
    }
}
