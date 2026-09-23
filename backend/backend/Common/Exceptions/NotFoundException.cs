namespace backend.Common.Exceptions;

public class NotFoundException(string message = "The requested resource was not found.") : Exception(message);
