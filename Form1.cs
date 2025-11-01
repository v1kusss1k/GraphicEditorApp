using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic;
namespace GraphicEditor
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            SetSize();
        }
        //класс для хранения линий
        private class ArrayPoints
        {
            private int index = 0;
            private Point[] points;

            public ArrayPoints(int size)
            {
                if (size <= 0) { size = 2; }
                points = new Point[size];
            }

            public void SetPoint(int x, int y)
            {
                if (index >= points.Length)
                {
                    index = 0;
                }
                points[index] = new Point(x, y);
                index++;
            }

            public void ResetPoints()
            {
                index = 0;
            }

            public int GetCountPoints()
            {
                return index;
            }

            public Point[] GetPoints()
            {
                return points;
            }
        }

        private bool isMouse = false;
        private ArrayPoints arrayPoints = new ArrayPoints(2);

        Bitmap map = new Bitmap(100, 100);
        Graphics graphics;

        Pen pen = new Pen(Color.Black, 3f);

        private Stack<Bitmap> undoStack = new Stack<Bitmap>();
        private Stack<Bitmap> redoStack = new Stack<Bitmap>();

        private Font currentFont = new Font("Times new Roman", 14);

        private void SetSize()
        {
            Rectangle rectangle = Screen.PrimaryScreen.Bounds;
            map = new Bitmap(rectangle.Width, rectangle.Height);
            graphics = Graphics.FromImage(map);

            pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
            pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            isMouse = true;

            undoStack.Push((Bitmap)map.Clone());
            redoStack.Clear();
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            isMouse = false;
            arrayPoints.ResetPoints();
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isMouse) { return; }

            arrayPoints.SetPoint(e.X, e.Y);
            if (arrayPoints.GetCountPoints() >= 2)
            {
                graphics.DrawLines(pen, arrayPoints.GetPoints());
                pictureBox1.Image = map;
                arrayPoints.SetPoint(e.X, e.Y);
            }


        }
        //цвет кисти
        private void button3_Click(object sender, EventArgs e)
        {
            pen.Color = ((Button)sender).BackColor;
        }

        private void button14_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK) 
            {
                pen.Color = colorDialog1.Color;
                ((Button)sender).BackColor = colorDialog1.Color;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            graphics.Clear(pictureBox1.BackColor);
            pictureBox1.Image = map;
        }
        
        //сохранение
        private void button1_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter =  "JPG(*.JPG)|*.jpg";
            if (saveFileDialog1.ShowDialog() == DialogResult.OK) 
            {
                if (pictureBox1.Image != null)
                { 
                    pictureBox1.Image.Save(saveFileDialog1.FileName);
                }
            }
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            pen.Width = trackBar1.Value;
        }

        //кнопка повторить
        private void buttonRedo_Click(object sender, EventArgs e)
        {
            if (redoStack.Count > 0) 
            {
                undoStack.Push((Bitmap)map.Clone());
                map = redoStack.Pop();
                graphics = Graphics.FromImage(map);
                pictureBox1.Image= map;
                pictureBox1.Invalidate();
            }   

        }

        //кнопка отменить
        private void buttonUndo_Click(object sender, EventArgs e) 
        {
            if (undoStack.Count > 0)
            {
                redoStack.Push((Bitmap)map.Clone());
                map = undoStack.Pop();
                graphics = Graphics.FromImage(map);
                pictureBox1.Image = map;
                pictureBox1.Invalidate();
            }
        }

        private void buttonText_Click(object sender, EventArgs e)
        {
            string text = Interaction.InputBox("Ввидите текст:", "Вставка текста", "");
            if (!string.IsNullOrEmpty(text)) 
            {
                undoStack.Push((Bitmap)map.Clone());

                using (Graphics g = Graphics.FromImage(map))
                {
                    g.DrawString(text, currentFont, new SolidBrush(pen.Color), new PointF(50, 50));
                }

                pictureBox1.Image = map;
            }
        }

        private void buttonFont_Click(object sender, EventArgs e)
        {
            if (fontDialog1.ShowDialog() == DialogResult.OK)
            {
                currentFont = fontDialog1.Font;
            }
        }

        private void buttonImage_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Выберите изображение";
                openFileDialog.Filter = "Файлы изображений |*jpg;*jpeg;*png;*.bmp";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        Image img = Image.FromFile(openFileDialog.FileName);

                        undoStack.Push((Bitmap)map.Clone());

                        float x = (pictureBox1.Width - img.Width) / 2;
                        float y = (pictureBox1.Height - img.Height) / 2;

                        graphics.DrawImage(img, x, y);
                        pictureBox1.Image = map;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при открытии изображения:" + ex.Message);
                    }
                }
            }
        }
    }
}
