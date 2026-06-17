namespace GameBackend.Core.Utils
{
    public class GameNotFoundException : Exception
    {
        public GameNotFoundException(string message) : base(message) { }
    }

    public class GameUnauthorizedException : Exception
    {
        public GameUnauthorizedException(string message) : base(message) { }
    }

    public class GameValidationException : Exception
    {
        public GameValidationException(string message) : base(message) { }
    }

    public class GameConflictException : Exception
    {
        public GameConflictException(string message) : base(message) { }
    }
}
