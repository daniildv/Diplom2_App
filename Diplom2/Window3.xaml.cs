using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Diplom2
{
    public partial class Window3 : Window
    {
        // все заявки с подгруженными связями
        private List<Request> _allRequests;
        // список активных лицевых счетов для popup
        private List<PersonalAccount> _allAccounts;

        public Window3()
        {
            InitializeComponent();
            Loaded += Window3_Loaded;
        }

        /// <summary>
        /// Устанавливает ФИО диспетчера в шапке окна.
        /// </summary>
        public void SetDispatcherName(string fullName)
        {
            txtDispatcherName.Text = $"Диспетчер {fullName}";
        }

        private void Window3_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
            LoadAccounts();
        }

        // Загружаем все заявки с нужными связанными таблицами
        private void LoadData()
        {
            try
            {
                using (var context = new uk_managementEntities())
                {
                    // Подгружаем RequestType, PersonalAccount, Apartment и House
                    _allRequests = context.Request
                                         .Include("RequestType")
                                         .Include("PersonalAccount.Apartment.House")
                                         .ToList();
                }
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заявок: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Загружаем активные лицевые счета для создания заявки по телефону
        private void LoadAccounts()
        {
            try
            {
                using (var context = new uk_managementEntities())
                {
                    _allAccounts = context.PersonalAccount
                                          .Where(pa => pa.is_active == true)
                                          .OrderBy(pa => pa.owner_fio)
                                          .ToList();
                    cmbAccounts.ItemsSource = _allAccounts;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки лицевых счетов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Применяем фильтры и обновляем список на экране
        private void ApplyFilters()
        {
            if (_allRequests == null) return;

            var filtered = _allRequests.AsEnumerable();

            // Фильтр по статусу
            string selectedStatus = (cmbStatusFilter.SelectedItem as ComboBoxItem)?.Content as string;
            if (!string.IsNullOrWhiteSpace(selectedStatus) && selectedStatus != "Все статусы")
            {
                filtered = filtered.Where(r => r.status == selectedStatus);
            }

            // Фильтр по адресу (ищем подстроку в объединённом адресе)
            string addressPart = txtAddressFilter.Text.Trim();
            if (!string.IsNullOrWhiteSpace(addressPart) && addressPart.ToLower() != "адрес")
            {
                filtered = filtered.Where(r =>
                {
                    var acc = r.PersonalAccount;
                    if (acc == null) return false;
                    string fullAddress = $"{acc.Apartment?.House?.address}, кв. {acc.Apartment?.number}";
                    return fullAddress.ToLower().Contains(addressPart.ToLower());
                });
            }

            // Фильтр по дате создания (если выбрана)
            if (dpDateFilter.SelectedDate.HasValue)
            {
                DateTime filterDate = dpDateFilter.SelectedDate.Value.Date;
                filtered = filtered.Where(r => r.created_at.HasValue &&
                                               r.created_at.Value.Date == filterDate);
            }

            // Преобразуем в список DispatchRequestItem для отображения
            var items = filtered.Select(r => new DispatchRequestItem(r)).ToList();
            lstRequests.ItemsSource = items;
        }

        // События изменения фильтров
        private void Filters_Changed(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        // Кнопка "+ Создать заявку (по телефону)" – показывает popup с выбором ЛС
        private void BtnCreateRequest_Click(object sender, RoutedEventArgs e)
        {
            if (_allAccounts == null || _allAccounts.Count == 0)
            {
                MessageBox.Show("Нет активных лицевых счетов.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            cmbAccounts.SelectedIndex = 0;
            popupSelectAccount.IsOpen = true;
        }

        // Кнопка в popup – создаёт заявку для выбранного ЛС
        private void BtnConfirmCreateRequest_Click(object sender, RoutedEventArgs e)
        {
            if (cmbAccounts.SelectedValue == null)
            {
                MessageBox.Show("Выберите лицевой счёт.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int selectedAccountId = (int)cmbAccounts.SelectedValue;
            popupSelectAccount.IsOpen = false;

            // Открываем окно создания заявки, как для жителя
            var createWindow = new Window4(selectedAccountId);
            createWindow.ShowDialog();

            // После создания обновляем список заявок
            LoadData();
        }
    }

    // Класс для отображения одной заявки в списке диспетчера (отдельный от RequestItem в Window1)
    public class DispatchRequestItem
    {
        private readonly Request _request;

        public DispatchRequestItem(Request request)
        {
            _request = request;
        }

        public string DisplayRequestInfo =>
            $"№{_request.request_id} — {_request.RequestType?.name ?? "Неизвестный тип"}";

        public string DisplayAddress
        {
            get
            {
                var acc = _request.PersonalAccount;
                if (acc == null) return "Адрес не указан";
                return $"{acc.Apartment?.House?.address}, кв. {acc.Apartment?.number}";
            }
        }

        public string DisplayMaster
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_request.appointed_master))
                    return $"Назначен: {_request.appointed_master}";
                if (_request.status == "Выполнена" || _request.status == "Закрыта")
                    return "Исполнитель не указан";
                return "";
            }
        }

        public string Status => _request.status;

        public Brush StatusColor
        {
            get
            {
                switch (_request.status)
                {
                    case "Новая":
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2"));
                    case "Назначена":
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8D5B7"));
                    case "В работе":
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1ECF1"));
                    case "Выполнена":
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D4EDDA"));
                    case "Закрыта":
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E3E5"));
                    case "Отклонена":
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F9E1E1"));
                    default:
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0F0F0"));
                }
            }
        }

        public Brush StatusForeground
        {
            get
            {
                switch (_request.status)
                {
                    case "Новая":
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#991B1B"));
                    case "Назначена":
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B5E3C"));
                    case "В работе":
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0C5460"));
                    case "Выполнена":
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#155724"));
                    case "Закрыта":
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#383D41"));
                    case "Отклонена":
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7F1D1D"));
                    default:
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));
                }
            }
        }
    }
}