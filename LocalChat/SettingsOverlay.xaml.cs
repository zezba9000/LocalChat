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

		public void Show(TranslationClient translationClient, string[] selectedLangCodes, DoneCallback callback)
		{
			this.callback = callback;
			Visibility = Visibility.Visible;
			if (langs == null) LoadLangs(translationClient);
			else Init(selectedLangCodes);
		}

		private async void LoadLangs(TranslationClient translationClient)
		{
			loadingGrid.Visibility = Visibility.Visible;
			langs = await translationClient.ListLanguagesAsync();
			loadingGrid.Visibility = Visibility.Hidden;
			Init(null);
		}

		private void Init(string[] selectedLangCodes)
		{
			if (selectedLangCodes == null)
			{
				foreach (var lang in langs)
				{
					if (string.IsNullOrEmpty(lang.Code)) continue;
					string content = string.Format("{0} :{1}", new CultureInfo(lang.Code).DisplayName, lang.Code);

					var item1 = new ListBoxItem();
					item1.Content = content;
					langList1.Items.Add(item1);

					var item2 = new ListBoxItem();
					item2.Content = content;
					langList2.Items.Add(item2);
				}
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
					((string)item1.Content).Split(':')[1],
					((string)item2.Content).Split(':')[1]
				};
				callback(langCodes, autoTranlateCheckBox.IsChecked == true);
			}
		}
	}
}
