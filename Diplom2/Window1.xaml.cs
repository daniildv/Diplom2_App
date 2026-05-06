using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Diplom2
{
    public partial class Window1 : Window
    {
        private readonly int _currentAccountId;

        public Window1(int accountId)
        {
            InitializeComponent();
            _currentAccountId = accountId;
            Loaded += Window1_Loaded;
        }

        private void Window1_Loaded(object sender, RoutedEventArgs e)
        {
            LoadUserInfo();
            LoadRecentRequests();
            LoadBalance();   // <-- теперь реальный баланс
        }

        private void LoadUserInfo()
        {
            try
            {
                using (var context = new uk_managementEntities())
                {
                    var account = context.PersonalAccount
                                         .Include("Apartment")
                                         .Include("Apartment.House")
                                         .FirstOrDefault(pa => pa.account_id == _currentAccountId);

                    if (account != null)
                    {
                        txtOwnerFio.Text = account.owner_fio;
                        string address = $"{account.Apartment.House.address}, кв. {account.Apartment.number}";
                        txtAddress.Text = address;
                    }
                    else
                    {
                        MessageBox.Show("Данные о лицевом счёте не найдены.",
                                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных пользователя: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadRecentRequests()
        {
            try
            {
                using (var context = new uk_managementEntities())
                {
                    var requests = context.Request
                                          .Include("RequestType")
                                          .Where(r => r.account_id == _currentAccountId)
                                          .OrderByDescending(r => r.created_at)
                                          .Take(5)
                                          .ToList();

                    var items = new List<RequestItem>();
                    foreach (var req in requests)
                    {
                        items.Add(new RequestItem
                        {
                            RequestId = req.request_id,
                            Description = req.RequestType.name,
                            Status = req.status,
                            CreatedAt = req.created_at
                        });
                    }
                    lstRequests.ItemsSource = items;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заявок: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadBalance()
        {
            try
            {
                using (var context = new uk_managementEntities())
                {
                    decimal totalAccrued = context.Accrual
                        .Where(a => a.account_id == _currentAccountId)
                        .Select(a => (decimal?)a.amount)
                        .Sum() ?? 0;

                    decimal totalPaid = context.Payment
                        .Where(p => p.account_id == _currentAccountId)
                        .Select(p => (decimal?)p.amount)
                        .Sum() ?? 0;

                    decimal balance = totalPaid - totalAccrued;

                    if (balance < 0)
                    {
                        txtBalance.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
                        txtBalance.Text = $"−{Math.Abs(balance):N2} ₽";
                    }
                    else if (balance > 0)
                    {
                        txtBalance.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));
                        txtBalance.Text = $"+{balance:N2} ₽";
                    }
                    else
                    {
                        txtBalance.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#606266"));
                        txtBalance.Text = "0,00 ₽";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка расчёта баланса: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                txtBalance.Text = "Ошибка";
            }
        }

        private void BtnPaymentHistory_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Раздел находится в разработке.",
                            "Уведомление", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnSubmitReadings_Click(object sender, RoutedEventArgs e)
        {
            var readingsWindow = new Window2(_currentAccountId);
            readingsWindow.ShowDialog();
        }

        private void BtnCreateRequest_Click(object sender, RoutedEventArgs e)
        {
            var createRequestWindow = new Window4(_currentAccountId);
            createRequestWindow.ShowDialog();
            LoadRecentRequests();
        }
    }

    public class RequestItem
    {
        public int RequestId { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public DateTime? CreatedAt { get; set; }

        public string DisplayText => $"№{RequestId} — {Description}";
        public string DisplayDate => CreatedAt.HasValue
            ? $"Создана {CreatedAt.Value:dd.MM.yyyy}"
            : string.Empty;

        public Brush StatusColor
        {
            get
            {
                switch (Status)
                {
                    case "В работе": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1ECF1"));
                    case "Новая": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2"));
                    case "Выполнена": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D4EDDA"));
                    case "Закрыта": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E3E5"));
                    default: return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0F0F0"));
                }
            }
        }

        public Brush StatusForeground
        {
            get
            {
                switch (Status)
                {
                    case "В работе": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0C5460"));
                    case "Новая": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#991B1B"));
                    case "Выполнена": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#155724"));
                    case "Закрыта": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#383D41"));
                    default: return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));
                }
            }
        }
    }
}