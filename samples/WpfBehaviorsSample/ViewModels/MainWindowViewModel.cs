using Minimal.Mvvm;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using WpfBehaviorsSample.Models;

namespace WpfBehaviorsSample.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel() 
        {
            Users =
            [
                new User("Alice Johnson", 29, "alice@example.com", UserRole.Manager),
                new User("Bob Smith", 34, "bob@example.com", UserRole.User),
                new User("Carol Clark", 41, "carol@example.com", UserRole.Admin, isActive: false),
                new User("David Brown", 23, "david@example.com", UserRole.User)
            ];
        }

        [Notify] private User? _selectedUser;

        public ObservableCollection<User> Users { get; }

        public ICommand SayHelloCommand { get; } = 
            new RelayCommand<User>(u => MessageBox.Show($"Hello, {u.Name}!"), static u => u is not null);
    }
}
