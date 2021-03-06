using System;

namespace Eto.Forms
{
	public interface ICheckBox : ITextControl
	{
		bool? Checked { get; set; }

		bool ThreeState { get; set; }
	}

	public class CheckBox : TextControl
	{
		new ICheckBox Handler { get { return (ICheckBox)base.Handler; } }

		public event EventHandler<EventArgs> CheckedChanged
		{
			add { Properties.AddEvent(CheckedChangedKey, value); }
			remove { Properties.RemoveEvent(CheckedChangedKey, value); }
		}

		static readonly object CheckedChangedKey = new object();

		public virtual void OnCheckedChanged(EventArgs e)
		{
			Properties.TriggerEvent(CheckedChangedKey, this, e);
		}

		public CheckBox()
			: this((Generator)null)
		{
		}

		public CheckBox(Generator generator) : this(generator, typeof(ICheckBox))
		{
		}

		protected CheckBox(Generator generator, Type type, bool initialize = true)
			: base(generator, type, initialize)
		{
		}

		public virtual bool? Checked
		{
			get { return Handler.Checked; }
			set { Handler.Checked = value; }
		}

		public bool ThreeState
		{
			get { return Handler.ThreeState; }
			set { Handler.ThreeState = value; }
		}

		public ObjectBinding<CheckBox, bool?> CheckedBinding
		{
			get
			{
				return new ObjectBinding<CheckBox, bool?>(
					this, 
					c => c.Checked, 
					(c, v) => c.Checked = v, 
					(c, h) => c.CheckedChanged += h, 
					(c, h) => c.CheckedChanged -= h
				);
			}
		}
	}
}
