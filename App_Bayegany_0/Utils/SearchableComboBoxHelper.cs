using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using BayeganyApp.Models;

namespace BayeganyApp.Utils
{
    /// <summary>
    /// قابلیت جستجو و فیلتر زنده در سطر اول لیست بازشونده (Popup) تمام کامبوباکس‌ها
    /// </summary>
    public static class SearchableComboBoxHelper
    {
        public static readonly DependencyProperty IsSearchEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsSearchEnabled",
                typeof(bool),
                typeof(SearchableComboBoxHelper),
                new PropertyMetadata(false, OnIsSearchEnabledChanged));

        public static bool GetIsSearchEnabled(DependencyObject obj) => (bool)obj.GetValue(IsSearchEnabledProperty);
        public static void SetIsSearchEnabled(DependencyObject obj, bool value) => obj.SetValue(IsSearchEnabledProperty, value);

        private static void OnIsSearchEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ComboBox cmb && (bool)e.NewValue)
            {
                AttachSearch(cmb);
            }
        }

        private static void AttachSearch(ComboBox cmb)
        {
            // جلوگیری از اتصال چندباره
            if (cmb.Tag as string == "SearchAttached")
                return;

            cmb.Tag = "SearchAttached";

            // گرفتن نمونه آیتم انتخاب‌شده قبل از فیلتر جهت بازیابی در صورت لغو
            object? originalSelection = null;
            TextBox? activeSearchBox = null;

            cmb.DropDownOpened += (s, e) =>
            {
                originalSelection = cmb.SelectedItem;

                // در صورتی که ساختار جستجو هنوز به پاپ‌آپ متصل نشده باشد
                EnsureSearchBoxInPopup(cmb, ref activeSearchBox);

                if (activeSearchBox != null)
                {
                    activeSearchBox.Text = "";
                    ClearFilter(cmb);

                    cmb.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        activeSearchBox.Focus();
                        Keyboard.Focus(activeSearchBox);
                    }), DispatcherPriority.Input);
                }
            };

            cmb.DropDownClosed += (s, e) =>
            {
                object? currentSelected = cmb.SelectedItem;

                // اگر آیتمی از قبل انتخاب شده بوده ولی در اثر فیلتر شدن نال شده و کاربر آیتم جدیدی انتخاب نکرده، برگردانده شود
                if (currentSelected == null && originalSelection != null)
                {
                    currentSelected = originalSelection;
                }

                // پاک‌کردن فیلتر برای نمایش مجدد همه آیتم‌ها در دفعات بعدی
                ClearFilter(cmb);

                if (currentSelected != null)
                {
                    cmb.SelectedItem = currentSelected;
                }

                if (activeSearchBox != null)
                {
                    activeSearchBox.Text = "";
                }
            };
        }

        private static void EnsureSearchBoxInPopup(ComboBox cmb, ref TextBox? searchBoxOut)
        {
            cmb.ApplyTemplate();

            if (cmb.Template.FindName("PART_Popup", cmb) is not Popup popup)
                return;

            Border? border = GetPopupBorder(popup);
            if (border == null)
                return;

            // تنظیم حداقل عرض پاپ‌آپ جهت خوانایی و جا داشتن کادر جستجو
            if (cmb.ActualWidth > 0)
            {
                border.MinWidth = Math.Max(280, cmb.ActualWidth);
            }

            // اگر قبلاً Grid جستجو تزریق شده، از همان استفاده کنیم
            if (border.Child is Grid existingGrid && existingGrid.Tag as string == "SearchableGrid")
            {
                if (searchBoxOut == null)
                {
                    searchBoxOut = FindVisualChild<TextBox>(existingGrid);
                }
                return;
            }

            if (border.Child == null)
                return;

            UIElement originalContent = border.Child;
            border.Child = null;

            var grid = new Grid
            {
                Tag = "SearchableGrid",
                Focusable = false
            };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // ساخت پنل بالای لیست (کادر جستجو)
            var searchPanel = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)), // #F8FAFC
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)), // #E2E8F0
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(6, 6, 6, 6),
                SnapsToDevicePixels = true
            };

            var boxBorder = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)), // #CBD5E1
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 3, 6, 3)
            };

            var boxGrid = new Grid();
            boxGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            boxGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            boxGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var icon = new TextBlock
            {
                Text = "🔍",
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139))
            };
            Grid.SetColumn(icon, 0);
            boxGrid.Children.Add(icon);

            var textGrid = new Grid();
            Grid.SetColumn(textGrid, 1);

            var searchBox = new TextBox
            {
                FontSize = 12.5,
                VerticalContentAlignment = VerticalAlignment.Center,
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                FlowDirection = FlowDirection.RightToLeft,
                FontFamily = cmb.FontFamily
            };

            var watermark = new TextBlock
            {
                Text = "جستجو و فیلتر در آیتم‌ها...",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false,
                FlowDirection = FlowDirection.RightToLeft,
                FontFamily = cmb.FontFamily
            };

            textGrid.Children.Add(searchBox);
            textGrid.Children.Add(watermark);
            boxGrid.Children.Add(textGrid);

            var clearBtn = new Button
            {
                Content = "✖",
                FontSize = 10,
                Width = 18,
                Height = 18,
                Padding = new Thickness(0),
                Margin = new Thickness(4, 0, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Visibility = Visibility.Collapsed,
                ToolTip = "پاک‌کردن متن جستجو"
            };
            Grid.SetColumn(clearBtn, 2);
            boxGrid.Children.Add(clearBtn);

            boxBorder.Child = boxGrid;
            searchPanel.Child = boxBorder;

            searchBox.TextChanged += (s, e) =>
            {
                string text = searchBox.Text;
                watermark.Visibility = string.IsNullOrEmpty(text) ? Visibility.Visible : Visibility.Collapsed;
                clearBtn.Visibility = string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible;
                ApplyFilter(cmb, text);
            };

            clearBtn.Click += (s, e) =>
            {
                searchBox.Text = "";
                searchBox.Focus();
            };

            searchBox.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Space)
                {
                    // جلوگیری از ارسال کلید Space به کامبوباکس (که پاپ‌آپ را می‌بندد)
                    int caret = searchBox.CaretIndex;
                    searchBox.Text = searchBox.Text.Insert(caret, " ");
                    searchBox.CaretIndex = caret + 1;
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape)
                {
                    cmb.IsDropDownOpen = false;
                    e.Handled = true;
                }
                else if (e.Key == Key.Enter)
                {
                    // انتخاب اولین آیتم فیلتر شده و بستن پاپ‌آپ
                    ICollectionView? view = GetView(cmb);
                    if (view != null)
                    {
                        foreach (var it in view)
                        {
                            cmb.SelectedItem = it;
                            break;
                        }
                    }
                    cmb.IsDropDownOpen = false;
                    e.Handled = true;
                }
                else if (e.Key == Key.Down)
                {
                    // حرکت به آیتم بعدی
                    NavigateItems(cmb, +1);
                    e.Handled = true;
                }
                else if (e.Key == Key.Up)
                {
                    // حرکت به آیتم قبلی
                    NavigateItems(cmb, -1);
                    e.Handled = true;
                }
            };

            // جلوگیری از کلیک‌های ناخواسته
            searchPanel.PreviewMouseDown += (s, e) => searchBox.Focus();
            boxBorder.PreviewMouseDown += (s, e) => searchBox.Focus();

            Grid.SetRow(searchPanel, 0);
            grid.Children.Add(searchPanel);

            Grid.SetRow(originalContent, 1);
            grid.Children.Add(originalContent);

            border.Child = grid;
            searchBoxOut = searchBox;
        }

        private static void NavigateItems(ComboBox cmb, int offset)
        {
            ICollectionView? view = GetView(cmb);
            if (view == null) return;

            var list = view.Cast<object>().ToList();
            if (list.Count == 0) return;

            int currentIndex = cmb.SelectedItem != null ? list.IndexOf(cmb.SelectedItem) : -1;
            int nextIndex = currentIndex + offset;

            if (nextIndex < 0) nextIndex = list.Count - 1;
            if (nextIndex >= list.Count) nextIndex = 0;

            cmb.SelectedItem = list[nextIndex];
        }

        private static Border? GetPopupBorder(Popup popup)
        {
            if (popup.Child is Border b)
                return b;

            if (popup.Child is FrameworkElement fe)
            {
                var prop = fe.GetType().GetProperty("Child");
                if (prop?.GetValue(fe) is Border b2)
                    return b2;

                return FindVisualChild<Border>(fe);
            }

            return null;
        }

        private static void ApplyFilter(ComboBox cmb, string query)
        {
            ICollectionView? view = GetView(cmb);
            if (view == null) return;

            if (string.IsNullOrWhiteSpace(query))
            {
                view.Filter = null;
            }
            else
            {
                view.Filter = item => ItemMatchesSearch(item, query);
            }
        }

        private static void ClearFilter(ComboBox cmb)
        {
            ICollectionView? view = GetView(cmb);
            if (view != null)
            {
                view.Filter = null;
            }
        }

        private static ICollectionView? GetView(ComboBox cmb)
        {
            if (cmb.ItemsSource != null)
                return CollectionViewSource.GetDefaultView(cmb.ItemsSource);

            return cmb.Items;
        }

        private static string NormalizePersian(string? text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text
                .Replace('ي', 'ی')
                .Replace('ك', 'ک')
                .Replace('\u200c', ' ')
                .Trim();
        }

        private static bool ItemMatchesSearch(object? item, string query)
        {
            if (item == null) return false;
            if (string.IsNullOrWhiteSpace(query)) return true;

            string normalizedQuery = NormalizePersian(query);
            if (string.IsNullOrEmpty(normalizedQuery)) return true;

            // فقط بر روی متنی که در آیتم به کاربر نمایش داده می‌شود (عنوان و کد) فیلتر انجام شود
            if (item is PersonItem p)
            {
                if (p.Id == 0 && !normalizedQuery.Contains("انتخاب") && !normalizedQuery.Contains("همه"))
                    return false;

                string visibleText = p.ToString();
                if (!string.IsNullOrWhiteSpace(p.NationalCode))
                {
                    visibleText += $" {p.NationalCode}";
                }
                return NormalizePersian(visibleText).IndexOf(normalizedQuery, StringComparison.OrdinalIgnoreCase) >= 0;
            }

            if (item is MainSubjectItem ms)
            {
                if (ms.Id == 0 && !normalizedQuery.Contains("انتخاب") && !normalizedQuery.Contains("همه"))
                    return false;

                string visibleText = ms.ToString();
                return NormalizePersian(visibleText).IndexOf(normalizedQuery, StringComparison.OrdinalIgnoreCase) >= 0;
            }

            if (item is SubSubject1Item ss1)
            {
                if (ss1.Id == 0 && !normalizedQuery.Contains("انتخاب") && !normalizedQuery.Contains("همه"))
                    return false;

                string visibleText = ss1.ToString();
                return NormalizePersian(visibleText).IndexOf(normalizedQuery, StringComparison.OrdinalIgnoreCase) >= 0;
            }

            if (item is SubSubject2Item ss2)
            {
                if (ss2.Id == 0 && !normalizedQuery.Contains("انتخاب") && !normalizedQuery.Contains("همه"))
                    return false;

                string visibleText = ss2.ToString();
                return NormalizePersian(visibleText).IndexOf(normalizedQuery, StringComparison.OrdinalIgnoreCase) >= 0;
            }

            string str = item.ToString() ?? "";
            return NormalizePersian(str).IndexOf(normalizedQuery, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;

                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }
    }
}
