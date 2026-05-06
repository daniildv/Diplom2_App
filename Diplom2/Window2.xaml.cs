using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;

namespace Diplom2
{
    public partial class Window2 : Window
    {
        private readonly int _currentAccountId;
        // список соответствий: индекс строки -> meter_id
        private readonly Dictionary<int, int> _rowToMeterId = new Dictionary<int, int>();

        public Window2(int accountId)
        {
            InitializeComponent();
            _currentAccountId = accountId;
            Loaded += Window2_Loaded;
        }

        private void Window2_Loaded(object sender, RoutedEventArgs e)
        {
            LoadMeters();
        }

        // Загружаем активные счётчики для лицевого счёта
        private void LoadMeters()
        {
            try
            {
                using (var context = new uk_managementEntities())
                {
                    // Получаем лицевой счёт, чтобы вывести ФИО и номер ЛС
                    var account = context.PersonalAccount.FirstOrDefault(pa => pa.account_id == _currentAccountId);
                    if (account != null)
                    {
                        txtOwnerFio.Text = account.owner_fio;
                        txtAccountNumber.Text = $"ЛС: {account.account_id}";
                    }

                    // Активные приборы учёта, отсортированные по типу услуги
                    var meters = context.MeterDevice
                                        .Where(m => m.account_iаd == _currentAccountId && m.is_active == true)
                                        .OrderBy(m => m.service_type)
                                        .ToList();

                    if (meters.Count == 0)
                    {
                        MessageBox.Show("Нет активных приборов учёта.", "Информация",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // Ограничимся максимум 3 строками (под нашу вёрстку)
                    int row = 0;
                    foreach (var meter in meters.Take(3))
                    {
                        if (row >= 3) break;

                        // Последнее показание
                        var lastReading = context.MeterReading
                                                 .Where(r => r.meter_id == meter.meter_id)
                                                 .OrderByDescending(r => r.reading_date)
                                                 .FirstOrDefault();

                        UpdateRow(row, meter, lastReading);
                        _rowToMeterId[row] = meter.meter_id;
                        row++;
                    }

                    // Скрываем неиспользуемые строки (например, если счётчиков меньше 3)
                    for (int i = row; i < 3; i++)
                        SetRowVisibility(i, false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки приборов учёта: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateRow(int rowIndex, MeterDevice meter, MeterReading lastReading)
        {
            string serviceName;
            switch (meter.service_type)
            {
                case "ХВС": serviceName = "Холодная вода (ХВС)"; break;
                case "ГВС": serviceName = "Горячая вода (ГВС)"; break;
                case "ЭЭ": serviceName = "Электроэнергия"; break;
                case "Отопление": serviceName = "Отопление"; break;
                default: serviceName = meter.service_type; break;
            }

            switch (rowIndex)
            {
                case 0:
                    lblService1.Text = serviceName;
                    lblLastValue1.Text = lastReading?.value.ToString("0.000") ?? "–";
                    lblLastDate1.Text = lastReading?.reading_date.ToString("dd.MM.yyyy") ?? "–";
                    txtNewValue1.Text = "";
                    break;
                case 1:
                    lblService2.Text = serviceName;
                    lblLastValue2.Text = lastReading?.value.ToString("0.000") ?? "–";
                    lblLastDate2.Text = lastReading?.reading_date.ToString("dd.MM.yyyy") ?? "–";
                    txtNewValue2.Text = "";
                    break;
                case 2:
                    lblService3.Text = serviceName;
                    lblLastValue3.Text = lastReading?.value.ToString("0.000") ?? "–";
                    lblLastDate3.Text = lastReading?.reading_date.ToString("dd.MM.yyyy") ?? "–";
                    txtNewValue3.Text = "";
                    break;
            }
        }

        private void SetRowVisibility(int rowIndex, bool visible)
        {
            switch (rowIndex)
            {
                case 0:
                    lblService1.Visibility = lblLastValue1.Visibility = lblLastDate1.Visibility =
                        txtNewValue1.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
                    break;
                case 1:
                    lblService2.Visibility = lblLastValue2.Visibility = lblLastDate2.Visibility =
                        txtNewValue2.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
                    break;
                case 2:
                    lblService3.Visibility = lblLastValue3.Visibility = lblLastDate3.Visibility =
                        txtNewValue3.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
                    break;
            }
        }

        private void BtnSubmitAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var newReadings = new List<Tuple<int, string>>(); // meter_id, textValue

                // Собираем непустые значения
                if (_rowToMeterId.ContainsKey(0) && !string.IsNullOrWhiteSpace(txtNewValue1.Text))
                    newReadings.Add(Tuple.Create(_rowToMeterId[0], txtNewValue1.Text.Trim()));
                if (_rowToMeterId.ContainsKey(1) && !string.IsNullOrWhiteSpace(txtNewValue2.Text))
                    newReadings.Add(Tuple.Create(_rowToMeterId[1], txtNewValue2.Text.Trim()));
                if (_rowToMeterId.ContainsKey(2) && !string.IsNullOrWhiteSpace(txtNewValue3.Text))
                    newReadings.Add(Tuple.Create(_rowToMeterId[2], txtNewValue3.Text.Trim()));

                if (newReadings.Count == 0)
                {
                    MessageBox.Show("Введите хотя бы одно показание.", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var context = new uk_managementEntities())
                {
                    foreach (var item in newReadings)
                    {
                        if (!decimal.TryParse(item.Item2, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out decimal value) || value < 0)
                        {
                            MessageBox.Show("Введите корректное числовое значение.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        var reading = new MeterReading
                        {
                            meter_id = item.Item1,
                            reading_date = DateTime.Now,
                            value = value,
                            source = "Житель",
                            confirmed = false   // новые показания ждут подтверждения
                        };
                        context.MeterReading.Add(reading);
                    }
                    context.SaveChanges();
                }

                MessageBox.Show("Показания успешно переданы!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения показаний: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}