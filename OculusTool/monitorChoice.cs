using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace OculusTool
{
    public partial class monitorChoice : Form
    {
        public monitorChoice()
        {
            InitializeComponent();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Program.monitor = 1;
            Program.dx11Force = checkBox1.Checked;
            this.Close();
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Program.monitor = 2;
            Program.dx11Force = checkBox1.Checked;
            this.Close();
        }

        private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Program.monitor = 3;
            Program.dx11Force = checkBox1.Checked;
            this.Close();
        }

        private void linkLabel4_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Program.monitor = 4;
            Program.dx11Force = checkBox1.Checked;
            this.Close();
        }

        private void linkLabel5_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Program.monitor = 5;
            Program.dx11Force = checkBox1.Checked;
            this.Close();
        }
    }
}
