using Google.Cloud.Translation.V2;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace LocalChat
{
	/// <summary>
	/// Interaction logic for SettingsOverlay.xaml
	/// </summary>
	public partial class SettingsOverlay : UserControl
	{
		public delegate void DoneCallback(string[] langCodes, bool autoTranlate);
		private DoneCallback callback;
		private IList<Language> langs;

		public SettingsOverlay()
		{
			InitializeComponent();
		}

		public void Show(TranslationClient translationClient, string[] selectedLangCodes, bool autoTranlate, DoneCallback callback)
		{
			this.callback = callback;
			Visibility = Visibility.Visible;
			autoTranlateCheckBox.IsChecked = autoTranlate;
			if (langs == null) LoadLangs(translationClient, selectedLangCodes);
			else SelectLangs(selectedLangCodes);
		}

		private async void LoadLangs(TranslationClient translationClient, string[] selectedLangCodes)
		{
			// loads supported langs from google
			loadingGrid.Visibility = Visibility.Visible;
			langs = await translationClient.ListLanguagesAsync();
			loadingGrid.Visibility = Visibility.Hidden;

			// fill list boxes
			foreach (var lang in langs)
			{
				if (string.IsNullOrEmpty(lang.Code)) continue;
				string content = string.Format("{0} ({1})", new CultureInfo(lang.Code).DisplayName, lang.Code);

				var item1 = new ListBoxItem();
				item1.Content = content;
				item1.Tag = lang.Code;
				langList1.Items.Add(item1);

				var item2 = new ListBoxItem();
				item2.Content = content;
				item2.Tag = lang.Code;
				langList2.Items.Add(item2);
			}

			// select langs from settings
			SelectLangs(selectedLangCodes);
		}

		private void SelectLangs(string[] selectedLangCodes)
		{
			if (selectedLangCodes == null || selectedLangCodes.Length != 2) return;
			SelectLangItem(langList1, selectedLangCodes[0]);
			SelectLangItem(langList2, selectedLangCodes[1]);
		}

		private void SelectLangItem(ListBox listBox, string langCode)
		{
			int index = 0;
			foreach (ListBoxItem item in listBox.Items)
			{
				string itemCode = (string)item.Tag;
				if (itemCode == langCode)
				{
					listBox.SelectedIndex = index;
					break;
				}

				++index;
			}
		}

		private void doneButton_Click(object sender, RoutedEventArgs e)
		{
			Visibility = Visibility.Hidden;
			if (langList1.SelectedIndex == -1 || langList2.SelectedIndex == -1)
			{
				callback(null, false);
			}
			else
			{
				var item1 = (ListBoxItem)langList1.SelectedValue;
				var item2 = (ListBoxItem)langList2.SelectedValue;
				var langCodes = new string[2]
				{
					(string)item1.Tag,
					(string)item2.Tag
				};
				callback(langCodes, autoTranlateCheckBox.IsChecked == true);
			}
		}
	}
}
