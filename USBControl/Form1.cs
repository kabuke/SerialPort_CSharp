using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;
using System.Text.RegularExpressions;
using System.IO;

namespace USBControl
{
    public partial class Form1 : Form
    {
        //USBHIDDRIVER.USBInterface usb = new USBInterface("vid_413c", "pid_2003");
        //byte[] currentread = new byte[1024];
        private Boolean sending;
        private Boolean receiving;
        private Thread t;
        delegate void Display(Byte[] buffer);
        delegate void Display2(Byte[] buffer);
        delegate void SetTextCallback(string text);
        delegate void SetListCallback(List<string> ListArray);
        List<string> hexStringLists = new List<string>();
        List<string> parseStringLists = new List<string>();
        string printOUT;
        string SourceData;
        string[] SplitData;
        int TotalDataCount;
        int DataCount;
        int label3Count;
        string oldSTR;

        private void DisplayText(Byte[] buffer)
        {
            string hex = BitConverter.ToString(buffer);
            hex = hex.Replace("-", "");
            hex = oldSTR + hex;
            hex = hex.Replace("0D0A", "#");
            //MessageBox.Show(String.Format(hex));
            SplitData = hex.Split('#');
            oldSTR = SplitData[SplitData.Length - 1];
            //SourceData += hex;
            //DateTime time_start = DateTime.Now;//計時開始 取得目前時間
            for (int x = 0; x < (SplitData.Length - 1); x++)
            {
                //this.SetTextToBox(SplitData[x]);
                //richTextBox2.Text += SplitData[x] + "\r\n";
                hexStringLists.Add(SplitData[x]);
                parseStringLists.Add(ParseSingleData(SplitData[x]));
            }
            label3Count += SplitData.Length;
            label3.Text = hexStringLists.Count.ToString() + " Count";
            Array.Clear(SplitData, 0, SplitData.Length);

            //DateTime time_end = DateTime.Now;//計時結束 取得目前時間
            //string result2 = ((TimeSpan)(time_end - time_start)).TotalMilliseconds.ToString();
            //MessageBox.Show(String.Format("Parse finish " + result2 + " Milliseconds " + SplitData.Length));

            //MessageBox.Show(String.Format(oldSTR + " " + SplitData.Length + "\r\n" + SplitData[0] + "\r\n" + SplitData[1] + "\r\n" + SplitData[SplitData.Length - 3] + "\r\n" + SplitData[SplitData.Length - 2] + "\r\n" + SplitData[SplitData.Length - 1]));

            // label3.Text = SourceData.Length + " length ";
            // richTextBox2.Lines = hexStringLists.ToArray();
            //if (label3.TextAlignChanged)
        }

