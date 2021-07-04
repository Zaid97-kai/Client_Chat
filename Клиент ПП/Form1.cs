using MyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Клиент_ПП
{
    public partial class Form1 : Form
    {
        MyLib.Message m;
        ComplexMessage cm = new ComplexMessage();
        byte[] data1;

        string message = "";
        private const string host = "127.0.0.1";
        private const int port = 8888;
        private string clientName = "";
        static TcpClient client;
        static NetworkStream stream;
        bool flag = true;
        bool namewassend = false; 
        public Form1()

        {
            InitializeComponent();
            client = new TcpClient();
            client.Connect(host, port); //подключение клиента
            richTextBoxChat.Text += "Введите свое имя: " + '\n';
            clients.client.Add("Никита", "123");
            clients.client.Add("НеНикита", "213");
            clients.client.Add("еще раз", "еще раз");
        }
        private void buttonSend_Click(object sender, EventArgs e)
        {
            if (!namewassend)
            {
                foreach (KeyValuePair<string, string> kli in clients.client)
                {
                    if(kli.Key == richTextBoxMessage.Text)
                    {
                        this.clientName = richTextBoxMessage.Text;
                        namewassend = true;
                        richTextBoxChat.Invoke(new Action(() => richTextBoxChat.Text += richTextBoxMessage.Text + '\n'));
                        richTextBoxChat.Invoke(new Action(() => richTextBoxChat.Text += "Введите пароль: " + '\n'));
                        richTextBoxMessage.Text = "";
                        return;
                    }
                }
                richTextBoxChat.Invoke(new Action(() => richTextBoxChat.Text += "Пользователь не найден! Введите имя еще раз:  " + '\n'));
                return;
            }
            if (namewassend && flag)
            {
                if (clients.client[clientName] == richTextBoxMessage.Text) //отправка имени пользователя
                {
                    richTextBoxMessage.Text = "";
                    richTextBoxChat.Clear();
                    this.message = clientName;
                    button1.Enabled = true;
                    timer1.Enabled = true; //запуск таймера
                }
                else
                {
                    richTextBoxChat.Invoke(new Action(() => richTextBoxChat.Text += "Пароль неверный! Попробуйте еще раз:  " + '\n'));
                    return;
                }
            }
            else //отправка текста сообщения
            {
                cm.NumberStatus = 1;
                this.message = richTextBoxMessage.Text + " (" + DateTime.Now.ToShortTimeString() + ')';
                timer1.Enabled = true; //запуск таймера
                richTextBoxChat.Text += this.clientName + ": " + this.message + '\n';
                richTextBoxMessage.Text = "";
                otrisovkaFoto();
            }
        }

        public void otrisovkaFoto()
        {
            richTextBoxChat.ReadOnly = false;
            foreach (KeyValuePair<string, int> keys in photo)
            {
                Image img = Image.FromFile(keys.Key);
                Clipboard.Clear();
                Clipboard.SetImage(img);

                richTextBoxChat.Focus();
                richTextBoxChat.SelectionStart = keys.Value;
                richTextBoxChat.ScrollToCaret();
                richTextBoxChat.Paste();
            }
            richTextBoxChat.ReadOnly = true;
        }
        // отправка сообщений
        void SendMessage(string message)
        {
            this.m = SerializeAndDeserialize.Serialize(message);
            cm.First = m;
            this.m = SerializeAndDeserialize.Serialize(this.cm); //Сериализация пакета с дальнейшей упаковкой в объект m
            data1 = this.m.Data; //Передача массива байтов Data из объекта m в массив data
            stream.Write(data1, 0, data1.Length); //Отправка массива байтов data серверу
            cm.NumberStatus = 0;
        }
        // получение сообщений
        void ReceiveMessage()
        {
            while (true)
            {
                //try
                //{
                    int numberOfBytesRead = 0;
                    byte[] readingData = new byte[6297630];
                    do
                    {
                        numberOfBytesRead = stream.Read(readingData, 0, readingData.Length); //Считываем данные, полученные от сервера, в массив байтов readingData
                    }
                    while (stream.DataAvailable);
                    this.m.Data = readingData;
                    cm = (ComplexMessage)SerializeAndDeserialize.Deserialize(m);
                    ComplexMessage complexMessage = (ComplexMessage)SerializeAndDeserialize.Deserialize(m);
                    if (cm.NumberStatus == 1)
                    {
                        message = (string)SerializeAndDeserialize.Deserialize(complexMessage.First);
                        richTextBoxChat.Invoke(new Action(() => richTextBoxChat.Text += message + '\n'));
                    }
                    if(cm.NumberStatus == 2)
                    {
                        MessageBox.Show("Фотка пришла");
                    }
                //}
                /*catch
                {

                    richTextBoxChat.Invoke(new Action(() => richTextBoxChat.Text += "Подключение прервано!" + "\n"));
                    Disconnect();
                }
                */
            }
        }
        static void Disconnect()
        {
            if (stream != null)
                stream.Close();//отключение потока
            if (client != null)
                client.Close();//отключение клиента
            Environment.Exit(0); //завершение процесса
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                if (flag)
                {
                    stream = client.GetStream(); // получаем поток
                                                 // запускаем новый поток для получения данных
                    Thread receiveThread = new Thread(new ThreadStart(ReceiveMessage));
                    receiveThread.Start(); //старт потока
                    SendMessage(clientName);
                    richTextBoxChat.Text += "Добро пожаловать, " +  clientName + "\n";
                    flag = false;
                    timer1.Enabled = false; //остановка таймера
                }
                else
                {
                    stream = client.GetStream(); // получаем поток
                                                 // запускаем новый поток для получения данных
                    Thread receiveThread = new Thread(new ThreadStart(ReceiveMessage));
                    receiveThread.Start(); //старт потока
                    if(cm.NumberStatus == 1)
                        SendMessage(message);
                    if (cm.NumberStatus == 2)
                        SendPhoto();
                    timer1.Enabled = false; //остановка таймера
                }
            }
            catch
            {
                MessageBox.Show("Катч");
            }
         }

        /*public void SendPhoto()
        {
            Image image = Image.FromFile(path);
            //запись в массив
            System.IO.MemoryStream memoryStream = new System.IO.MemoryStream();
            image.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
            this.m.Data = memoryStream.ToArray();
            cm.First = m;
            this.m = SerializeAndDeserialize.Serialize(this.cm); //Сериализация пакета с дальнейшей упаковкой в объект m
            data1 = this.m.Data; //Передача массива байтов Data из объекта m в массив data
            stream.Write(data1, 0, data1.Length); //Отправка массива байтов data серверу
            cm.NumberStatus = 0;
        }
        */
        public void SendPhoto()
        {
            Image image = Image.FromFile(path);
            //запись в массив
            System.IO.MemoryStream memoryStream = new System.IO.MemoryStream();
            image.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
            this.m = SerializeAndDeserialize.Serialize(image);
            cm.First = m;
            this.m = SerializeAndDeserialize.Serialize(this.cm); //Сериализация пакета с дальнейшей упаковкой в объект m
            data1 = this.m.Data; //Передача массива байтов Data из объекта m в массив data
            stream.Write(data1, 0, data1.Length); //Отправка массива байтов data серверу
            cm.NumberStatus = 0;
        }


        public string path;
        Dictionary<string, int> photo = new Dictionary<string, int>();
        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                path = openFileDialog1.FileName;
                Image img = Image.FromFile(path);
                richTextBoxChat.Text += this.clientName + ": " + " (" + DateTime.Now.ToShortTimeString() + ')' + '\n';
                photo.Add(path, richTextBoxChat.Text.Length - 8);
                cm.NumberStatus = 2;
                timer1.Enabled = true; //запуск таймера
                otrisovkaFoto();
            }
        }
    }
    class clients
    {
        public static Dictionary<string, string> client = new Dictionary<string, string>();  
    }
}
