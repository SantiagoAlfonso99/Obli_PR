
public class UserDTO
{
    public string UserName { get; set; }
    public string Password { get; set; }

    public UserDTO(User user)
    {
        UserName = user.UserName;
        Password = user.Password;
    }
}