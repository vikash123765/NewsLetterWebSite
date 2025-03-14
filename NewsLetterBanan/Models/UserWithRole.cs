public class UserWithRole
{
    // User ID (from AspNetUsers table)
    public string UserId { get; set; }

    // UserName (from AspNetUsers table)
    public string UserName { get; set; }

    // Role name (from AspNetRoles table)
    public string Role { get; set; }

    // Combined display text (e.g., "john_doe (Role: Admin)")
    public string UserNameWithRole { get; set; }
}