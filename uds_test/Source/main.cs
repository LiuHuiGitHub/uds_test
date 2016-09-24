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
        can_driver driver = new can_driver();
        uds_trans trans = new uds_trans();
        List<uds_seriver> serivers_list = new List<Uds.uds_seriver>();

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
            uds_init();
            mmTimerInit();
        }

        #region Bus Params Page

        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();

        private void BusParamsInit()
        {
            driver.SelectChannel(ref comboBoxChannel);
            LoadBusParamsSetting();
        }

        private void comboBoxChannel_Click(object sender, EventArgs e)
        {
            driver.SelectChannel(ref comboBoxChannel);
        }

        private void buttonSchedulerSwitch_Click(object sender, EventArgs e)
        {
            if (buttonBusSwitch.Text == "Bus On")
            {
                if (driver.OpenChannel(ref comboBoxChannel, ref comboBoxBaud, int.Parse(textBoxSjw.Text)) == true)
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
                        if (driver.BusLoad(ref busload) == true)
                        {
                            progressBarBusLoad.Value = busload;
                        }
                        //can.WriteData(0x7DF, new byte[] { 0x3E, 0x80, 0x00,0x00,0x00,0x00,0x00,0x00 }, 8);
                    };
                    timer.Enabled = true;

                }
            }
            else
            {
                driver.CloseChannel();
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
            byte[] dat = new byte[8];
            if (driver.ReadData(out id, ref dat, out dlc, out time) == true)
            {
                trans.Can_Trans_RxFrams(id, dat, dlc);
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

                        driver.WriteData(Sch.Id, Sch.Dat, Sch.Dlc);
                    }
                }
            }
            foreach (OneShotCtrl Sch in oneShotCtrlList)
            {
                if (Sch.Updated)
                {
                    Sch.Updated = false;

                    driver.WriteData(Sch.Id, Sch.Dat, Sch.Dlc);
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

        #region TextBoxRightClick

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StreamWriter myStream;
            saveFileDialog.Filter = "text文本 (*.txt)|*.txt|所有文件 (*.*)|*.*";
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.RestoreDirectory = true;
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                myStream = new StreamWriter(saveFileDialog.FileName);
                myStream.Write(textBoxStream.Text);
                myStream.Close();
            }
        }

        private void CopyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetDataObject(textBoxStream.Text);
        }

        private void DeleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textBoxStream.Text = "";
        }

        #endregion

        #region UDS
        uds_seriver now_seriver = new uds_seriver();
        uds_seriver.SubFunction now_sub_function = new uds_seriver.SubFunction();

        private void uds_init()
        {
            driver = new can_driver();
            trans = new uds_trans();
            serivers_list = new List<Uds.uds_seriver>();

            /*使用事件委托传参*/
            driver.EventWriteData += new EventHandler(
                (sender1, e1) =>
                {
                    can_driver.WriteDataEventArgs writeFarme = (can_driver.WriteDataEventArgs)e1;
                    textBoxStream.Text += writeFarme.id.ToString("X3") + " "
                    + writeFarme.dlc.ToString("X1") + " "
                    + writeFarme.dat.HexToStrings(" ") + "\r\n";
                }
                );
            trans.EventTxFarms += new EventHandler(
                (sender1, e1) =>
                {
                    uds_trans.FarmsEventArgs TxFarme = (uds_trans.FarmsEventArgs)e1;
                    EventHandler TextBoxUpdate = delegate
                    {
                        textBoxStream.Text += TxFarme.id.ToString("X3") + " "
                        + TxFarme.dlc.ToString("X1") + " "
                        + TxFarme.dat.HexToStrings(" ") + "\r\n";
                    };
                    try { Invoke(TextBoxUpdate); } catch { };
                }
                );
            trans.EventRxFarms += new EventHandler(
                (sender1, e1) =>
                {
                    uds_trans.FarmsEventArgs RxFarme = (uds_trans.FarmsEventArgs)e1;
                    EventHandler TextBoxUpdate = delegate
                    {
                        textBoxStream.Text += RxFarme.id.ToString("X3") + " "
                        + RxFarme.dlc.ToString("X1") + " "
                        + RxFarme.dat.HexToStrings(" ") + "\r\n";
                    };
                    try { Invoke(TextBoxUpdate); } catch { };
                }
                );
            uds_seriver seriver = new uds_seriver();
            uds_seriver.SubFunction sub_function = new uds_seriver.SubFunction();

            #region $11
            seriver.sid = "11";
            seriver.name = "ECU Reset";
            seriver.sub_function_list = new List<uds_seriver.SubFunction>();
            //@parameter
            //@sub_function_1
            sub_function = new uds_seriver.SubFunction();
            sub_function.id = "01";
            sub_function.name = "Hard Reset";
            seriver.sub_function_list.Add(sub_function);
            //@sub_function_2
            sub_function = new uds_seriver.SubFunction();
            sub_function.id = "02";
            sub_function.name = "Key Off On Reset";
            seriver.sub_function_list.Add(sub_function);
            //@sub_function_3
            sub_function = new uds_seriver.SubFunction();
            sub_function.id = "03";
            sub_function.name = "Soft Reset";
            seriver.sub_function_list.Add(sub_function);
            //@sub_function_4
            sub_function = new uds_seriver.SubFunction();
            sub_function.id = "04";
            sub_function.name = "Enable Rapid Power Shut Down";
            seriver.sub_function_list.Add(sub_function);
            //@sub_function_5
            sub_function = new uds_seriver.SubFunction();
            sub_function.id = "05";
            sub_function.name = "Disable Rapid Power Shut Down";
            seriver.sub_function_list.Add(sub_function);
            serivers_list.Add(seriver);
            #endregion

            #region $14
            seriver = new uds_seriver();
            seriver.sid = "14";
            seriver.name = "Clear Diagnostic Information";
            seriver.parameter = "FF FF FF";
            seriver.sub_function_list = new List<uds_seriver.SubFunction>();
            serivers_list.Add(seriver);
            #endregion

            #region $19
            seriver = new uds_seriver();
            seriver.sid = "19";
            seriver.name = "Read DTC Information";
            seriver.sub_function_list = new List<uds_seriver.SubFunction>();
            //@sub_function_1
            sub_function = new uds_seriver.SubFunction();
            sub_function.id = "01";
            sub_function.name = "Report Number Of DTC By Status Mask";
            sub_function.parameter = "FF";
            seriver.sub_function_list.Add(sub_function);
            //@sub_function_2
            sub_function = new uds_seriver.SubFunction();
            sub_function.id = "02";
            sub_function.name = "Report DTC By Status Mask";
            sub_function.parameter = "FF";
            seriver.sub_function_list.Add(sub_function);
            //@sub_function_3
            sub_function = new uds_seriver.SubFunction();
            sub_function.id = "04";
            sub_function.name = "Report DTC Snapshot Record By DTC Number";
            sub_function.parameter = "FF FF FF";
            seriver.sub_function_list.Add(sub_function);
            //@sub_function_4
            sub_function = new uds_seriver.SubFunction();
            sub_function.id = "06";
            sub_function.name = "Report DTC Extended Datar Record By DTC Number";
            sub_function.parameter = "FF FF FF FF";
            seriver.sub_function_list.Add(sub_function);
            //@sub_function_5
            sub_function = new uds_seriver.SubFunction();
            sub_function.id = "0A";
            sub_function.name = "Report Supported DTC";
            seriver.sub_function_list.Add(sub_function);
            serivers_list.Add(seriver);
            #endregion

            #region $22
            seriver = new uds_seriver();
            seriver.sid = "22";
            seriver.name = "Read Data By Identifier";
            seriver.sub_function_list = new List<uds_seriver.SubFunction>();
            serivers_list.Add(seriver);
            #endregion

            #region $23
            seriver = new uds_seriver();
            seriver.sid = "23";
            seriver.name = "Read Memory By Address";
            seriver.parameter = "00 00 00 00 00 01";
            seriver.sub_function_list = new List<uds_seriver.SubFunction>();
            //@sub_function_1
            sub_function = new uds_seriver.SubFunction();
            sub_function.id = "42";
            sub_function.name = "Address Lenght And Read Memory Lenght";
            seriver.sub_function_list.Add(sub_function);
            serivers_list.Add(seriver);
            #endregion

            #region $28
            seriver = new uds_seriver();
            seriver.sid = "28";
            seriver.name = "Communication Control";
            seriver.parameter = "03";
            seriver.sub_function_list = new List<uds_seriver.SubFunction>();
            //@sub_function_1
            sub_function = new uds_seriver.SubFunction();
            sub_function.id = "00";
            sub_function.name = "Enable Rx And Tx";
            seriver.sub_function_list.Add(sub_function);
            //@sub_function_2
            sub_function = new uds_seriver.SubFunction();
            sub_function.id = "80";
            sub_function.name = "Enable Rx And Tx Suppress Pos Res";
            seriver.sub_function_list.Add(sub_function);
            //@sub_function_3
            sub_function = new uds_seriver.SubFunction();
            sub_function.id = "03";
            sub_function.name = "Enable Rx And Tx";
            seriver.sub_function_list.Add(sub_function);
            //@sub_function_4
            sub_function = new uds_seriver.SubFunction();
            sub_function.id = "83";
            sub_function.name = "Enable Rx And Tx Suppress Pos Res";
            seriver.sub_function_list.Add(sub_function);
            serivers_list.Add(seriver);
            #endregion

            #region $2E
            seriver = new uds_seriver();
            seriver.sid = "2E";
            seriver.name = "Write Data By Identifier";
            seriver.sub_function_list = new List<uds_seriver.SubFunction>();
            serivers_list.Add(seriver);
            #endregion

            #region $2F
            seriver = new uds_seriver();
            seriver.sid = "2F";
            seriver.name = "Input Output Control By Identifier ";
            seriver.sub_function_list = new List<uds_seriver.SubFunction>();
            serivers_list.Add(seriver);
            #endregion

            #region $31
            seriver = new uds_seriver();
            seriver.sid = "31";
            seriver.name = "Routine Control";
            seriver.sub_function_list = new List<uds_seriver.SubFunction>();
            seriver.sub_function_list = new List<uds_seriver.SubFunction>();
            //@sub_function_1
            sub_function = new uds_seriver.SubFunction();
            sub_function.id = "01";
            sub_function.name = "Start Routine";
            seriver.sub_function_list.Add(sub_function);
            //@sub_function_2
            sub_function = new uds_seriver.SubFunction();
            sub_function.id = "02";
            sub_function.name = "Stop Routine";
            seriver.sub_function_list.Add(sub_function);
            //@sub_function_2
            sub_function = new uds_seriver.SubFunction();
            sub_function.id = "03";
            sub_function.name = "Request Routine Result";
            seriver.sub_function_list.Add(sub_function);
            serivers_list.Add(seriver);
            #endregion

            #region $3D
            seriver = new uds_seriver();
            seriver.sid = "3D";
            seriver.name = "Write Memory By Address";
            seriver.parameter = "00 00 00 00 00 01 00";
            seriver.sub_function_list = new List<uds_seriver.SubFunction>();
            //@sub_function_1
            sub_function = new uds_seriver.SubFunction();
            sub_function.id = "42";
            sub_function.name = "Address 4 Byte Data 2 Byte";
            seriver.sub_function_list.Add(sub_function);
            serivers_list.Add(seriver);
            #endregion

            #region $3E
            seriver = new uds_seriver();
            seriver.sid = "3E";
            seriver.name = "Tester Present";
            seriver.sub_function_list = new List<uds_seriver.SubFunction>();
            //@sub_function_1
            sub_function = new uds_seriver.SubFunction();
            sub_function.id = "00";
            sub_function.name = "Test Present";
            seriver.sub_function_list.Add(sub_function);
            //@sub_function_2
            sub_function = new uds_seriver.SubFunction();
            sub_function.id = "80";
            sub_function.name = "Test Present Suppress Pos Res";
            seriver.sub_function_list.Add(sub_function);
            serivers_list.Add(seriver);
            #endregion

            #region $85
            seriver = new uds_seriver();
            seriver.sid = "85";
            seriver.name = "Control DTC Setting";
            seriver.sub_function_list = new List<uds_seriver.SubFunction>();
            //@sub_function_1
            sub_function = new uds_seriver.SubFunction();
            sub_function.id = "01";
            sub_function.name = "DTC Logging On";
            seriver.sub_function_list.Add(sub_function);
            //@sub_function_2
            sub_function = new uds_seriver.SubFunction();
            sub_function.id = "81";
            sub_function.name = "DTC Logging On Suppress Pos Res";
            seriver.sub_function_list.Add(sub_function);
            //@sub_function_3
            sub_function = new uds_seriver.SubFunction();
            sub_function.id = "02";
            sub_function.name = "DTC Logging Off";
            seriver.sub_function_list.Add(sub_function);
            //@sub_function_4
            sub_function = new uds_seriver.SubFunction();
            sub_function.id = "82";
            sub_function.name = "DTC Logging Off Suppress Pos Res";
            seriver.sub_function_list.Add(sub_function);
            serivers_list.Add(seriver);
            #endregion

            foreach (uds_seriver ss in serivers_list)
            {
                comboBoxSerivers.Items.Add("$" + ss.sid + " " + ss.name);
            }
            comboBoxSerivers.SelectedIndex = 0;
        }
        private void button_Click(object sender, EventArgs e)
        {
            trans.rx_id = 0x7B8;
            trans.tx_id = 0x7B0;

            trans.CanTrans_TxMsg(textBoxTransData.Text.StringToHex());
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

                trans.CanTrans_TxMsg(textBoxTransData.Text.StringToHex());
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

        private void comboBoxSerivers_SelectedIndexChanged(object sender, EventArgs e)
        {
            now_seriver = serivers_list[comboBoxSerivers.SelectedIndex];
            now_sub_function = new uds_seriver.SubFunction();

            comboBoxSubFunction.Items.Clear();
            foreach (uds_seriver.SubFunction sub in now_seriver.sub_function_list)
            {
                comboBoxSubFunction.Items.Add("$" + sub.id + " "+ sub.name);
                comboBoxSubFunction.SelectedIndex = 0;
            }
            textBoxParameter.Text = now_seriver.parameter;
            updateTransData();
        }

        private void comboBoxSubFunction_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (now_seriver.sub_function_list.Count != 0)
            {
                now_sub_function = now_seriver.sub_function_list[comboBoxSubFunction.SelectedIndex];
            }
            textBoxParameter.Text = now_seriver.parameter;
            updateTransData();
        }

        void updateTransData()
        {
            string strings = now_seriver.sid;
            strings += now_sub_function.id;
            strings += now_sub_function.parameter;
            strings += textBoxParameter.Text;
            textBoxTransData.Text = strings.StringToHex().HexToStrings(" ");
        }

        private void textBoxParameter_TextChanged(object sender, EventArgs e)
        {
            updateTransData();
        }
    }
}
