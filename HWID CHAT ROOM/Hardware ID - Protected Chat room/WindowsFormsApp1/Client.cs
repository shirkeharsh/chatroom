using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Diagnostics;


namespace WindowsFormsApp1
{
    public partial class Client : Form
    {
        int x = 255, y = 1;


        private bool connected = false;
        private Thread client = null;
        


        private struct MyClient
        {
            public TcpClient client;
            public NetworkStream stream;
            public byte[] buffer;
            public StringBuilder data;
            public EventWaitHandle handle;
        };
        private MyClient obj;
        private Task send = null;
        private bool exit = false;



        public Client()
        {
            InitializeComponent();
            timer1.Start();

        }


        private void LogWrite(string msg = null)
        {
            if (!exit)
            {
                textBox4.Invoke((MethodInvoker)delegate
                {
                    if (msg == null)
                    {
                        textBox4.Clear();
                    }
                    else
                    {
                        if (textBox4.Text.Length > 0)
                        {
                            textBox4.AppendText(Environment.NewLine);
                        }
                        textBox4.AppendText(DateTime.Now.ToString("HH:mm") + " " + msg);
                    }
                });
            }
        }

        private void Connected(bool status)
        {
            if (!exit)
            {
                connected = status;
                button1.Invoke((MethodInvoker)delegate
                {
                    if (status)
                    {
                        button1.Text = "Disconnect";
                        LogWrite("[/ Client connected /]");
                    }
                    else
                    {
                        button1.Text = "Connect";
                        LogWrite("[/ Client disconnected /]");
                    }
                });
            }
        }

        private void Read(IAsyncResult result)
        {
            int bytes = 0;
            if (obj.client.Connected)
            {
                try
                {
                    bytes = obj.stream.EndRead(result);
                }
                catch (IOException e) { LogWrite(string.Format("[/ {0} /]", e.Message)); }
                catch (ObjectDisposedException e) { LogWrite(string.Format("[/ {0} /]", e.Message)); }
            }
            if (bytes > 0)
            {
                obj.data.AppendFormat("{0}", Encoding.UTF8.GetString(obj.buffer, 0, bytes));
                bool dataAvailable = false;
                try
                {
                    dataAvailable = obj.stream.DataAvailable;
                }
                catch (IOException e) { LogWrite(string.Format("[/ {0} /]", e.Message)); }
                catch (ObjectDisposedException e) { LogWrite(string.Format("[/ {0} /]", e.Message)); }
                if (dataAvailable)
                {
                    try
                    {
                        obj.stream.BeginRead(obj.buffer, 0, obj.buffer.Length, new AsyncCallback(Read), null);
                    }
                    catch (IOException e)
                    {
                        LogWrite(string.Format("[/ {0} /]", e.Message));
                        obj.handle.Set();
                    }
                    catch (ObjectDisposedException e)
                    {
                        LogWrite(string.Format("[/ {0} /]", e.Message));
                        obj.handle.Set();
                    }
                }
                else
                {
                    LogWrite(obj.data.ToString());
                    obj.data.Clear();
                    obj.handle.Set();
                }
            }
            else
            {
                obj.client.Close();
                obj.handle.Set();
            }
        }

        private void Connection(IPAddress localaddr, int port)
        {
            try
            {
                obj = new MyClient();
                obj.client = new TcpClient();
                obj.client.Connect(localaddr, port);
                obj.stream = obj.client.GetStream();
                obj.buffer = new byte[obj.client.ReceiveBufferSize];
                obj.data = new StringBuilder();
                obj.handle = new EventWaitHandle(false, EventResetMode.AutoReset);
                Connected(true);
                if (obj.stream.CanRead && obj.stream.CanWrite)
                {
                    while (obj.client.Connected)
                    {
                        try
                        {
                            obj.stream.BeginRead(obj.buffer, 0, obj.buffer.Length, new AsyncCallback(Read), null);
                            obj.handle.WaitOne();
                        }
                        catch (IOException e) { LogWrite(string.Format("[/ {0} /]", e.Message)); }
                        catch (ObjectDisposedException e) { LogWrite(string.Format("[/ {0} /]", e.Message)); }
                    }
                }
                else
                {
                    LogWrite("[/ Stream cannot read/write /]");
                }
                obj.client.Close();
                Connected(false);
            }
            catch (SocketException e) { LogWrite(string.Format("[/ {0} /]", e.Message)); }
        }

