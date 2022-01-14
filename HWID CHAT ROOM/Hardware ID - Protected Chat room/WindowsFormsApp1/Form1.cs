using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Media;

namespace WindowsFormsApp1
{


    public partial class Form1 : Form
    {
        private Timer tmr;
        private int scrll { get; set; }

        public Form1()
        {
            InitializeComponent();
            SoundPlayer My_JukeBox = new SoundPlayer(@"C:\WINDOWS\Media\Speech Off.wav");
            My_JukeBox.Play();
        }

        string HWID;
       


        private void Form1_Load(object sender, EventArgs e)
        {
            HWID = System.Security.Principal.WindowsIdentity.GetCurrent().User.Value;
            textBox1.Text = HWID;
            textBox1.ReadOnly = true;
            checkonline();
            tmr = new Timer();
            tmr.Tick += new EventHandler(this.timer1_Tick);
            tmr.Interval = 120;
            tmr.Start();
            label1.Text = "Confidential Zone     ";
            // ScrollLabel();
        }


        private void button1_Click_1(object sender, EventArgs e)
        {
            
            WebClient wb = new WebClient();
            string HWIDLIST = wb.DownloadString("https://raw.githubusercontent.com/anony123456/sdwqfqiqfa/master/hwid");
            if (HWIDLIST.Contains(textBox1.Text))
            {

                this.Hide();
                var Form2 = new Client();
               Form2.Closed += (s, args) => this.Close();
                Form2.Show();

            }
            else
            {
                String message = "Access Denied";
                MessageBox.Show(message);
                Application.Exit();
            }
        }

        private void checkonline()
        {
            
            try
            {
                using (var client = new WebClient())
                {
                    using (client.OpenRead("https://google.com/"))
                    {
                        label1.ForeColor = Color.Green;
                        label1.Text = ("Online");
                    }
                }
            }
            catch
            {
               
                label1.ForeColor = Color.Red;
                label1.Text = ("Offline");
                Application.Exit();
            }
        }


        private void button2_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(HWID);
            button2.Enabled = false;
            button2.Text = "Copied";
        }

        private void label1_Click(object sender, EventArgs e) 
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
        System.Diagnostics.Process.Start ("https://forms.gle/iq45MexjVLqUQE6P7");
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            label1.Text = label1.Text.Substring(1, label1.Text.Length - 1) + label1.Text.Substring(0, 1);

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }






}
