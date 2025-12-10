using System;

namespace FusionComms.Utilities
{
    public class NotFoundException : Exception
    {
        public NotFoundException()
        {

        }

        public NotFoundException(string message) : base(message)
        {

        }

        public NotFoundException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
    public class MontyException : Exception
    {
        public MontyException()
        {

        }

        public MontyException(string message) : base(message)
        {

        }

        public MontyException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}
