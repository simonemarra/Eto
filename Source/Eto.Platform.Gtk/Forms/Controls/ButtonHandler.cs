using System;
using Eto.Forms;
using Eto.Drawing;

namespace Eto.Platform.GtkSharp
{
	/// <summary>
	/// Button handler.
	/// </summary>
	/// <copyright>(c) 2012-2013 by Curtis Wensley</copyright>
	/// <license type="BSD-3">See LICENSE for full terms</license>
	public class ButtonHandler : GtkControl<Gtk.Button, Button>, IButton
	{
		Image image;
		readonly Gtk.AccelLabel label;
		readonly Gtk.Image gtkimage;
		readonly Gtk.Table table;
		ButtonImagePosition imagePosition;

		protected override Gtk.Widget FontControl
		{
			get { return label; }
		}

		public ButtonHandler ()
		{
			Control = new Gtk.Button ();

			// need separate widgets as the theme can (and usually) disables images on buttons
			// gtk3 can override the theme per button, but gtk2 cannot
			table = new Gtk.Table (3, 3, false);
			table.ColumnSpacing = 0;
			table.RowSpacing = 0;
			label = new Gtk.AccelLabel (string.Empty);
			label.NoShowAll = true;
			table.Attach (label, 1, 2, 1, 2, Gtk.AttachOptions.Expand, Gtk.AttachOptions.Expand, 0, 0);
			gtkimage = new Gtk.Image ();
			gtkimage.NoShowAll = true;
			SetImagePosition (false);
			Control.Add (table);

			Control.Clicked += delegate {
				Widget.OnClick (EventArgs.Empty);
			};
			var defaultSize = Button.DefaultSize;
			Control.SizeAllocated += delegate(object o, Gtk.SizeAllocatedArgs args) {
				var size = args.Allocation;
				if (size.Width > 1 || size.Height > 1) {
					if (defaultSize.Width > size.Width)
						size.Width = defaultSize.Width;
					if (defaultSize.Height > size.Height)
						size.Height = defaultSize.Height;
					if (args.Allocation != size)
						Control.SetSizeRequest (size.Width, size.Height);
				}
			};
		}

		public override string Text
		{
			get { return MnuemonicToString (label.Text); }
			set
			{
				label.TextWithMnemonic = StringToMnuemonic (value);
				SetImagePosition ();
			}
		}

		public Image Image
		{
			get { return image; }
			set
			{
				image = value;
				image.SetGtkImage (gtkimage);
				if (value == null)
					gtkimage.Hide ();
				else
					gtkimage.Show ();
			}
		}

		void SetImagePosition (bool removeImage = true)
		{
			uint left, top;
			bool shouldHideLabel = false;

			switch (ImagePosition) {
			case ButtonImagePosition.Above:
				left = 1; top = 0;
				shouldHideLabel = true;
				break;
			case ButtonImagePosition.Below:
				left = 1; top = 2;
				shouldHideLabel = true;
				break;
			case ButtonImagePosition.Left:
				left = 0; top = 1;
				break;
			case ButtonImagePosition.Right:
				left = 2; top = 1;
				break;
			case ButtonImagePosition.Overlay:
				left = 1; top = 1;
				break;
			default:
				throw new NotSupportedException ();
			}
			shouldHideLabel &= string.IsNullOrEmpty (label.Text);
			if (shouldHideLabel)
				label.Hide ();
			else
				label.Show ();

			var right = left + 1;
			var bottom = top + 1;
			var options = shouldHideLabel ? Gtk.AttachOptions.Expand : Gtk.AttachOptions.Shrink;
			if (removeImage)
				table.Remove (gtkimage);
			table.Attach (gtkimage, left, right, top, bottom, options, options, 0, 0);

		}

		public ButtonImagePosition ImagePosition
		{
			get { return imagePosition; }
			set {
				if (imagePosition != value) {
					imagePosition = value;
					SetImagePosition ();
				}
			}
		}
	}
}
