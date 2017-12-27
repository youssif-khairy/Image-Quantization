using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ImageQuantization
{
    public partial class Image_Quantize : Form
    {
        public Image_Quantize()
        {
            InitializeComponent();
        }

        RGBPixel[,] ImageMatrix;
        private void btnOpen_Click(object sender, EventArgs e)
        {

            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //Open the browsed image and display it
                string OpenedFilePath = openFileDialog1.FileName;
                ImageMatrix = ImageOperations.OpenImage(OpenedFilePath);
                ImageOperations.get_Distincit(ImageMatrix);
                ImageOperations.grph(int.Parse(txtGaussSigma.Text));
                ImageOperations.Clustring();
                ImageOperations.DisplayImage(ImageMatrix, pictureBox1);
                ImageMatrix = ImageOperations.Refill_Mtrx(ImageMatrix);
            }
            txtWidth.Text = ImageOperations.GetWidth(ImageMatrix).ToString();
            txtHeight.Text = ImageOperations.GetHeight(ImageMatrix).ToString();


        }

        private void button1_Click(object sender, EventArgs e)
        {
            ImageOperations.DisplayImage(ImageMatrix, pictureBox2);
        }

        

    }
}
