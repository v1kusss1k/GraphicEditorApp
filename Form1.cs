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
        // паттерн "команда", базовый интерфейс для всех операций
        public interface ICommand
        {
            void Execute();
            void Undo();
        }

        // интерфейс для инструментов рисования
        public interface IGraphicsTool
        {
            Color Color { get; set; }
            float Width { get; set; }
            IGraphicsTool Clone();
            void DrawLine(Graphics graphics, Point start, Point end);
        }

        // интерфейс для контейнера изображений
        public interface IImageContainer
        {
            Image Image { get; set; }
            int Width { get; }
            int Height { get; }
        }

        // реализация IGraphicsTool на основе стандартного Pen
        public class PenGraphicsTool : IGraphicsTool
        {
            private Pen _pen;

            public PenGraphicsTool(Pen pen)
            {
                _pen = (Pen)pen.Clone();
            }

            public Color Color
            {
                get => _pen.Color;
                set => _pen.Color = value;
            }

            public float Width
            {
                get => _pen.Width;
                set => _pen.Width = value;
            }

            public IGraphicsTool Clone()
            {
                return new PenGraphicsTool((Pen)_pen.Clone());
            }

            public void DrawLine(Graphics graphics, Point start, Point end)
            {
                graphics.DrawLine(_pen, start, end);
            }
        }


        // реализация IImageContainer на основе PictureBox
        public class PictureBoxImageContainer : IImageContainer
        {
            private PictureBox _pictureBox;

            public PictureBoxImageContainer(PictureBox pictureBox)
            {
                _pictureBox = pictureBox;
            }

            public Image Image
            {
                get => _pictureBox.Image;
                set => _pictureBox.Image = value;
            }

            public int Width => _pictureBox.Width;
            public int Height => _pictureBox.Height;
        }


        // команда очистки холста 
        public class ClearCommand : ICommand
        {
            private Bitmap _backupBefore;
            private IImageContainer _imageContainer;

            public ClearCommand(Bitmap currentMap, IImageContainer imageContainer)
            {
                _backupBefore = (Bitmap)currentMap.Clone();
                _imageContainer = imageContainer;
            }

            public void Execute()
            {
                Bitmap newBitmap = new Bitmap(_backupBefore.Width, _backupBefore.Height);
                using (Graphics g = Graphics.FromImage(newBitmap))
                {
                    g.Clear(Color.White);
                }
                _imageContainer.Image = newBitmap;
            }

            public void Undo()
            {
                _imageContainer.Image = _backupBefore;
            }
        }

        // команда вставки текста 
        public class TextCommand : ICommand
        {
            private Bitmap _backupBefore;
            private string _text;
            private Font _font;
            private Color _color;
            private IImageContainer _imageContainer;

            public TextCommand(Bitmap currentMap, string text, Font font, Color color, IImageContainer imageContainer)
            {
                _backupBefore = (Bitmap)currentMap.Clone();
                _text = text;
                _font = font;
                _color = color;
                _imageContainer = imageContainer;
            }

            public void Execute()
            {
                if (string.IsNullOrEmpty(_text)) return;
                Bitmap temp = (Bitmap)_backupBefore.Clone();
                using (Graphics g = Graphics.FromImage(temp))
                {
                    g.DrawString(_text, _font, new SolidBrush(_color), new PointF(50, 50));
                }
                _imageContainer.Image = temp;
            }

            public void Undo()
            {
                _imageContainer.Image = _backupBefore;
            }
        }

        // команда вставки изображения 
        public class ImageCommand : ICommand
        {
            private Bitmap _backupBefore;
            private Image _image;
            private float _x, _y;
            private IImageContainer _imageContainer;

            public ImageCommand(Bitmap currentMap, Image image, float x, float y, IImageContainer imageContainer)
            {
                _backupBefore = (Bitmap)currentMap.Clone();
                _image = image;
                _x = x;
                _y = y;
                _imageContainer = imageContainer;
            }

            public void Execute()
            {
                Bitmap temp = (Bitmap)_backupBefore.Clone();
                using (Graphics g = Graphics.FromImage(temp))
                {
                    g.DrawImage(_image, _x, _y);
                }
                _imageContainer.Image = temp;
            }

            public void Undo()
            {
                _imageContainer.Image = _backupBefore;
            }
        }

        public class DrawBrushCommand : ICommand
        {
            private Bitmap _backupBefore;
            private List<Point> _points;
            private IGraphicsTool _graphicsTool;
            private IImageContainer _imageContainer;

            // команда рисования кистью
            public DrawBrushCommand(Bitmap backupBefore, List<Point> points, IGraphicsTool graphicsTool, IImageContainer imageContainer)
            {
                _backupBefore = backupBefore;
                _points = new List<Point>(points);
                _graphicsTool = graphicsTool.Clone();
                _imageContainer = imageContainer;
            }

            public void Execute()
            {
                Bitmap temp = (Bitmap)_backupBefore.Clone();
                using (Graphics g = Graphics.FromImage(temp))
                {
                    for (int i = 1; i < _points.Count; i++)
                    {
                        _graphicsTool.DrawLine(g, _points[i - 1], _points[i]);
                    }
                }
                _imageContainer.Image = temp;
            }

            public void Undo()
            {
                _imageContainer.Image = _backupBefore;
            }
        }

        // поля формы основные переменные
        private Bitmap map;
        private Graphics graphics;
        private IGraphicsTool graphicsTool; 
        private Font currentFont = new Font("Times New Roman", 14);

        // стеки для реализации Undo/Redo 
        private Stack<ICommand> undoStack = new Stack<ICommand>();
        private Stack<ICommand> redoStack = new Stack<ICommand>();

        // переменные для отслеживания состояния рисования
        private bool isMouse = false;
        private Point lastPoint;
        private Bitmap backupBeforeDrawing;
        private List<Point> currentPoints;

        public Form1()
        {
            InitializeComponent();

            graphicsTool = new PenGraphicsTool(new Pen(Color.Black, 3f));

            SetSize();

            // кнопки выбора цвета
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

        // инициализация размера холста
        private void SetSize()
        {
            Rectangle rectangle = Screen.PrimaryScreen.Bounds;
            map = new Bitmap(rectangle.Width, rectangle.Height);
            graphics = Graphics.FromImage(map);
            graphics.Clear(Color.White);

            // настройка инструмента через интерфейс 
            graphicsTool.Width = 3f;

            pictureBox1.Image = map;
        }

        // нажатие мыши, начало рисования
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            isMouse = true;
            lastPoint = e.Location;
            backupBeforeDrawing = (Bitmap)pictureBox1.Image.Clone();
            currentPoints = new List<Point>();
            currentPoints.Add(e.Location);
        }

        // движения мыши 
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isMouse) return;

            // сохранение точки
            currentPoints.Add(e.Location);

            // рисование на временной копии
            Bitmap temp = (Bitmap)backupBeforeDrawing.Clone();
            using (Graphics g = Graphics.FromImage(temp))
            {
                // улучшение сглаживания
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // рисование всей линии по всем сохраненным точкам
                for (int i = 1; i < currentPoints.Count; i++)
                {
                    graphicsTool.DrawLine(g, currentPoints[i - 1], currentPoints[i]);
                }

                // дополнительно рисуем последний отрезок для плавности
                if (currentPoints.Count > 0)
                {
                    graphicsTool.DrawLine(g, lastPoint, e.Location);
                }
            }
            pictureBox1.Image = temp;
            lastPoint = e.Location;
        }

        // отпускание мыши - завершение рисования и сохранение команды
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (!isMouse) return;
            isMouse = false;

            // добавление последней точки
            currentPoints.Add(e.Location);

            // обновление основного изображение
            map = (Bitmap)pictureBox1.Image.Clone();

            // реализация интерфейсов и сохранеие команды
            IImageContainer imageContainer = new PictureBoxImageContainer(pictureBox1);
            ICommand cmd = new DrawBrushCommand(backupBeforeDrawing, currentPoints, graphicsTool, imageContainer);
            undoStack.Push(cmd);
            redoStack.Clear();

            currentPoints = null;
        }

        // выбора цвета из палитры
        private void colorButton_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            graphicsTool.Color = btn.BackColor;
        }

        // выбора цвета через диалог
        private void button14_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                graphicsTool.Color = colorDialog1.Color;
                button14.BackColor = colorDialog1.Color;
            }
        }

        // изменения толщины кисти
        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            graphicsTool.Width = trackBar1.Value;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            IImageContainer imageContainer = new PictureBoxImageContainer(pictureBox1);
            ICommand cmd = new ClearCommand((Bitmap)pictureBox1.Image, imageContainer);
            cmd.Execute();
            undoStack.Push(cmd);
            redoStack.Clear();
            map = (Bitmap)pictureBox1.Image.Clone();
        }

        // кнопка отмены действия (Undo)
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

        // кнопка повтора действия (Redo)
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

        // кнопка вставки текста
        private void buttonText_Click(object sender, EventArgs e)
        {
            string text = Interaction.InputBox("Введите текст:", "Вставка текста", "");
            if (!string.IsNullOrEmpty(text))
            {
                IImageContainer imageContainer = new PictureBoxImageContainer(pictureBox1);
                ICommand cmd = new TextCommand((Bitmap)pictureBox1.Image, text, currentFont, graphicsTool.Color, imageContainer);
                cmd.Execute();
                undoStack.Push(cmd);
                redoStack.Clear();
                map = (Bitmap)pictureBox1.Image.Clone();
            }
        }

        // кнопка вставки изображения
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
                        IImageContainer imageContainer = new PictureBoxImageContainer(pictureBox1);
                        ICommand cmd = new ImageCommand((Bitmap)pictureBox1.Image, img, x, y, imageContainer);
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

        // кнопка сохранения изображения
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