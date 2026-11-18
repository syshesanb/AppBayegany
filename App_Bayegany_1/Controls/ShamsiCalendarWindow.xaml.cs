using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BayeganyApp.Controls
{
    public partial class ShamsiCalendarWindow : Window
    {
        private readonly PersianCalendar _pc = new PersianCalendar();
        private int _currentYear;
        private int _currentMonth;

        public string SelectedDate { get; private set; } = string.Empty;

        private static readonly string[] MonthNames = {
            "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
            "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"
        };

        public ShamsiCalendarWindow(string initialDate = "")
        {
            InitializeComponent();

            foreach (var month in MonthNames)
            {
                CmbMonth.Items.Add(month);
            }

            int currentSysYear = _pc.GetYear(DateTime.Now);
            for (int y = 1380; y <= 1430; y++)
            {
                CmbYear.Items.Add(y);
            }

            if (!string.IsNullOrWhiteSpace(initialDate) && initialDate.Length == 10)
            {
                var parts = initialDate.Split('/');
                if (parts.Length == 3 &&
                    int.TryParse(parts[0], out int py) &&
                    int.TryParse(parts[1], out int pm))
                {
                    _currentYear = py;
                    _currentMonth = pm;
                }
                else
                {
                    SetToToday();
                }
            }
            else
            {
                SetToToday();
            }

            CmbMonth.SelectedIndex = _currentMonth - 1;
            CmbYear.SelectedItem = _currentYear;

            RenderCalendar();
        }

        private void SetToToday()
        {
            DateTime now = DateTime.Now;
            _currentYear = _pc.GetYear(now);
            _currentMonth = _pc.GetMonth(now);
        }

        private void RenderCalendar()
        {
            GridDays.Children.Clear();

            if (_currentYear < 1380 || _currentYear > 1430 || _currentMonth < 1 || _currentMonth > 12)
                return;

            DateTime firstDayOfMonth = _pc.ToDateTime(_currentYear, _currentMonth, 1, 0, 0, 0, 0);
            DayOfWeek dow = _pc.GetDayOfWeek(firstDayOfMonth);

            int offset = dow switch
            {
                DayOfWeek.Saturday => 0,
                DayOfWeek.Sunday => 1,
                DayOfWeek.Monday => 2,
                DayOfWeek.Tuesday => 3,
                DayOfWeek.Wednesday => 4,
                DayOfWeek.Thursday => 5,
                DayOfWeek.Friday => 6,
                _ => 0
            };

            for (int i = 0; i < offset; i++)
            {
                GridDays.Children.Add(new Border());
            }

            int daysInMonth = _pc.GetDaysInMonth(_currentYear, _currentMonth);

            DateTime today = DateTime.Now;
            int todayYear = _pc.GetYear(today);
            int todayMonth = _pc.GetMonth(today);
            int todayDay = _pc.GetDayOfMonth(today);

            for (int day = 1; day <= daysInMonth; day++)
            {
                int currentDayNumber = day;
                Button dayButton = new Button
                {
                    Content = currentDayNumber.ToString(),
                    Margin = new Thickness(2),
                    Height = 32,
                    FontSize = 13,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Background = Brushes.White,
                    Foreground = Brushes.Black,
                    BorderThickness = new Thickness(1),
                    BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#E2E8F0")!
                };

                if (_currentYear == todayYear && _currentMonth == todayMonth && day == todayDay)
                {
                    dayButton.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#DBEAFE")!;
                    dayButton.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#2563EB")!;
                    dayButton.FontWeight = FontWeights.Bold;
                }

                dayButton.Click += (s, e) =>
                {
                    SelectedDate = $"{_currentYear:D4}/{_currentMonth:D2}/{currentDayNumber:D2}";
                    DialogResult = true;
                    Close();
                };

                GridDays.Children.Add(dayButton);
            }
        }

        private void CmbMonth_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbMonth.SelectedIndex >= 0)
            {
                _currentMonth = CmbMonth.SelectedIndex + 1;
                RenderCalendar();
            }
        }

        private void CmbYear_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbYear.SelectedItem != null && int.TryParse(CmbYear.SelectedItem.ToString(), out int y))
            {
                _currentYear = y;
                RenderCalendar();
            }
        }

        private void BtnPrevMonth_Click(object sender, RoutedEventArgs e)
        {
            if (_currentMonth > 1)
            {
                _currentMonth--;
            }
            else if (_currentYear > 1380)
            {
                _currentYear--;
                _currentMonth = 12;
            }
            CmbMonth.SelectedIndex = _currentMonth - 1;
            CmbYear.SelectedItem = _currentYear;
        }

        private void BtnNextMonth_Click(object sender, RoutedEventArgs e)
        {
            if (_currentMonth < 12)
            {
                _currentMonth++;
            }
            else if (_currentYear < 1430)
            {
                _currentYear++;
                _currentMonth = 1;
            }
            CmbMonth.SelectedIndex = _currentMonth - 1;
            CmbYear.SelectedItem = _currentYear;
        }

        private void BtnToday_Click(object sender, RoutedEventArgs e)
        {
            DateTime now = DateTime.Now;
            SelectedDate = $"{_pc.GetYear(now):D4}/{_pc.GetMonth(now):D2}/{_pc.GetDayOfMonth(now):D2}";
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
