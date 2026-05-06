using System;
using System.Data.Entity;
using System.Linq;
using System.Windows;

namespace Diplom2
{
    public partial class Window4 : Window
    {
        private readonly int _currentAccountId;

        /// <summary>
        /// Конструктор принимает ID лицевого счёта текущего жильца.
        /// </summary>
        public Window4(int accountId)
        {
            InitializeComponent();
            _currentAccountId = accountId;
            Loaded += Window4_Loaded;
        }

        private void Window4_Loaded(object sender, RoutedEventArgs e)
        {
            LoadRequestTypes();
        }

        // Загружаем все типы заявок из справочника
        private void LoadRequestTypes()
        {
            try
            {
                using (var context = new uk_managementEntities())
                {
                    var requestTypes = context.RequestType
                                              .OrderBy(rt => rt.name)
                                              .ToList();

                    cmbRequestType.ItemsSource = requestTypes;
                    cmbRequestType.SelectedIndex = 0;   // выбираем первый по умолчанию
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки типов заявок: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Отправка заявки
        private void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, что выбран тип
            if (cmbRequestType.SelectedValue == null)
            {
                MessageBox.Show("Выберите тип заявки.", "Предупреждение",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedTypeId = (int)cmbRequestType.SelectedValue;
            var description = txtDescription.Text.Trim();

            if (string.IsNullOrWhiteSpace(description))
            {
                MessageBox.Show("Введите описание проблемы.", "Предупреждение",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                txtDescription.Focus();
                return;
            }

            try
            {
                using (var context = new uk_managementEntities())
                {
                    // Создаём новую заявку
                    var newRequest = new Request
                    {
                        account_id = _currentAccountId,
                        type_id = selectedTypeId,
                        description = description,
                        status = "Новая",              // значение по умолчанию из схемы
                        created_at = DateTime.Now,
                        // appointed_master, completed_at, rating – оставляем null
                        appointed_master = null,
                        completed_at = null,
                        rating = null,
                        comment_close = null
                    };

                    context.Request.Add(newRequest);
                    context.SaveChanges();
                }

                MessageBox.Show("Заявка успешно создана!", "Успех",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания заявки: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Отмена
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}