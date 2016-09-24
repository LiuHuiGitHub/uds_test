using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Dongzr.MidiLite;
using System.Threading;
using System.IO;
using canlibCLSNET;

using User_Control;

using uds_test.Source.Transmit;
using uds_test.Source.BusParams;
using Uds;
using MyFormat;

namespace uds_test
{
    public partial class main : Form
    {
        can_driver can = new can_driver();

        public main()
        {
            InitializeComponent();
            this.Width = 720;
            this.Height = 620;

            SetStyle(
                     ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.ResizeRedraw
                     | ControlStyles.Selectable
                     | ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.UserPaint
                     | ControlStyles.SupportsTransparentBackColor,
                     true);
            BusParamsInit();
            TransmitInit();
            mmTimerInit();
        }

        #region Bus Params Page

        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();

        private void BusParamsInit()
        {
            can.SelectChannel(ref comboBoxChannel);
            LoadBusParamsSetting();
        }

        private void comboBoxChannel_Click(object sender, EventArgs e)
        {
            can.SelectChannel(ref comboBoxChannel);
        }

        private void buttonSchedulerSwitch_Click(object sender, EventArgs e)
        {
            if (buttonBusSwitch.Text == "Bus On")
            {
                if (can.OpenChannel(ref comboBoxChannel, ref comboBoxBaud, int.Parse(textBoxSjw.Text)) == true)
                {
                    buttonBusSwitch.Text = "Bus Off";
                    mmTimerStart();
                    comboBoxBaud.Enabled = false;
                    comboBoxChannel.Enabled = false;
                    textBoxSjw.Enabled = false;

                    timer = new System.Windows.Forms.Timer();
                    timer.Interval = 1000;
                    timer.Tick += delegate
                    {
                        int busload = 0;
                        if (can.BusLoad(ref busload) == true)
                        {
                            progressBarBusLoad.Value = busload;
                        }
                    };
                    timer.Enabled = true;
                }
            }
            else
            {
                can.CloseChannel();
                buttonBusSwitch.Text = "Bus On";
                mmTimerStop();
                comboBoxBaud.Enabled = true;
                comboBoxChannel.Enabled = true;
                textBoxSjw.Enabled = true;

                timer.Enabled = false;
                progressBarBusLoad.Value = 0;
            }
        }

        private void LoadBusParamsSetting()
        {
            textBoxSjw.Text = BusParams.Default.canSJW;
            comboBoxBaud.Text = BusParams.Default.canFreq;
        }

        void SaveBusParamsSetting()
        {
            BusParams.Default.canSJW = textBoxSjw.Text;
            BusParams.Default.canFreq = comboBoxBaud.Text;
            BusParams.Default.Save();
        }
        #endregion

        #region mmTimer_10ms

        MmTimer mmTimer;
        /// <summary>
        /// mmTime init
        /// </summary>
        void mmTimerInit()
        {
            mmTimer = new MmTimer();
            mmTimer.Mode = MmTimerMode.Periodic;
            mmTimer.Interval = 10;
            mmTimer.Tick += new EventHandler(mmTimer_tick);
        }
        /// <summary>
        /// mmTimer start
        /// </summary>
        void mmTimerStart()
        {
            mmTimer.Start();
        }
        /// <summary>
        /// mmTimer stpp
        /// </summary>
        void mmTimerStop()
        {
            mmTimer.Stop();
            mmTimer.Dispose();
        }

        /// <summary>
        /// mmtimer handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void mmTimer_tick(object sender, EventArgs e)
        {
            //匿名委托，用于this.Invoke调用
            EventHandler TextBoxUpdate = delegate
            {
                UpdateAndTransmit();
            };
            try { Invoke(TextBoxUpdate); } catch { };
            int id;
            int dlc;
            long time;
            byte[] data = new byte[8];
            if (can.ReadData(out id, ref data, out dlc, out time) == true)
            {
                trans.Can_Trans_RxFrams(id, data, dlc);
            }
            trans.CanTrans_Manage(10);
        }
        #endregion

        #region Transmit Page

        List<CycleCtrl> cycleCtrlList = new List<CycleCtrl>();
        List<OneShotCtrl> oneShotCtrlList = new List<OneShotCtrl>();

        private void TransmitInit()
        {
            cycleCtrlList.Add(cycleCtrl1);
            cycleCtrlList.Add(cycleCtrl2);
            cycleCtrlList.Add(cycleCtrl3);
            cycleCtrlList.Add(cycleCtrl4);
            cycleCtrlList.Add(cycleCtrl5);
            cycleCtrlList.Add(cycleCtrl6);
            cycleCtrlList.Add(cycleCtrl7);
            cycleCtrlList.Add(cycleCtrl8);
            oneShotCtrlList.Add(oneShotCtrl1);
            oneShotCtrlList.Add(oneShotCtrl2);
            oneShotCtrlList.Add(oneShotCtrl3);
            oneShotCtrlList.Add(oneShotCtrl4);

            LoadTransmitSetting();
        }

        private void UpdateAndTransmit()
        {
            foreach (CycleCtrl Sch in cycleCtrlList)
            {
                if (Sch.Check)
                {
                    Sch.TimeCnt += 10;
                    if (Sch.TimeCnt >= Sch.Interval)
                    {
                        Sch.TimeCnt -= Sch.Interval;

                        can.WriteData(Sch.Id, Sch.Dat, Sch.Dlc);
                    }
                }
            }
            foreach (OneShotCtrl Sch in oneShotCtrlList)
            {
                if (Sch.Updated)
                {
                    Sch.Updated = false;

                    can.WriteData(Sch.Id, Sch.Dat, Sch.Dlc);
                }
            }
        }

