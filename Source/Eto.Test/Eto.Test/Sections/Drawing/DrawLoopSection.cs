using System;
using System.Collections.Generic;
using Eto.Drawing;
using Eto.Forms;
using System.Threading;
using System.Diagnostics;

namespace Eto.Test.Sections.Drawing
{
	public class DrawLoopSection : Scrollable
	{
		readonly Drawable drawable;
		readonly DirectDrawingRenderer renderer;
		readonly Panel content;
		bool useCreateGraphics;
		Thread thread;
		Action drawFrame;
		Status status = new Status();

		class Status
		{
			public bool Stop { get; set; }
		}

		public DrawLoopSection()
			: this(null)
		{
		}

		public DrawLoopSection(Generator generator)
			: base(generator)
		{
			drawable = new Drawable(Generator)
			{
				Style = "direct",
				BackgroundColor = Colors.Black
			};
			drawable.Paint += (sender, e) => renderer.DrawFrame(e.Graphics, drawable.Size);
			renderer = new DirectDrawingRenderer(Generator);

			var layout = new DynamicLayout(new Padding(10));
			layout.AddSeparateRow(null, UseTexturesAndGradients(), UseCreateGraphics(), null);
			layout.Add(content = new Panel { Content = drawable });
			this.Content = layout;
		}

		void DrawLoop(object data)
		{
			var currentStatus = (Status)data;
			renderer.RestartFPS();
			while (!currentStatus.Stop)
			{
				var draw = drawFrame;
				if (draw != null)
					Application.Instance.Invoke(draw);
				Thread.Sleep(0);
			}
		}

		void SetMode()
		{
			drawFrame = null;
			status.Stop = true;
			if (useCreateGraphics)
			{
				renderer.EraseBoxes = true;
				if (!drawable.SupportsCreateGraphics)
				{
					content.BackgroundColor = Colors.Red;
					content.Content = new Label { Text = "This platform does not support Drawable.CreateGraphics", TextColor = Colors.White, VerticalAlign = VerticalAlign.Middle, HorizontalAlign = HorizontalAlign.Center };
					return;
				}
				drawFrame = DrawWithCreateGraphics;
				using (var graphics = drawable.CreateGraphics())
					graphics.Clear();
			}
			else
			{
				renderer.EraseBoxes = false;
				drawFrame = () => { if (!status.Stop) drawable.Invalidate(); };
			}
			content.Content = drawable;
			status = new Status();
			thread = new Thread(DrawLoop);
			thread.Start(status);
		}

		public override void OnUnLoad(EventArgs e)
		{
			status.Stop = true;
			base.OnUnLoad(e);
		}

		void DrawWithCreateGraphics()
		{
			if (status.Stop)
				return;
			using (var graphics = drawable.CreateGraphics())
			{
				renderer.DrawFrame(graphics, drawable.Size);
			}
		}

		Control UseTexturesAndGradients ()
		{
			var control = new CheckBox {
				Text = "Use Textures && Gradients",
				Checked = renderer.UseTexturesAndGradients
			};
			control.CheckedChanged += (sender, e) => {
				renderer.UseTexturesAndGradients = control.Checked ?? false;
				lock (renderer.Boxes)
				{
					renderer.Boxes.Clear();
					renderer.RestartFPS();
				}
				if (useCreateGraphics && drawable.SupportsCreateGraphics)
					using (var graphics = drawable.CreateGraphics())
						graphics.Clear();
			};
			return control;
		}

		Control UseCreateGraphics()
		{
			var control = new CheckBox
			{
				Text = "Use CreateGraphics",
				Checked = useCreateGraphics
			};
			control.CheckedChanged += (sender, e) =>
			{
				useCreateGraphics = control.Checked ?? false;
				renderer.RestartFPS();
				Application.Instance.AsyncInvoke(SetMode);
			};
			return control;
		}

		public override void OnLoadComplete (EventArgs e)
		{
			base.OnLoadComplete (e);
			SetMode();
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
				status.Stop = true;
			base.Dispose(disposing);
		}
	}

	public class DirectDrawingRenderer
	{
		readonly Image texture;
		readonly Font font;
		readonly SolidBrush textBrush;

		public readonly Stopwatch Watch = new Stopwatch();
		public int TotalFrames { get; set; }
		public readonly List<Box> Boxes = new List<Box>();
		public bool UseTexturesAndGradients { get; set; }
		public Generator Generator { get; private set; }
		public bool EraseBoxes { get; set; }

		public DirectDrawingRenderer(Generator generator)
		{
			Generator = generator;
			texture = TestIcons.Textures(generator);
			font = SystemFonts.Default(generator: generator);
			textBrush = new SolidBrush(Colors.White, generator);
		}

		public void RestartFPS()
		{
			Watch.Restart();
			TotalFrames = 0;
		}

		public class Box
		{
			static readonly Random random = new Random();
			SizeF increment;
			readonly Color color;
			readonly float rotation;
			float angle;
			readonly Action<Graphics> draw;
			readonly Action<Graphics> erase;
			readonly Brush fillBrush;
			RectangleF position;
			IMatrix transform;
			DirectDrawingRenderer renderer;

