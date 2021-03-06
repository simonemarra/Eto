using System;

namespace Eto.Forms
{
	public interface ICommonDialog : IInstanceWidget
	{
		DialogResult ShowDialog (Window parent);
	}
	
	public abstract class CommonDialog : InstanceWidget
	{
		new ICommonDialog Handler { get { return (ICommonDialog)base.Handler; } }
		
		protected CommonDialog (Generator g, Type type, bool initialize = true)
			: base (g, type, initialize)
		{
		}

		public DialogResult ShowDialog (Control parent)
		{
			return ShowDialog (parent.ParentWindow);
		}
		
		public DialogResult ShowDialog (Window parent)
		{
			return Handler.ShowDialog (parent);
		}
		
	}
}