        private void LoadTransmitSetting()
        {
            if (Transmit.Default.SaveFlag)
            {
                int index = 0;
                /*Cycle Ctrl*/
                foreach (CycleCtrl Sch in cycleCtrlList)
                {
                    if (index < Transmit.Default.CtrlData.Length)
                    {
                        Sch.Init(Transmit.Default.CtrlData[index++]);
                    }
                }
                /*One Shot Ctrl*/
                foreach (OneShotCtrl Sch in oneShotCtrlList)
                {
                    if (index < Transmit.Default.CtrlData.Length)
                    {
                        Sch.Init(Transmit.Default.CtrlData[index++]);
                    }
                }
            }
            else
            {
                this.ClearTransmit();
            }
        }
        /// <summary>
        /// clear local settings
        /// </summary>
        private void ClearTransmit()
        {
            /*Cycle Ctrl*/
            foreach (CycleCtrl Sch in cycleCtrlList)
            {
                Sch.Clear();
            }
            /*One Shot Ctrl*/
            foreach (OneShotCtrl Sch in oneShotCtrlList)
            {
                Sch.Clear();
            }
        }
        /// <summary>
        /// save local settings
        /// </summary>
        void SaveTransmitSetting()
        {
            /*Cycle Ctrl*/
            string[] stringArr = new string[cycleCtrlList.Count + oneShotCtrlList.Count];
            int index = 0;
            foreach (CycleCtrl Sch in cycleCtrlList)
            {
                stringArr[index++] = Sch.ToSetting();
            }
            /*One Shot Ctrl*/
            foreach (OneShotCtrl Sch in oneShotCtrlList)
            {
                stringArr[index++] = Sch.ToSetting();
            }
            Transmit.Default.CtrlData = stringArr;
            Transmit.Default.SaveFlag = true;
            Transmit.Default.Save();
        }
        /// <summary>
        /// clear data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearTransmit_Click(object sender, EventArgs e)
        {
            ClearTransmit();
        }
        /// <summary>
        /// save data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveTransmit_Click(object sender, EventArgs e)
        {
            SaveTransmitSetting();
        }

        #endregion

        #region TextClick

#if flase
        private void copyToClipBoardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(textBoxShow.Text != "")
            {
                Clipboard.SetDataObject(textBoxShow.Text);
            }
        }

        private void saveToFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StreamWriter myStream;
            saveFileDialog.Filter = "text文本 (*.txt)|*.txt|所有文件 (*.*)|*.*";
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.RestoreDirectory = true;
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                myStream = new StreamWriter(saveFileDialog.FileName);
                myStream.Write(textBoxShow.Text);
                myStream.Close();
            }
        }
#endif
        #endregion

        private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab == tabPageBusParams)
            {
            }
            else if (tabControl.SelectedTab == tabPageTransmit)
            {
            }
        }

        private void main_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveBusParamsSetting();
        }

        private void main_FormResized(object sender, EventArgs e)
        {
            Text = "uds_test" + " Width:" + Size.Width.ToString("d3") + " Height:" + Size.Height.ToString("d3");
        }

        uds_can_trans trans = new uds_can_trans();

        private void button_Click(object sender, EventArgs e)
        {
            trans.rx_id = 0x7B8;
            trans.tx_id = 0x7B0;

            trans.CanTrans_TxMsg(textBox2.Text.StringToHex());
        }

        bool delete_char_flag = false;
        private void trans_data_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox textbox = (TextBox)sender;
            string strings = textbox.Text;
            delete_char_flag = true;

            if (e.KeyChar != 8  /* 允许使用退格符 */
                && e.KeyChar != 0x0003 /* 允许使用复制符 */
                && e.KeyChar != 0x0016 /* 允许使用粘贴符 */
                && e.KeyChar != 0x0018 /* 允许使用剪切符 */
                && !Char.IsDigit(e.KeyChar)
                && !(((int)e.KeyChar >= 'A' && (int)e.KeyChar <= 'F'))
                && !(((int)e.KeyChar >= 'a' && (int)e.KeyChar <= 'f'))
                )
            {
                e.Handled = true;
            }
            if (e.KeyChar == 8)
            {
                delete_char_flag = false;
            }
            else if (e.KeyChar == '\r')
            {
                trans.rx_id = 0x7B8;
                trans.tx_id = 0x7B0;

                trans.CanTrans_TxMsg(textBox2.Text.StringToHex());
            }
        }

        private void trans_data_TextChanged(object sender, EventArgs e)
        {
            TextBox textbox = (TextBox)sender;
            string strings = textbox.Text;
            if (delete_char_flag == false)
            {
                return;
            }
            strings = strings.Replace(" ", "");     //将原string中的空格删除
            strings = strings.Replace("0x", "");
            strings = strings.Replace("0X", "");
            strings = strings.Replace(",", "");
            if (strings.Length == 0 || strings.Length % 2 != 0)
            {
                return;
            }
            strings = strings.StringToHex().HexToStrings(" ");
            textbox.Text = strings;
            textbox.SelectionStart = textbox.Text.Length;
        }
    }
}
