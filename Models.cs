namespace Experimental56;

public enum UserRole { Admin, Teacher, Student }
public enum TestStatus { Draft, Published, Archived }
public enum QuestionType { SingleChoice, Numeric }

public sealed class User
{
    public long Id { get; set; }
    public string Login { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public UserRole Role { get; set; }
    public string Salt { get; set; } = "";
    public string Hash { get; set; } = "";
    public bool IsActive { get; set; }
}

public sealed class SessionUser
{
    public long Id { get; init; }
    public string Login { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public UserRole Role { get; init; }
}
