using System;
using SD = System.Drawing;
using SWF = System.Windows.Forms;
using Eto.Forms;
using System.Runtime.InteropServices;

namespace Eto.Platform.Windows
{
	public class TextBoxHandler : WindowsControl<TextBoxHandler.WatermarkTextBox, TextBox>, ITextBox
	{
		public class WatermarkTextBox : SWF.TextBox
		{
			const uint ECM_FIRST = 0x1500;
			const uint EM_SETCUEBANNER = ECM_FIRST + 1;

			[DllImport ("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
			static extern IntPtr SendMessage (IntPtr hWnd, uint msg, uint wParam, [MarshalAs (UnmanagedType.LPWStr)] string lParam);

			string watermarkText;
			public string WatermarkText
			{
				get { return watermarkText; }
				set
				{
					watermarkText = value;
					SetWatermark (watermarkText);
				}
			}

			void SetWatermark (string watermarkText)
			{
				SendMessage(Handle, EM_SETCUEBANNER, 0, watermarkText);
			}

		}
		public TextBoxHandler ()
		{
			Control = new WatermarkTextBox ();
		}

		public bool ReadOnly {
			get { return Control.ReadOnly; }
			set { Control.ReadOnly = value; }
		}
		
		public int MaxLength {
			get { return Control.MaxLength; }
			set { Control.MaxLength = value; }
		}

		public string PlaceholderText {
			get { return Control.WatermarkText;  }
			set { Control.WatermarkText = value; }
		}

		public void SelectAll ()
		{
			Control.Focus ();
			Control.SelectAll ();
		}
	}
}
