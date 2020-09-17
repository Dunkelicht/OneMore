﻿//************************************************************************************************
// Copyright © 2020 Steven M Cohn.  All rights reserved.
//************************************************************************************************

#pragma warning disable CS3003  // Type is not CLS-compliant
#pragma warning disable IDE1006 // Words must begin with upper case

namespace River.OneMoreAddIn.Dialogs
{
	using River.OneMoreAddIn.Helpers.Settings;
	using System;
	using System.Windows.Forms;
	using Resx = River.OneMoreAddIn.Properties.Resources;


	internal partial class OutlineDialog : LocalizableForm
	{

		public OutlineDialog()
		{
			InitializeComponent();

			TagSymbol = 0;

			if (NeedsLocalizing())
			{
				Text = Resx.OutlineDialog_Text;
				tooltip.SetToolTip(numberingBox, Resx.OutlineDialog_numberingBox_Tooltip);
				tooltip.SetToolTip(indentTagBox, Resx.OutlineDialog_indentTagBox_Tooltip);

				Localize(new string[]
				{
					"numberingGroup",
					"numberingBox",
					"alphaRadio",
					"numRadio",
					"cleanBox",
					"indentationsGroup",
					"indentBox",
					"indentTagBox",
					"removeTagsBox",
					"tagLabel",
					"okButton",
					"cancelButton"
				});
			}

			RestoreSettings();
		}


		private void RestoreSettings()
		{
			var provider = new SettingsProvider();
			var settings = provider.GetCollection("outline");
			if (settings != null)
			{
				numberingBox.Checked = settings.Get<bool>("addNumbering");
				if (numberingBox.Checked)
				{
					alphaRadio.Checked = settings.Get<bool>("alphaNumbering");
					numRadio.Checked = settings.Get<bool>("numericNumbering");
				}

				cleanBox.Checked = settings.Get<bool>("cleanupNumbering");
				indentBox.Checked = settings.Get<bool>("indent");

				indentTagBox.Checked = settings.Get<bool>("indentTagged");
				if (indentTagBox.Checked)
				{
					removeTagsBox.Checked = settings.Get<bool>("removeTags");

					var symbol = settings.Get<int>("tagSymbol");
					if (symbol > 0)
					{
						using (var dialog = new TagPickerDialog(0, 0))
						{
							var glyph = dialog.GetGlyph(symbol);
							if (glyph != null)
							{
								tagButton.Text = null;
								tagButton.Image = glyph;
							}
						}
					}
				}
			}
		}


		public bool AlphaNumbering => alphaRadio.Enabled && alphaRadio.Checked;

		public bool NumericNumbering => numRadio.Enabled && numRadio.Checked;

		public bool CleanupNumbering => cleanBox.Checked;

		public bool Indent => indentBox.Checked;

		public bool IndentTagged => indentTagBox.Checked;

		public bool RemoveTags => removeTagsBox.Checked;

		public int TagSymbol { get; private set; }


		protected override void OnShown(EventArgs e)
		{
			Location = new System.Drawing.Point(Location.X, Location.Y - (Height / 2));
			UIHelper.SetForegroundWindow(this);
		}


		private void numberingBox_CheckedChanged(object sender, EventArgs e)
		{
			alphaRadio.Enabled = numberingBox.Checked;
			numRadio.Enabled = numberingBox.Checked;
			SetOK();
		}


		private void cleanBox_CheckedChanged(object sender, EventArgs e)
		{
			SetOK();
		}


		private void indentBox_CheckedChanged(object sender, EventArgs e)
		{
			SetOK();
		}


		private void indentTagBox_CheckedChanged(object sender, EventArgs e)
		{
			tagButton.Enabled = removeTagsBox.Enabled = indentTagBox.Checked;
			SetOK();
		}


		private void tagButton_Click(object sender, EventArgs e)
		{
			var location = PointToScreen(tagButton.Location);

			using (var dialog = new TagPickerDialog(
				location.X + tagButton.Bounds.Location.X - tagButton.Width,
				location.Y + tagButton.Bounds.Location.Y))
			{
				if (dialog.ShowDialog(this) == DialogResult.OK)
				{
					var glyph = dialog.GetGlyph();
					if (glyph != null)
					{
						tagButton.Text = null;
						tagButton.Image = glyph;
					}
					else
					{
						tagButton.Text = "?";
					}

					TagSymbol = dialog.Symbol;
				}
			}
		}


		private void SetOK()
		{
			okButton.Enabled = 
				numberingBox.Checked || cleanBox.Checked || 
				indentBox.Checked || indentTagBox.Checked;
		}


		private void okButton_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;

			var settings = new SettingCollection("outline");
			settings.Add("addNumbering", numberingBox.Checked);
			settings.Add("alphaNumbering", AlphaNumbering);
			settings.Add("numericNumbering", NumericNumbering);
			settings.Add("cleanupNumbering", CleanupNumbering);
			settings.Add("indent", Indent);
			settings.Add("indentTagged", IndentTagged);
			settings.Add("removeTags", RemoveTags);
			settings.Add("tagSymbol", TagSymbol);

			var provider = new SettingsProvider();
			provider.SetCollection(settings);
			provider.Save();
		}
	}
}
