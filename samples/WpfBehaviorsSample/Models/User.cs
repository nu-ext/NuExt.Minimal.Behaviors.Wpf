using Minimal.Mvvm;

namespace WpfBehaviorsSample.Models
{
    public partial class User : BindableBase
    {
        public User(string name, int age, string email, UserRole role = UserRole.User, bool isActive = true)
        {
            Name = name;
            Age = age;
            Email = email;
            Role = role;
            IsActive = isActive;
        }

        [Notify] private Guid _id = Guid.NewGuid();
        [Notify] private DateTimeOffset _createdAt = DateTimeOffset.UtcNow;
        [Notify, AlsoNotify(nameof(Display))] private bool _isActive = true;

        // Profile
        [Notify, AlsoNotify(nameof(Display))] private string _name = string.Empty;
        [Notify, AlsoNotify(nameof(Display))] private int _age;
        [Notify] private string _email = string.Empty;
        [Notify, AlsoNotify(nameof(Display))] private UserRole _role = UserRole.User;

        public string Display => $"{Name} ({Age}) • {Role}" + (IsActive ? string.Empty : " • inactive");

        public static bool LooksLikeEmail(string? value)
            => !string.IsNullOrWhiteSpace(value) && value!.Contains('@') && value!.Contains('.');

    }
    public enum UserRole
    {
        User,
        Manager,
        Admin
    }
}
