using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Model.Core; 
using Model.Data; // AllInformation и сериализаторы

namespace PetShelter
{
    public partial class MainWindow : Window
    {
        // Храним главную базу приютов в динамическом списке List
        private List<Shelter> _shelters;

        public MainWindow()
        {
            InitializeComponent();

            // Автоматически загружаем базу данных с Рабочего стола
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            _shelters = AllInformation.LoadOrCreateData(desktopPath);

            // Включаем встроенный делегат Func для валидации имени
            Shelter.CheckIfPetIsAllowed = (pet) => pet.Name.Length > 2;

            // Заполняем выпадающие списки стартовыми пунктами
            FillComboBoxes();
        }

        // Метод наполнения элементов ComboBox данными
        private void FillComboBoxes()
        {
            cmbShelters.Items.Add("Все приюты");
            foreach (Shelter s in _shelters)
            {
                cmbShelters.Items.Add(s.Name);
            }
            cmbShelters.SelectedIndex = 0;

            cmbPetTypes.Items.Add("Все виды");
            cmbPetTypes.Items.Add("Кот (Cat)");
            cmbPetTypes.Items.Add("Собака (Dog)");
            cmbPetTypes.Items.Add("Кролик (Rabbit)");
            cmbPetTypes.Items.Add("Попугай (Parrot)");
            cmbPetTypes.SelectedIndex = 0;

            cmbFormats.Items.Add("JSON (.json)");
            cmbFormats.Items.Add("XML (.xml)");
            cmbFormats.SelectedIndex = 0;
        }

        // определяет выбранный системный тип класса C#
        private Type GetSelectedType()
        {
            return cmbPetTypes.SelectedIndex switch
            {
                1 => typeof(Cat),
                2 => typeof(Dog),
                3 => typeof(Rabbit),
                4 => typeof(Parrot),
                _ => null
            };
        }

        // Срабатывает при каждом переключении фильтров — обновляет блок статистики (ICountable)
        private void FilterChanged(object sender, RoutedEventArgs e)
        {
            if (txtStatistics == null || _shelters == null) return;

            Type selectedType = GetSelectedType();

            if (cmbShelters.SelectedIndex > 0)
            {
                Shelter s = _shelters[cmbShelters.SelectedIndex - 1];

                txtStatistics.Text = $"Приют: \"{s.Name}\"\n" +
                                     $" Вместимость: {s.Capacity} мест\n" +
                                     $" Открытая зона: {(s.HasOpenTerritory ? "Да" : "Нет")}\n" +
                                     $"🐾 Всего подопечных: {s.Count()} шт.\n" +
                                     $" Животных этого вида: {s.Count(selectedType)} шт.\n" +
                                     $" Процентная доля вида: {s.Percentage(selectedType)}%";
            }
            else
            {
                //  перегрузки оператора ">" (first / second)
                Shelter leaderShelter = _shelters[0];
                int totalPets = 0;
                int totalCountOfType = 0;

                foreach (Shelter s in _shelters)
                {
                    if (chkOpenTerritory.IsChecked == true && !s.HasOpenTerritory) continue;

                    totalPets += s.Count();
                    totalCountOfType += s.Count(selectedType);

                    if (s > leaderShelter) leaderShelter = s;
                }

                int totalPercent = totalPets > 0 ? (int)Math.Round((double)totalCountOfType / totalPets * 100) : 0;

                txtStatistics.Text = $" Сводная статистика по базе:\n" +
                                     $"🐾 Общее число животных: {totalPets} шт.\n" +
                                     $" Всего данного вида в сети: {totalCountOfType} шт.\n" +
                                     $" Общий процент вида: {totalPercent}%\n" +
                                     $" Самый заполненный приют: \"{leaderShelter.Name}\"";
            }
        }

        // Срабатывает при переключении выпадающего списка форматов (JSON/XML)
        private void CmbFormats_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_shelters == null) return;

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            Serializer oldSerializer = AllInformation.CurrentSerializer;
            Serializer newSerializer = cmbFormats.SelectedIndex == 0 ? new JsonSerializer() : new XmlSerializer();

            if (oldSerializer.GetType() == newSerializer.GetType()) return;

            AllInformation.CurrentSerializer = newSerializer;
            _shelters = AllInformation.LoadOrCreateData(desktopPath);
            AllInformation.ConvertReports(desktopPath, oldSerializer, newSerializer);

            FilterChanged(null, null);
        }

        // нажатии на  поиска "TAKE ME HOME"
        private void BtnShowPets_Click(object sender, RoutedEventArgs e)
        {
            List<Pet> resultList = new List<Pet>();
            Type selectedType = GetSelectedType();
            int selectedShelterIndex = cmbShelters.SelectedIndex;

            if (selectedShelterIndex > 0)
            {
                Shelter s = _shelters[selectedShelterIndex - 1];

                if (chkOpenTerritory.IsChecked == true && !s.HasOpenTerritory)
                {
                    MessageBox.Show("Выбранный вами приют закрытого типа, а включена галочка открытых участков!",
                                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                resultList = s.Filter(selectedType, checkClaustrophobia: true);
            }
            else
            {
                foreach (Shelter s in _shelters)
                {
                    if (chkOpenTerritory.IsChecked == true && !s.HasOpenTerritory) continue;
                    resultList.AddRange(s.Filter(selectedType, checkClaustrophobia: false));
                }
            }

            // Открываем второе поп-арт окно таблицы результатов
            TableWindow okno = new TableWindow(resultList, _shelters, selectedShelterIndex);
            okno.Owner = this;
            okno.ShowDialog();

            FilterChanged(null, null);
        }
    }
}