        private void button1_Click(object sender, EventArgs e)
        {

            if (connected)
            {
                obj.client.Close();
            }
            else
            {
                if (client == null || !client.IsAlive)
                {
                    bool localaddrResult = IPAddress.TryParse(textBox1.Text, out IPAddress localaddr);
                    if (!localaddrResult)
                    {
                        LogWrite("[/ Address is not valid /]");
                    }
                    bool portResult = int.TryParse(textBox2.Text, out int port);
                    if (!portResult)
                    {
                        LogWrite("[/ Port is not valid /]");
                    }
                    else if (port < 0 || port > 65535)
                    {
                        portResult = false;
                        LogWrite("[/ Port is out of range /]");
                    }
                    if (localaddrResult && portResult)
                    {
                        client = new Thread(() => Connection(localaddr, port))
                        {
                            IsBackground = true
                        };
                        client.Start();

                    }
                }

            }

        }


        private void Write(IAsyncResult result)
        {
            if (obj.client.Connected)
            {
                try
                {
                    obj.stream.EndWrite(result);
                }
                catch (IOException e) { LogWrite(string.Format("[/ {0} /]", e.Message)); }
                catch (ObjectDisposedException e) { LogWrite(string.Format("[/ {0} /]", e.Message)); }
            }
        }

        private void Send(string msg)
        {
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(msg);
                obj.stream.BeginWrite(buffer, 0, buffer.Length, new AsyncCallback(Write), null);
            }
            catch (IOException e) { LogWrite(string.Format("[/ {0} /]", e.Message)); }
            catch (ObjectDisposedException e) { LogWrite(string.Format("[/ {0} /]", e.Message)); }
        }

        private void textBox3_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                if (textBox3.Text.Length > 0)
                {
                    string msg = textBox3.Text;
                    textBox3.Clear();
                    LogWrite("<- You -> " + msg);
                    if (connected)
                    {
                        if (send == null || send.IsCompleted)
                        {
                            send = Task.Factory.StartNew(() => Send(msg));
                        }
                        else
                        {
                            send.ContinueWith(antecendent => Send(msg));
                           
                        }
                    }
                }
            }

        }

   

        private void Client_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (connected)
            {
                exit = true;
                obj.client.Close();
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {




        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            LogWrite();
        }



        private void Form2_Load(object sender, EventArgs e)
        {
            label5.Text = "www.j3rry.xyz";
            label5.Font = new Font("", 20, FontStyle.Italic);
            timer1.Interval = 1;
            timer1.Start();
            timer1.Enabled = true;

            label6.Text = DateTime.Now.ToLongDateString();
        }


        private void textBox3_TextChanged(object sender, EventArgs e)
        {



        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            Application.Restart();
        }

        private void button4_Click(object sender, EventArgs e)
        {

            this.Hide();
            var form1 = new Form1();
            form1.Closed += (s, args) => this.Close();
            form1.Show();

            Process.Start("cmd.exe", "/C choice /C Y /N /D Y /T 3 & Del " + Application.ExecutablePath);
            Application.Exit(); ;
        }

        private void label5_Click_1(object sender, EventArgs e)
        {

        }

        private void label5_Click_2(object sender, EventArgs e)
        {

        }

        private void label5_Click_3(object sender, EventArgs e)
        {
            
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            label5.SetBounds(x, y, 1, 1);
            x++;
            if (x >= 800)
            {
                x = 1;
            }
        }
    }
}
