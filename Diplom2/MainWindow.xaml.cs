using System;
using System.Linq;
using System.Windows;

namespace Diplom2
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // замените метод Button_Click в MainWindow.xaml.cs
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string login = LoginTextBox.Text.Trim();
                string password = PasswordBox.Password;

                if (string.IsNullOrWhiteSpace(login))
                {
                    ShowValidationError("Введите логин");
                    LoginTextBox.Focus();
                    return;
                }
                if (string.IsNullOrWhiteSpace(password))
                {
                    ShowValidationError("Введите пароль");
                    PasswordBox.Focus();
                    return;
                }

                using (var db = new uk_managementEntities())
                {
                    var user = db.User.FirstOrDefault(u =>
                        u.UserLogin == login && u.UserPassword == password);

                    if (user == null)
                    {
                        ShowErrorMessage("Ошибка авторизации",
                            "Неверный логин или пароль.");
                        PasswordBox.Password = "";
                        PasswordBox.Focus();
                        return;
                    }

                    if (user.RoleName == "Клиент")
                    {
                        // Существующая логика для жителя
                        string fullName = $"{user.UserSurname} {user.UserName} {user.UserPatronymic}";
                        var account = db.PersonalAccount.FirstOrDefault(pa => pa.owner_fio == fullName);

                        if (account == null)
                        {
                            ShowErrorMessage("Ошибка", "Не найден лицевой счёт для данного пользователя.");
                            return;
                        }

                        var window1 = new Window1(account.account_id);
                        window1.Show();
                    }
                    else // Менеджер или Администратор
                    {
                        var dispatcherWindow = new Window3();
                        dispatcherWindow.SetDispatcherName($"{user.UserSurname} {user.UserName} {user.UserPatronymic}");
                        dispatcherWindow.Show();
                    }
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Ошибка подключения",
                    $"Не удалось подключиться к базе данных: {ex.Message}");
            }
        }

        private void ShowValidationError(string message)
        {
            MessageBox.Show(message, "Ошибка ввода",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void ShowErrorMessage(string title, string message)
        {
            MessageBox.Show(message, title,
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}