        private void SetTextToBox(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.richTextBox2.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetTextToBox);
                this.Invoke(d, new object[] { text });
            }
            else
            {

                this.richTextBox2.Text += text + "\r\n";
            }
        }

        static String ParseSingleData(string rawdata)
        {
            if (rawdata.Length < 31)
            {
                if (rawdata.Length == 30)
                {
                    string tempdata = '0' + rawdata;
                    rawdata = tempdata;
                }
            }
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);

            string rtn = Convert.ToInt32(rawdata.Substring(0, 2), 16) + "|";
            rtn += ParseLatLon(rawdata.Substring(10, 8)) + "|";
            rtn += ParseLatLon(rawdata.Substring(2, 8)) + "|";

            rtn += origin.AddSeconds(Convert.ToInt32(rawdata.Substring(18, 8), 16)).ToString("yyyy-MM-dd HH:mm:ss") + "|";
            rtn += Convert.ToInt32(rawdata.Substring(26, 2), 16) + "|";
            rtn += ParseAngle(rawdata.Substring(28, 4));
            return rtn;
        }


        public Form1()
        {
            InitializeComponent();
            InitializeComboBox();
            getAvailablePorts();
            backgroundWorker2.WorkerReportsProgress = true;
            //backgroundWorker2.DoWork += new DoWorkEventHandler(backgroundWorker2_DoWork);
            backgroundWorker2.ProgressChanged += new ProgressChangedEventHandler(backgroundWorker2_ProgressChanged);
        }

        void getAvailablePorts ()
        {
            String[] ports = SerialPort.GetPortNames();
            comboBox1.Items.AddRange(ports);
        }

        private void InitializeComboBox()
        {
            string[] employees = new string[]{"Beep OFF","Beep ON"};
            this.comboBox2.Items.AddRange(employees);
            this.comboBox2.IntegralHeight = false;
            this.comboBox2.MaxDropDownItems = 2;
            //this.comboBox2.DropDownStyle = ComboBoxStyle.DropDownList;
            this.comboBox2.Name = "comboBox2";
            //this.comboBox2.TabIndex = 0;
            this.comboBox2.SelectedIndex = 0;
            //this.comboBox2.SelectedIndexChanged += new System.EventHandler(comboBox2_SelectedIndexChanged);

            string[] modes = new string[] { "ECO SAM Mode", "BATTERY Mode" };
            this.comboBox3.Items.AddRange(modes);
            this.comboBox3.IntegralHeight = false;
            this.comboBox3.MaxDropDownItems = 2;
            //this.comboBox2.DropDownStyle = ComboBoxStyle.DropDownList;
            this.comboBox3.Name = "comboBox3";
            //this.comboBox3.TabIndex = 0;
            this.comboBox3.SelectedIndex = 0;
            //this.comboBox2.SelectedIndexChanged += new System.EventHandler(comboBox2_SelectedIndexChanged);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                if (comboBox1.Text == "")
                {
                    richTextBox2.Text = "Please select Port....";
                }
                else
                {
                    // 連接com port時的初始化設定。
                    serialPort1.PortName = comboBox1.Text;
                    serialPort1.BaudRate = 115200;
                    serialPort1.DataBits = 8;
		            serialPort1.Parity = Parity.None;
                    serialPort1.ReadTimeout = 2000;
                    //serialPort1.DataReceived += new SerialDataReceivedEventHandler(comport_DataReceived);
                    serialPort1.Open();

                    richTextBox2.Text = comboBox1.Text + " Connected.\r\n";
                    button2.Enabled = false; /* 連接成功後就把connect按鈕功能取消 */
                    button3.Enabled = true;
                    button4.Enabled = true;
                    button5.Enabled = true;
                    button6.Enabled = true;
                    button7.Enabled = true;
                    button8.Enabled = true;
                    button9.Enabled = true;
                    button10.Enabled = true;
                    textBox1.Enabled = true;
                    textBox2.Enabled = true;
                    textBox3.Enabled = true;
                    textBox4.Enabled = true;
                    textBox5.Enabled = true;
                    textBox6.Enabled = true;
                    textBox7.Enabled = true;
                    textBox8.Enabled = true;
                    textBox9.Enabled = true;
                    textBox10.Enabled = true;
                    textBox11.Enabled = true;
                    comboBox2.Enabled = true;
                    comboBox3.Enabled = true;
                    progressBar1.Value = 100;
                }
            }
            catch (UnauthorizedAccessException)
            {
                richTextBox2.Text = "Unauthorize Access";
            }
            finally
            {
                sending = false;
            }
        }

        private void ShowLines(List<string> ListArray)
        {
            if (this.richTextBox2.InvokeRequired)
            {
                SetListCallback d = new SetListCallback(ShowLines);
                this.Invoke(d, new object[] { ListArray });
            }
            else
            {
                this.richTextBox2.Lines = parseStringLists.ToArray();
            }
        }

        private void GotoLineShow()
        {
            this.ShowLines(parseStringLists);
            
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.backgroundWorker2.RunWorkerAsync(parseStringLists.Count);
            if (serialPort1.IsOpen && !sending && parseStringLists.Count > 0)
            {
                this.richTextBox2.ResetText();
                //this.richTextBox2.Lines = parseStringLists.ToArray();
                t = new Thread(GotoLineShow);
                t.IsBackground = true;
                t.Start();

            }
        }

        private void backgroundWorker2_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.progressBar2.Value = e.ProgressPercentage;
        }

        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            // Your background task goes here
            for (int i = 0; i <= 100; i++)
            {
                // Report progress to 'UI' thread
                backgroundWorker2.ReportProgress(i);
                // Simulate long task
                System.Threading.Thread.Sleep(1);
            }
        }

        static String ParseAngle(string HexAngle)
        {
            int Angle0 = Convert.ToInt32(HexAngle, 16) / 10;
            int Angle1 = Convert.ToInt32(HexAngle, 16) % 10;

            string lastStr = Convert.ToString(Angle0) + '.' + Convert.ToString(Angle1) + '°';

            return lastStr;
        }

        static String ParseLatLon(string HexLatLon)
        {
            int LatLon0 = Convert.ToInt32(HexLatLon, 16) / 10000;
            int LatLon1 = Convert.ToInt32(HexLatLon, 16) % 10000;
            int extent = (int)(LatLon0) / 60;
            int remainder = (int)(LatLon0) % 60;
            string lastStr = Convert.ToString(extent) + '°' + Convert.ToString(remainder) + '.' + Convert.ToString(LatLon1);

            return lastStr;
        }

        static DateTime ConvertFromUnixTimestamp(int timestamp)
        {
            //MessageBox.Show(String.Format(timestamp.ToString()));
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            //DateTime origin = new DateTime(1970, 1, 1);
            return origin.AddSeconds(timestamp);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            serialPort1.Close();
            progressBar1.Value = 0;
            richTextBox2.Text = "Disconnected.";
            button2.Enabled = true; /* 斷線後就把connect按鈕功能打開 */
            button3.Enabled = false;
            button4.Enabled = false;
            button5.Enabled = false;
            button6.Enabled = false;
            button7.Enabled = false;
            button8.Enabled = false;
            button9.Enabled = false;
            button10.Enabled = false;
            textBox1.Enabled = false;
            textBox2.Enabled = false;
            textBox3.Enabled = false;
            textBox4.Enabled = false;
            textBox5.Enabled = false;
            textBox6.Enabled = false;
            textBox7.Enabled = false;
            textBox8.Enabled = false;
            textBox9.Enabled = false;
            textBox10.Enabled = false;
            textBox11.Enabled = false;
            comboBox2.Enabled = false;
            comboBox3.Enabled = false;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
                oldSTR.Remove(0, oldSTR.Length);
                char[] all_whitespaces = new char[] {
                    // SpaceSeparator category
                    '\u0020', '\u1680', '\u180E', '\u2000', '\u2001', '\u2002', '\u2003', 
                    '\u2004', '\u2005', '\u2006', '\u2007', '\u2008', '\u2009', '\u200A', 
                    '\u202F', '\u205F', '\u3000',
                    // LineSeparator category
                    '\u2028',
                    // ParagraphSeparator category
                    '\u2029',
                    // Latin1 characters
                    '\u0009', '\u000A', '\u000B', '\u000C', '\u000D', '\u0085', '\u00A0',
                    // ZERO WIDTH SPACE (U+200B) & ZERO WIDTH NO-BREAK SPACE (U+FEFF)
                    '\u200B', '\uFEFF'
                };
                oldSTR.Trim(all_whitespaces);
            }
            catch
            {
                oldSTR = null;
            }

            hexStringLists = new List<string>();
            parseStringLists = new List<string>();
            Thread.Sleep(16);

            if (serialPort1.IsOpen && !sending)
            {
                richTextBox2.ResetText();
                t = new Thread(ReadDataLog);
                t.IsBackground = true;
                t.Start(serialPort1 as Object);
            }
            button5.Enabled = false;
        }

        private void ReadDataLog(Object port)
        {
            Byte[] buffer = new Byte[1024];
            for (int i = 0; i < 1024; i++)
            {
                buffer[i] = (Byte)(i % 256);
            }
            sending = true;
            try
            {
                //byte[] byteToCommand = Encoding.ASCII.GetBytes("$PSRF101,0,0,0,0,0,0,12,4*10\r\n");  // 工程模式指令
                byte[] byteToCommand = Encoding.ASCII.GetBytes("$READDATALOG,A*2B\r\n");
                //byte[] byteToCommand = Encoding.ASCII.GetBytes("$READSET,A*3D\r\n");
                serialPort1.Write(byteToCommand, 0, byteToCommand.Length);
                //(port as SerialPort).Write(buffer, 0, buffer.Length);
                receiving = true;
                t = new Thread(DoReceive);
                t.IsBackground = true;
                t.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("Error Message:{0}", ex.ToString()));
            }
            finally
            {
                sending = false;
            }

        }


        private void SendData(string dataToSend)
        {
            sending = true;
            try
            {
                byte[] byteToCommand = Encoding.ASCII.GetBytes(dataToSend);
                serialPort1.Write(byteToCommand, 0, byteToCommand.Length);
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("Error Message:{0}", ex.ToString()));
            }
            finally
            {
                sending = false;
            }

        }

        private void DoReceive()
        {
            Byte[] buffer = new Byte[1024];
            Thread.Sleep(100);
            while (receiving)
            {
                try
                {
                    if (serialPort1.BytesToRead > 0)
                    {
                        Int32 length = serialPort1.Read(buffer, 0, buffer.Length);
                        Array.Resize(ref buffer, length);
                        Display d = new Display(DisplayText);
                        this.Invoke(d, new Object[] { buffer });
                        Array.Resize(ref buffer, 1024);
                    }
                }
                catch (Exception)
                {
                    //MessageBox.Show(String.Format("Error Message:{0}", ex.ToString()));
                    //MessageBox.Show(String.Format("BytesToRead finish"));
                }
                
                Thread.Sleep(16);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            byte[] byteToCommand = Encoding.ASCII.GetBytes("$CLEAR,DATALOG,A*4C\r\n");
            serialPort1.Write(byteToCommand, 0, byteToCommand.Length);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            try
            {
                SendData("$READSET,A*3D\r\n");
                SourceData = serialPort1.ReadLine();
                richTextBox2.Text = SourceData;
                string[] Setting = SourceData.Split(',');

                textBox10.Text = Setting[1].ToString();
                textBox11.Text = Setting[2].ToString();
                textBox1.Text = Setting[3].ToString();
                textBox2.Text = Setting[4].ToString();
                textBox3.Text = Setting[5].ToString();
                textBox4.Text = Setting[6].ToString();

                textBox5.Text = Setting[7].ToString();
                textBox6.Text = Setting[8].ToString();
                textBox7.Text = Setting[9].ToString();
                textBox8.Text = Setting[10].ToString();

                textBox9.Text = Setting[11].ToString();

                comboBox2.SelectedIndex = Convert.ToInt32(Setting[12]);
                comboBox3.SelectedIndex = Convert.ToInt32(Setting[13]);
            }
            catch
            {
                MessageBox.Show(String.Format("Not Responses\r\nTry again Please.."));
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            try
            {
                // $SET,1234567890ABCDEF,2016049999,110,02,03,300, 02,20,02,20 ,15,1,0,A*03
                string CommandStr = "SET," + textBox10.Text.ToString() + ",";
                CommandStr += textBox11.Text.ToString() + ",";
                CommandStr += textBox1.Text.ToString().PadLeft(3, '0') + ",";
                CommandStr += textBox2.Text.ToString().PadLeft(2, '0') + ",";
                CommandStr += textBox3.Text.ToString().PadLeft(2, '0') + ",";
                CommandStr += textBox4.Text.ToString().PadLeft(3, '0') + ",";

                CommandStr += textBox5.Text.ToString().PadLeft(2, '0') + ",";
                CommandStr += textBox6.Text.ToString().PadLeft(2, '0') + ",";
                CommandStr += textBox7.Text.ToString().PadLeft(2, '0') + ",";
                CommandStr += textBox8.Text.ToString().PadLeft(2, '0') + ",";

                CommandStr += textBox9.Text.ToString().PadLeft(2, '0') + ",";
                CommandStr += comboBox2.SelectedIndex.ToString() + ",";
                CommandStr += comboBox3.SelectedIndex.ToString() + ",A";

                string checksum = HEXChecksum(CommandStr).PadLeft(2, '0');
                CommandStr = "$" + CommandStr + "*" + checksum;
                //MessageBox.Show(String.Format(CommandStr));
                try
                {
                    SendData(CommandStr + "\r\n");
                    SourceData = serialPort1.ReadLine();
                    string[] Setting = SourceData.Split(',');
                    if (Setting[1] == "OK") {
                        MessageBox.Show(String.Format("Write setting " + Setting[1] + " !"));
                    }
                    else
                    {
                        MessageBox.Show(String.Format("error " + SourceData + " !"));
                    }
                }
                catch
                {
                    MessageBox.Show(String.Format("Not Responses\r\nTry again Please.."));
                }


            }
            catch
            {
                MessageBox.Show(String.Format("Not Responses\r\nTry again Please.."));
            }
        }

        private void richTextBox2_TextChanged(object sender, EventArgs e)
        {
            //richTextBox2.SelectionStart = richTextBox2.TextLength;
            //richTextBox2.ScrollToCaret();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            Stream myStream;
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            saveFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            saveFileDialog1.FilterIndex = 1;
            saveFileDialog1.FileName = "gpslog.txt";
            saveFileDialog1.RestoreDirectory = true;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if ((myStream = saveFileDialog1.OpenFile()) != null)
                {
                    // Code to write the stream goes here.
                    myStream.Close();
                }
                //System.IO.File.WriteAllLines(saveFileDialog1.FileName, parseStringLists);
                LogData(saveFileDialog1.FileName, parseStringLists);
            }
        }

        private void LogData(string str, List<string> list)
        {
            //FolderBrowserDialog path = new FolderBrowserDialog();
            //path.ShowDialog();
            //string SaveFileInfo = path.SelectedPath+"gpslog.txt";
            System.IO.File.WriteAllLines(str, list);
            MessageBox.Show(String.Format(str + "\r\nGpsData output finish"));
            
            //using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"gpslog.txt"))
            //{
            //    string[] strs = str.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            //    foreach (string line in strs)
            //    {
            //        file.WriteLine(line + "\r\n");
            //    }
            //}
            
        }

        // 查詢大寫十六進制的checksum
        public string HEXChecksum(string dataToCalculate)
        {
            byte[] byteToCalculate = Encoding.ASCII.GetBytes(dataToCalculate);
            string checksum = Convert.ToString(GetXOR(byteToCalculate), 16);
            return checksum.ToUpper();
        }

        // 十進制的checksum
        public static byte GetXOR(byte[] Cmd)
        {
            byte check = (byte)(Cmd[0] ^ Cmd[1]);
            for (int i = 2; i < Cmd.Length; i++)
            {
                check = (byte)(check ^ Cmd[i]);
            }
            return check;
        }

        private void button10_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen && !sending)
            {
                richTextBox2.ResetText();
                this.t = new Thread(this.ParseRAWData);
                this.t.IsBackground = true;
                this.t.Start();
            }
        }

        private void ParseRAWData()
        {
            //SplitData = SourceData.Split('#');
            DateTime time_start = DateTime.Now;//計時開始 取得目前時間
            for (int i = 0; i < hexStringLists.Count; i++)
            {
                try {
                    hexStringLists[i] = hexStringLists[i].Substring(0, 18) + "|" + hexStringLists[i].Substring(18, 8) + "|" + hexStringLists[i].Substring(26, 6);
                }
                catch
                {

                }

            }

            DateTime time_end = DateTime.Now;//計時結束 取得目前時間
            string result2 = ((TimeSpan)(time_end - time_start)).TotalMilliseconds.ToString();
            MessageBox.Show(String.Format("Parse finish " + result2 + " sec"));

            System.IO.File.WriteAllLines("RawGpsData.txt", hexStringLists);
            MessageBox.Show(String.Format("RawGpsData output finish"));
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }



        //// checksum 的三種方法
        //public static byte CheckSum(byte[] array)
        //{
        //    return array.Aggregate<byte, byte>(0, (current, b) => (byte)((current + b) & 0xff));
        //}

        //public static byte ComputeAdditionChecksum(byte[] data)
        //{
        //    long longSum = data.Sum(x => (long)x);
        //    return unchecked((byte)longSum);
        //}

        //private string CalculateChecksum(string dataToCalculate)
        //{
        //    byte[] byteToCalculate = Encoding.ASCII.GetBytes(dataToCalculate);
        //    int checksum = 0;
        //    foreach (byte chData in byteToCalculate)
        //    {
        //        //richTextBox1.Text += chData+"\r\n";
        //        checksum += chData;
        //    }
        //    checksum &= 0xff;
        //    richTextBox1.Text += checksum + "\r\n";
        //    richTextBox1.Text += checksum.ToString("X2") + "\r\n";
        //    return checksum.ToString("X2");
        //}

    }
}