			public SizeF Increment { get { return increment; } set { increment = value; } }

			static Color GetRandomColor(Random random)
			{
				return Color.FromArgb(random.Next(byte.MaxValue), random.Next(byte.MaxValue), random.Next(byte.MaxValue));
			}

			public Box(Size canvasSize, bool useTexturesAndGradients, DirectDrawingRenderer renderer)
			{
				this.renderer = renderer;
				var size = new SizeF(random.Next(50) + 50, random.Next(50) + 50);
				var location = new PointF(random.Next(canvasSize.Width - (int)size.Width), random.Next(canvasSize.Height - (int)size.Height));
				position = new RectangleF(location, size);
				increment = new SizeF(random.Next(3) + 1, random.Next(3) + 1);
				if (random.Next(2) == 1)
					increment.Width = -increment.Width;
				if (random.Next(2) == 1)
					increment.Height = -increment.Height;

				angle = random.Next(360);
				rotation = (random.Next(20) - 10f) / 4f;

				var rect = new RectangleF(size);
				color = GetRandomColor(random);
				var colorPen = new Pen(color, generator: renderer.Generator);
				var blackPen = Pens.Black(renderer.Generator);
				var blackBrush = Brushes.Black(renderer.Generator);
				switch (random.Next(useTexturesAndGradients ? 4 : 2))
				{
					case 0:
						draw = g => g.DrawRectangle(colorPen, rect);
						erase = g => g.DrawRectangle(blackPen, rect);
						break;
					case 1:
						draw = g => g.DrawEllipse(colorPen, rect);
						erase = g => g.DrawEllipse(blackPen, rect);
						break;
					case 2:
						switch (random.Next(2))
						{
							case 0:
								fillBrush = new LinearGradientBrush(GetRandomColor(random), GetRandomColor(random), PointF.Empty, new PointF(size.Width, size.Height), renderer.Generator);
								break;
							case 1:
								fillBrush = new TextureBrush(renderer.texture, 1f, renderer.Generator)
								{
									Transform = Matrix.FromScale(size / 80, renderer.Generator)
								};
								break;
						}
						draw = g => g.FillEllipse(fillBrush, rect);
						erase = g => g.FillEllipse(blackBrush, rect);
						break;
					case 3:
						switch (random.Next(2))
						{
							case 0:
								fillBrush = new LinearGradientBrush(GetRandomColor(random), GetRandomColor(random), PointF.Empty, new PointF(size.Width, size.Height), renderer.Generator);
								break;
							case 1:
								fillBrush = new TextureBrush(renderer.texture, 1f, renderer.Generator)
								{
									Transform = Matrix.FromScale(size / 80, renderer.Generator)
								};
								break;
						}
						draw = g => g.FillRectangle(fillBrush, rect);
						erase = g => g.FillRectangle(blackBrush, rect);
						break;
				}
			}

			public void Move(Size bounds)
			{
				position.Offset(increment);
				var center = position.Center;
				if (increment.Width > 0 && center.X >= bounds.Width)
					increment.Width = -increment.Width;
				else if (increment.Width < 0 && center.X < 0)
					increment.Width = -increment.Width;

				if (increment.Height > 0 && center.Y >= bounds.Height)
					increment.Height = -increment.Height;
				else if (increment.Height < 0 && center.Y < 0)
					increment.Height = -increment.Height;
				angle += rotation;

				transform = Matrix.FromTranslation(position.Location, renderer.Generator);
				transform.RotateAt(angle, position.Width / 2, position.Height / 2);
			}

			public void Erase(Graphics graphics)
			{
				if (transform != null)
				{
					graphics.SaveTransform();
					graphics.MultiplyTransform(transform);
					erase(graphics);
					graphics.RestoreTransform();
				}
			}

			public void Draw(Graphics graphics)
			{
				graphics.SaveTransform();
				graphics.MultiplyTransform(transform);
				draw(graphics);
				graphics.RestoreTransform();
			}
		}

		void InitializeBoxes(Size canvasSize)
		{
			for (int i = 0; i < 20; i++)
				Boxes.Add(new Box(canvasSize, UseTexturesAndGradients, this));
		}

		public void DrawFrame(Graphics graphics, Size canvasSize)
		{
			if (graphics == null)
				return;
			lock (Boxes)
			{
				if (Boxes.Count == 0 && canvasSize.Width > 1 && canvasSize.Height > 1)
					InitializeBoxes(canvasSize);

				var fps = TotalFrames / Watch.Elapsed.TotalSeconds;
				var fpsText = string.Format("Frames per second: {0:0.00}", fps);
				if (EraseBoxes)
					graphics.FillRectangle(Colors.Black, new RectangleF(graphics.MeasureString(font, fpsText)));
				graphics.DrawText(font, textBrush, 0, 0, fpsText);

				var bounds = canvasSize;
				graphics.AntiAlias = false;
				foreach (var box in Boxes)
				{
					if (EraseBoxes)
						box.Erase(graphics);
					box.Move(bounds);
					box.Draw(graphics);
				}
				TotalFrames++;
			}
		}
	}
}

