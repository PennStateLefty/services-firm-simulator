namespace EmployeeService.Exceptions;

public class EmailAlreadyExistsException : InvalidOperationException
{
    public string Email { get; }

    public EmailAlreadyExistsException(string email) 
        : base($"An employee with email address '{email}' already exists.")
    {
        Email = email;
    }

    public EmailAlreadyExistsException(string email, string message) 
        : base(message)
    {
        Email = email;
    }
}
