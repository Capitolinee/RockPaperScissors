using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RockPaperScissors
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        // UDP通訊物件
        UdpClient U;

        // 監聽專用的執行緒
        Thread Th;

        // 對方資訊 (Opponent Pick)
        string oppPick;

        // 我方資訊
        string myPick;

        // 遊戲成果
        string gameResult;

        /// <summary>
        /// 監聽 UDP
        /// </summary>
        private void Listen()
        {
            //取得監聽用的通訊port
            int Port = int.Parse(tbReceivePort.Text);

            //建立UDP監聽器物件
            U = new UdpClient(Port); 

            //建立本機端
            IPEndPoint EP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), Port);

            while (true)    
            {
                var packet = U.Receive(ref EP);
                
                string str = Encoding.UTF8.GetString(packet, 0, packet.Length);

                if (str.ToLower().Contains("opppick"))
                {
                    oppPick = str.Split(':')[1];
                }
                else if (str.ToLower().Contains("result"))
                {
                    gameResult = str.Split(':')[1];

                    if (gameResult == "你贏了")
                    {
                        lblResult.Text = "你輸了";
                    }
                    else if(gameResult == "你輸了")
                    {
                        lblResult.Text = "你贏了";
                    }
                    else
                    {
                        lblResult.Text = "平手";
                    }
                    
                    btnReset.Enabled = true;
                }
            }
        }

        /// <summary>
        /// 回傳本機IP
        /// </summary>
        /// <returns></returns>
        private string MyIP()
        {
            string hn = Dns.GetHostName(); //取得本機的主機名稱
            IPAddress[] ips = Dns.GetHostEntry(hn).AddressList;//取得本機IP陣列(可能多個)
            foreach (IPAddress ip in ips) //一次(foreach)取一個ip(it)，列舉所有IP
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)//若ip格式是IPv4格式
                {
                    return ip.ToString(); //只回傳此格式的IP=>字串
                }
            }
            return ""; //若找不到IP，回傳空字串
        }        
        
        /// <summary>
        /// 傳送字串給對手
        /// </summary>
        /// <param name="str"></param>
        private void Send(string str)
        {
            // 對手的Port
            int Port = int.Parse(tbTargetPort.Text);

            // 宣告對手的UDP通訊物件
            UdpClient S = new UdpClient(tbTargetIP.Text, Port);
            
            // 轉碼:字串=>B陣列
            byte[] bStr = Encoding.UTF8.GetBytes(str);

            // 傳送選項給對手
            S.Send(bStr, bStr.Length); 
        }

        /// <summary>
        /// 遊戲規則，並回傳遊戲結果
        /// </summary>
        /// <param name="myPick"></param>
        /// <param name="oppoPick"></param>
        /// <returns></returns>
        private string CheckRules(string myPick, string oppoPick)
        {             
            // 遊戲結果
            string gameResult = string.Empty;

            if (myPick == oppoPick)
            {
                gameResult = "平手";
            }

            switch (myPick.ToLower())
            {
                // 我方出剪刀
                case "剪刀":

                    // 對方出叔投
                    if (oppoPick.ToLower() == "叔投")
                    {
                        gameResult = "你輸了";
                    }
                    // 對方出布
                    else if (oppoPick.ToLower() == "布")
                    {
                        gameResult = "你贏了";
                    }
                    break;

                // 我方出叔投
                case "叔投":

                    // 對方出布
                    if (oppoPick.ToLower() == "布")
                    {
                        gameResult = "你輸了";
                    }
                    // 對方出剪刀
                    else if (oppoPick.ToLower() == "剪刀")
                    {
                        gameResult = "你贏了";
                    }
                    break;

                // 我方出布
                case "布":

                    // 對方出剪刀
                    if (oppoPick.ToLower() == "剪刀")
                    {
                        gameResult = "你輸了";
                    }
                    // 對方出叔投
                    else if (oppoPick.ToLower() == "叔投")
                    {
                        gameResult = "你贏了";
                    }
                    break;
            }

            return gameResult;
        }

        /// <summary>
        /// 重製遊戲
        /// </summary>
        private void ResetGame()
        {
            btnReset.Enabled = false;
            radScissors.Enabled = true;
            btnSend.Enabled = true;
            oppPick = null;
            myPick = null;
            lblResult.Text = "";
        }

        #region 所有監聽事件

        /// <summary>
        /// 啟動監聽按鈕 監聽 - 按下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnActivate_Click(object sender, EventArgs e)
        {
            //忽略其他跨Thread操作的錯誤
            Control.CheckForIllegalCrossThreadCalls = false;

            //建立監聽的執行緒Thread->Listen
            Th = new Thread(Listen);

            //啟動監聽Thread
            Th.Start();

            //已啟動，將此按鈕暫時失效
            btnActivate.Enabled = false;
        }

        /// <summary>
        /// WinForm 監聽 - 載入事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Load(object sender, EventArgs e)
        {
            this.Text += "UDP_猜拳遊戲，本機的IP:" + MyIP();
            tbTargetIP.Text = MyIP();
        }

        /// <summary>
        /// WinForm 監聽 - 關閉事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                Th.Abort(); //關閉執行緒
                U.Close(); //關閉監聽器
            }
            catch
            {

            }
        }

        /// <summary>
        /// 傳送按鈕 監聽 - 按下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSend_Click(object sender, EventArgs e)
        {
            // 將送出按鈕禁用
            btnSend.Enabled = false;

            // 將重製按鈕啟用
            btnReset.Enabled = true;

            myPick = string.Empty;

            // 取得所有 radioButton 的值
            foreach (RadioButton rBtn in this.Controls.OfType<RadioButton>())
            {                
                if (rBtn.Checked)
                {
                    myPick = rBtn.Text;
                    break;
                }
            }

            // 送出我方資訊
            Send("oppPick:" + myPick);            

            // 若雙方接送出資料，執行遊戲規則，並將結果同步
            if (!string.IsNullOrEmpty(myPick) && !string.IsNullOrEmpty(oppPick)){

                // 算出結果
                var result = CheckRules(myPick, oppPick);

                // 顯示結果
                lblResult.Text = result;                

                // 將結果一同送出
                Send("result:" + result);
            }
        }        

        /// <summary>
        /// 重製按鈕 監聽 - 按下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnReset_Click(object sender, EventArgs e)
        {            
            ResetGame();
        }

        #endregion
    }
}
