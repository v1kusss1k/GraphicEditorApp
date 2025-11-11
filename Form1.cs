using Microsoft.VisualBasic;
using Microsoft.VisualBasic.Devices;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GraphicEditor
{
    public partial class Form1 : Form
    {
        public interface ICommand
        {
            void Execute();
            void Undo();
        }

        public class ClearCommand : ICommand
        {
            private Bitmap _backupBefore;
            private PictureBox _pictureBox;

            public ClearCommand(Bitmap currentMap, PictureBox pictureBox)
            {
                _backupBefore = (Bitmap)currentMap.Clone();
                _pictureBox = pictureBox;
            }

            public void Execute()
            {
                Bitmap newBitmap = new Bitmap(_backupBefore.Width, _backupBefore.Height);
                using (Graphics g = Graphics.FromImage(newBitmap))
                {
                    g.Clear(Color.White);
                }
                _pictureBox.Image = newBitmap;
            }

            public void Undo()
            {
                _pictureBox.Image = _backupBefore;
            }
        }

        public class TextCommand : ICommand
        {
            private Bitmap _backupBefore;
            private string _text;
            private Font _font;
            private Color _color;
            private PictureBox _pictureBox;

            public TextCommand(Bitmap currentMap, string text, Font font, Color color, PictureBox pictureBox)
            {
                _backupBefore = (Bitmap)currentMap.Clone();
                _text = text;
                _font = font;
                _color = color;
                _pictureBox = pictureBox;
            }

            public void Execute()
            {
                if (string.IsNullOrEmpty(_text)) return;
                Bitmap temp = (Bitmap)_backupBefore.Clone();
                using (Graphics g = Graphics.FromImage(temp))
                {
                    g.DrawString(_text, _font, new SolidBrush(_color), new PointF(50, 50));
                }
                _pictureBox.Image = temp;
            }

            public void Undo()
            {
                _pictureBox.Image = _backupBefore;
            }
        }

        public class ImageCommand : ICommand
        {
            private Bitmap _backupBefore;
            private Image _image;
            private float _x, _y;
            private PictureBox _pictureBox;

            public ImageCommand(Bitmap currentMap, Image image, float x, float y, PictureBox pictureBox)
            {
                _backupBefore = (Bitmap)currentMap.Clone();
                _image = image;
                _x = x;
                _y = y;
                _pictureBox = pictureBox;
            }

            public void Execute()
            {
                Bitmap temp = (Bitmap)_backupBefore.Clone();
                using (Graphics g = Graphics.FromImage(temp))
                {
                    g.DrawImage(_image, _x, _y);
                }
                _pictureBox.Image = temp;
            }

            public void Undo()
            {
                _pictureBox.Image = _backupBefore;
            }
        }

        public class DrawBrushCommand : ICommand
        {
            private Bitmap _backupBefore;
            private List<Point> _points;
            private Pen _pen;
            private PictureBox _pictureBox;

            public DrawBrushCommand(Bitmap backupBefore, List<Point> points, Pen pen, PictureBox pictureBox)
            {
                _backupBefore = backupBefore;
                _points = new List<Point>(points);
                _pen = (Pen)pen.Clone();
                _pictureBox = pictureBox;
            }

            public void Execute()
            {
                Bitmap temp = (Bitmap)_backupBefore.Clone();
                using (Graphics g = Graphics.FromImage(temp))
                {
                    // Рисуем всю линию по всем точкам
                    for (int i = 1; i < _points.Count; i++)
                    {
                        g.DrawLine(_pen, _points[i - 1], _points[i]);
                    }
                }
                _pictureBox.Image = temp;
            }

            public void Undo()
            {
                _pictureBox.Image = _backupBefore;
            }
        }

        //поля формы
        private Bitmap map;
        private Graphics graphics;
        private Pen pen = new Pen(Color.Black, 3f);
        private Font currentFont = new Font("Times New Roman", 14);

        private Stack<ICommand> undoStack = new Stack<ICommand>();
        private Stack<ICommand> redoStack = new Stack<ICommand>();

        private bool isMouse = false;
        private Point lastPoint;
        private Bitmap backupBeforeDrawing;
        private List<Point> currentPoints;

        public Form1()
        {
            InitializeComponent();
            SetSize();

            Button[] colorButtons = { button3, button4, button5, button6, button7,
                                      button8, button9, button10, button11, button12, button13 };
            Color[] colors = { Color.White, Color.Red, Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0))))), Color.Yellow, Color.Lime,
                               Color.Cyan, Color.Blue, Color.Purple, Color.Black, Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(64)))), ((int)(((byte)(0))))), Color.Gray };

            for (int i = 0; i < colorButtons.Length; i++)
            {
                colorButtons[i].BackColor = colors[i];
                colorButtons[i].Click += colorButton_Click;
            }
        }

        private void SetSize()
        {
            Rectangle rectangle = Screen.PrimaryScreen.Bounds;
            map = new Bitmap(rectangle.Width, rectangle.Height);
            graphics = Graphics.FromImage(map);
            graphics.Clear(Color.White);

            pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
            pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

            pictureBox1.Image = map;
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            isMouse = true;
            lastPoint = e.Location;
            backupBeforeDrawing = (Bitmap)pictureBox1.Image.Clone();
            currentPoints = new List<Point>();
            currentPoints.Add(e.Location);
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isMouse) return;

            // Сохраняем точку
            currentPoints.Add(e.Location);

            // Рисуем предпросмотр на временной копии
            Bitmap temp = (Bitmap)backupBeforeDrawing.Clone();
            using (Graphics g = Graphics.FromImage(temp))
            {
                // Рисуем всю линию по всем сохраненным точкам
                for (int i = 1; i < currentPoints.Count; i++)
                {
                    g.DrawLine(pen, currentPoints[i - 1], currentPoints[i]);
                }
            }
            pictureBox1.Image = temp;
            lastPoint = e.Location;
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (!isMouse) return;
            isMouse = false;

            // Добавляем последнюю точку
            currentPoints.Add(e.Location);

            // Обновляем основное изображение
            map = (Bitmap)pictureBox1.Image.Clone();

            // Сохраняем команду
            ICommand cmd = new DrawBrushCommand(backupBeforeDrawing, currentPoints, pen, pictureBox1);
            undoStack.Push(cmd);
            redoStack.Clear();

            currentPoints = null;
        }

        private void colorButton_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            pen.Color = btn.BackColor;
        }

        private void button14_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                pen.Color = colorDialog1.Color;
                button14.BackColor = colorDialog1.Color;
            }
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            pen.Width = trackBar1.Value;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ICommand cmd = new ClearCommand((Bitmap)pictureBox1.Image, pictureBox1);
            cmd.Execute();
            undoStack.Push(cmd);
            redoStack.Clear();
            map = (Bitmap)pictureBox1.Image.Clone();
        }

        private void buttonRedo_Click(object sender, EventArgs e)
        {
            if (redoStack.Count > 0)
            {
                ICommand cmd = redoStack.Pop();
                cmd.Execute();
                undoStack.Push(cmd);
                map = (Bitmap)pictureBox1.Image.Clone();
            }
        }

        private void buttonUndo_Click(object sender, EventArgs e)
        {
            if (undoStack.Count > 0)
            {
                ICommand cmd = undoStack.Pop();
                cmd.Undo();
                redoStack.Push(cmd);
                map = (Bitmap)pictureBox1.Image.Clone();
            }
        }

        private void buttonText_Click(object sender, EventArgs e)
        {
            string text = Interaction.InputBox("Введите текст:", "Вставка текста", "");
            if (!string.IsNullOrEmpty(text))
            {
                ICommand cmd = new TextCommand((Bitmap)pictureBox1.Image, text, currentFont, pen.Color, pictureBox1);
                cmd.Execute();
                undoStack.Push(cmd);
                redoStack.Clear();
                map = (Bitmap)pictureBox1.Image.Clone();
            }
        }

        private void buttonImage_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Файлы изображений|*.jpg;*.jpeg;*.png;*.bmp";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        Image img = Image.FromFile(openFileDialog.FileName);
                        float x = (pictureBox1.Width - img.Width) / 2;
                        float y = (pictureBox1.Height - img.Height) / 2;
                        ICommand cmd = new ImageCommand((Bitmap)pictureBox1.Image, img, x, y, pictureBox1);
                        cmd.Execute();
                        undoStack.Push(cmd);
                        redoStack.Clear();
                        map = (Bitmap)pictureBox1.Image.Clone();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка: " + ex.Message);
                    }
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "JPG(*.JPG)|*.jpg";
            if (saveFileDialog1.ShowDialog() == DialogResult.OK && pictureBox1.Image != null)
            {
                pictureBox1.Image.Save(saveFileDialog1.FileName);
            }
        }
    }
}