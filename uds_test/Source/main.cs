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
using System.Runtime.InteropServices;

using User_Control;

using uds_test.Source.Transmit;
using Uds;
using MyFormat;

namespace uds_test
{
    public partial class main : Form
    {
        [DllImport("SecurityAccess.dll", EntryPoint = "SecurityAccess", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        extern static uint SecurityAccess(uint project, uint seed, uint level);


        can_driver driver = new can_driver();
        uds_trans trans = new uds_trans();
        List<uds_service> services_list = new List<Uds.uds_service>();

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
            //textBoxStream.Text = SecurityAccess(0, 0x12345678, 0x12345678).ToString();
            mmTimerInit();
        }

        #region Bus Params Page

        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        int time_cnt = 0;

        private void BusParamsInit()
        {
            driver.SelectChannel(ref comboBoxChannel);
            comboBoxBaud.SelectedIndex = 1;
        }

        private void comboBoxChannel_Click(object sender, EventArgs e)
        {
            driver.SelectChannel(ref comboBoxChannel);
        }

        private void buttonSchedulerSwitch_Click(object sender, EventArgs e)
        {
            if (buttonBusSwitch.Text == "打开")
            {
                if (driver.OpenChannel(ref comboBoxChannel, ref comboBoxBaud) == true)
                {
                    buttonBusSwitch.Text = "关闭";
                    mmTimerStart();
                    comboBoxBaud.Enabled = false;
                    comboBoxChannel.Enabled = false;

                    timer = new System.Windows.Forms.Timer();
                    timer.Interval = 1000;
                    timer.Tick += delegate
                    {
                        int busload = 0;
                        if (driver.BusLoad(ref busload) == true)
                        {
                            progressBarBusLoad.Value = busload;
                        }
                        if (++time_cnt > 4)
                        {
                            time_cnt = 0;
                            driver.WriteData(0x7DF, new byte[] { 0x3E, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, 8);
                        }
                    };
                    timer.Enabled = true;

                    tabControl.SelectedTab = tabPageUDS;
                }
            }
            else
            {
                driver.CloseChannel();
                buttonBusSwitch.Text = "打开";
                mmTimerStop();
                comboBoxBaud.Enabled = true;
                comboBoxChannel.Enabled = true;

                timer.Enabled = false;
                progressBarBusLoad.Value = 0;
            }
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

        private void ClearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textBoxStream.Text = "";
        }

        #endregion

        #region UDS
        uds_service now_service = new uds_service();
        uds_service service_10 = new uds_service();
        uds_service service_27 = new uds_service();

        private void uds_seriver_init()
        {
            services_list = new List<Uds.uds_service>();
            uds_service service = new uds_service();
            uds_service.SubFunction sub_function = new uds_service.SubFunction();
            uds_service.Identifier identifier = new uds_service.Identifier();

            #region $10 Diagnostic Session Control
            service_10 = new uds_service();
            service_10.sid = "10";
            service_10.name = "Diagnostic Session Control";
            service_10.sub_function_list = new List<uds_service.SubFunction>();
            //@sub_function
            //1
            sub_function = new uds_service.SubFunction();
            sub_function.id = "01";
            sub_function.name = "Default Session";
            service_10.sub_function_list.Add(sub_function);
            //2
            sub_function = new uds_service.SubFunction();
            sub_function.id = "02";
            sub_function.name = "Programing Session";
            service_10.sub_function_list.Add(sub_function);
            //3
            sub_function = new uds_service.SubFunction();
            sub_function.id = "03";
            sub_function.name = "Extended Diagnostic Session";
            service_10.sub_function_list.Add(sub_function);
            //@identifier
            //@end_identifier
            #endregion

            #region $11 ECU Reset
            service = new uds_service();
            service.sid = "11";
            service.name = "ECU Reset";
            service.sub_function_list = new List<uds_service.SubFunction>();
            //@parameter
            //@sub_function_1
            sub_function = new uds_service.SubFunction();
            sub_function.id = "01";
            sub_function.name = "Hard Reset";
            service.sub_function_list.Add(sub_function);
            //@sub_function_2
            sub_function = new uds_service.SubFunction();
            sub_function.id = "02";
            sub_function.name = "Key Off On Reset";
            service.sub_function_list.Add(sub_function);
            //@sub_function_3
            sub_function = new uds_service.SubFunction();
            sub_function.id = "03";
            sub_function.name = "Soft Reset";
            service.sub_function_list.Add(sub_function);
            //@sub_function_4
            sub_function = new uds_service.SubFunction();
            sub_function.id = "04";
            sub_function.name = "Enable Rapid Power Shut Down";
            service.sub_function_list.Add(sub_function);
            //@sub_function_5
            sub_function = new uds_service.SubFunction();
            sub_function.id = "05";
            sub_function.name = "Disable Rapid Power Shut Down";
            service.sub_function_list.Add(sub_function);
            services_list.Add(service);
            #endregion

            #region $14 Clear Diagnostic Information
            service = new uds_service();
            service.sid = "14";
            service.name = "Clear Diagnostic Information";
            service.sub_function_list = new List<uds_service.SubFunction>();
            //@sub_function
            //end_sub_function
            //@identifier
            identifier = new uds_service.Identifier();
            identifier.id = "FFFFFF";
            identifier.name = "Clear All DTC";
            service.identifier_list.Add(identifier);
            //@end_identifier
            services_list.Add(service);
            #endregion

            #region $19 Read DTC Information
            service = new uds_service();
            service.sid = "19";
            service.name = "Read DTC Information";
            service.sub_function_list = new List<uds_service.SubFunction>();
            //@sub_function_1
            sub_function = new uds_service.SubFunction();
            sub_function.id = "01";
            sub_function.name = "Report Number Of DTC By Status Mask";
            sub_function.parameter = "FF";
            service.sub_function_list.Add(sub_function);
            //@sub_function_2
            sub_function = new uds_service.SubFunction();
            sub_function.id = "02";
            sub_function.name = "Report DTC By Status Mask";
            sub_function.parameter = "FF";
            service.sub_function_list.Add(sub_function);
            //@sub_function_3
            sub_function = new uds_service.SubFunction();
            sub_function.id = "04";
            sub_function.name = "Report DTC Snapshot Record By DTC Number";
            sub_function.parameter = "FF FF FF";
            service.sub_function_list.Add(sub_function);
            //@sub_function_4
            sub_function = new uds_service.SubFunction();
            sub_function.id = "06";
            sub_function.name = "Report DTC Extended Datar Record By DTC Number";
            sub_function.parameter = "FF FF FF FF";
            service.sub_function_list.Add(sub_function);
            //@sub_function_5
            sub_function = new uds_service.SubFunction();
            sub_function.id = "0A";
            sub_function.name = "Report Supported DTC";
            service.sub_function_list.Add(sub_function);
            services_list.Add(service);
            #endregion

            #region $22 Read Data By Identifier
            service = new uds_service();
            service.sid = "22";
            service.name = "Read Data By Identifier";
            service.parameter = "";
            service.sub_function_list = new List<uds_service.SubFunction>();
            //@sub_function
            //end_sub_function
            //@identifier
            identifier = new uds_service.Identifier();
            identifier.id = "F183";
            identifier.name = "ECU Bootloader Software Number";
            service.identifier_list.Add(identifier);
            identifier = new uds_service.Identifier();
            identifier.id = "F184";
            identifier.name = "ECU Application Software Number";
            service.identifier_list.Add(identifier);
            identifier = new uds_service.Identifier();
            identifier.id = "F18E";
            identifier.name = "ECU Assembly Number(Part Number)";
            service.identifier_list.Add(identifier);
            identifier = new uds_service.Identifier();
            identifier.id = "F18A";
            identifier.name = "System Supplier Identifier";
            service.identifier_list.Add(identifier);
            identifier = new uds_service.Identifier();
            identifier.id = "F190";
            identifier.name = "Vin";
            service.identifier_list.Add(identifier);
            identifier = new uds_service.Identifier();
            identifier.id = "F191";
            identifier.name = "ECU Hardware Number";
            service.identifier_list.Add(identifier);
            identifier = new uds_service.Identifier();
            identifier.id = "F1A0";
            identifier.name = "Vehicle Network Number";
            service.identifier_list.Add(identifier);
            identifier = new uds_service.Identifier();
            identifier.id = "F198";
            identifier.name = "Repair Shop Code/Tester Serial Number";
            service.identifier_list.Add(identifier);
            identifier = new uds_service.Identifier();
            identifier.id = "F199";
            identifier.name = "Programming Date";
            service.identifier_list.Add(identifier);
            identifier = new uds_service.Identifier();
            identifier.id = "F284";
            identifier.name = "ATECH Application Software Number";
            service.identifier_list.Add(identifier);
            //@end_identifier
            services_list.Add(service);
            #endregion

            #region $27 Security Access
            service_27 = new uds_service();
            service_27.sid = "27";
            service_27.name = "Security Access";
            service_27.sub_function_list = new List<uds_service.SubFunction>();
            //@sub_function
            //1
            sub_function = new uds_service.SubFunction();
            sub_function.id = "01";
            sub_function.name = "Extended";
            service_27.sub_function_list.Add(sub_function);
            //2
            sub_function = new uds_service.SubFunction();
            sub_function.id = "03";
            sub_function.name = "Reprogramming";
            service_27.sub_function_list.Add(sub_function);
            //3
            sub_function = new uds_service.SubFunction();
            sub_function.id = "05";
            sub_function.name = "Immobiliser";
            service_27.sub_function_list.Add(sub_function);
            //4
            sub_function = new uds_service.SubFunction();
            sub_function.id = "07";
            sub_function.name = "Development";
            service_27.sub_function_list.Add(sub_function);
            //@identifier
            //@end_identifier
            #endregion

            #region $23 Read Memory By Address
            service = new uds_service();
            service.sid = "23";
            service.name = "Read Memory By Address";
            service.parameter = "00 00 00 00 00 01";
            service.sub_function_list = new List<uds_service.SubFunction>();
            //@sub_function
            //end_sub_function
            //@identifier
            identifier = new uds_service.Identifier();
            identifier.id = "42";
            identifier.name = "Address Lenght And Read Memory Lenght";
            service.identifier_list.Add(identifier);
            //@end_identifier
            services_list.Add(service);
            #endregion

            #region $28 Communication Control
            service = new uds_service();
            service.sid = "28";
            service.name = "Communication Control";
            service.sub_function_list = new List<uds_service.SubFunction>();
            //@sub_function
            sub_function = new uds_service.SubFunction();
            sub_function.id = "00";
            sub_function.name = "Enable Rx And Tx";
            service.sub_function_list.Add(sub_function);
            sub_function = new uds_service.SubFunction();
            sub_function.id = "80";
            sub_function.name = "Enable Rx And Tx Suppress Pos Res";
            service.sub_function_list.Add(sub_function);
            sub_function = new uds_service.SubFunction();
            sub_function.id = "03";
            sub_function.name = "Disable Rx And Tx";
            service.sub_function_list.Add(sub_function);
            sub_function = new uds_service.SubFunction();
            sub_function.id = "83";
            sub_function.name = "Rx And Tx Suppress Pos Res";
            service.sub_function_list.Add(sub_function);
            //end_sub_function
            //@identifier
            identifier = new uds_service.Identifier();
            identifier.id = "01";
            identifier.name = "Normal Communication Messages";
            service.identifier_list.Add(identifier);
            identifier = new uds_service.Identifier();
            identifier.id = "02";
            identifier.name = "Network Management Communication Messages";
            service.identifier_list.Add(identifier);
            identifier = new uds_service.Identifier();
            identifier.id = "03";
            identifier.name = "Normal and Network Management Communication Messages";
            service.identifier_list.Add(identifier);
            //@end_identifier
            services_list.Add(service);
            #endregion

            #region $2E "Write Data By Identifier
            service = new uds_service();
            service.sid = "2E";
            service.name = "Write Data By Identifier";
            service.sub_function_list = new List<uds_service.SubFunction>();
            //@identifier_7
            identifier = new uds_service.Identifier();
            identifier.id = "F198";
            identifier.name = "Repair Shop Code/Tester Serial Number";
            service.identifier_list.Add(identifier);
            //@identifier_8
            identifier = new uds_service.Identifier();
            identifier.id = "F199";
            identifier.name = "Programming Date";
            service.identifier_list.Add(identifier);
            services_list.Add(service);
            #endregion

            #region $2F Input Output Control By Identifier
            service = new uds_service();
            service.sid = "2F";
            service.name = "Input Output Control By Identifier";
            service.sub_function_list = new List<uds_service.SubFunction>();
            services_list.Add(service);
            #endregion

            #region $31 Routine Control
            service = new uds_service();
            service.sid = "31";
            service.name = "Routine Control";
            service.sub_function_list = new List<uds_service.SubFunction>();
            service.sub_function_list = new List<uds_service.SubFunction>();
            //@sub_function
            sub_function = new uds_service.SubFunction();
            sub_function.id = "01";
            sub_function.name = "Start Routine";
            service.sub_function_list.Add(sub_function);
            sub_function = new uds_service.SubFunction();
            sub_function.id = "02";
            sub_function.name = "Stop Routine";
            service.sub_function_list.Add(sub_function);
            sub_function = new uds_service.SubFunction();
            sub_function.id = "03";
            sub_function.name = "Request Routine Result";
            service.sub_function_list.Add(sub_function);
            //end_sub_function
            //@identifier
            //1
            identifier = new uds_service.Identifier();
            identifier.id = "7501";
            identifier.name = "Generate Random Secret Key";
            identifier.parameter = "AABBCCDD";
            service.identifier_list.Add(identifier);
            //2
            identifier = new uds_service.Identifier();
            identifier.id = "7502";
            identifier.name = "Lock ECU";
            identifier.parameter = "AABBCCDD";
            service.identifier_list.Add(identifier);
            //3
            identifier = new uds_service.Identifier();
            identifier.id = "7503";
            identifier.name = "Add Key";
            identifier.parameter = "AABBCCDD";
            service.identifier_list.Add(identifier);
            //4
            identifier = new uds_service.Identifier();
            identifier.id = "7504";
            identifier.name = "Delete Key";
            identifier.parameter = "AABBCCDD";
            service.identifier_list.Add(identifier);
            //5
            identifier = new uds_service.Identifier();
            identifier.id = "7505";
            identifier.name = "Learn Secret Key from EMS";
            identifier.parameter = "AABBCCDD";
            service.identifier_list.Add(identifier);
            //6
            identifier = new uds_service.Identifier();
            identifier.id = "7506";
            identifier.name = "Teach Secret key to EMS";
            identifier.parameter = "AABBCCDD";
            service.identifier_list.Add(identifier);
            //7
            identifier = new uds_service.Identifier();
            identifier.id = "7507";
            identifier.name = "Key Test";
            identifier.parameter = "AABBCCDD";
            service.identifier_list.Add(identifier);
            //8
            identifier = new uds_service.Identifier();
            identifier.id = "7508";
            identifier.name = "DD Windows Position";
            identifier.parameter = "64000000";
            service.identifier_list.Add(identifier);
            //9
            identifier = new uds_service.Identifier();
            identifier.id = "7509";
            identifier.name = "PD Windows Position";
            identifier.parameter = "64000000";
            service.identifier_list.Add(identifier);
            //10
            identifier = new uds_service.Identifier();
            identifier.id = "750A";
            identifier.name = "RLD Windows Position";
            identifier.parameter = "64000000";
            service.identifier_list.Add(identifier);
            //11
            identifier = new uds_service.Identifier();
            identifier.id = "750B";
            identifier.name = "RRD Windows Position";
            identifier.parameter = "64000000";
            service.identifier_list.Add(identifier);
            //12
            identifier = new uds_service.Identifier();
            identifier.id = "750C";
            identifier.name = "HornCtr";
            identifier.parameter = "64000000";
            service.identifier_list.Add(identifier);
            //13
            identifier = new uds_service.Identifier();
            identifier.id = "750D";
            identifier.name = "RRD Windows Position";
            identifier.parameter = "64646464";
            service.identifier_list.Add(identifier);
            //14
            identifier = new uds_service.Identifier();
            identifier.id = "750E";
            identifier.name = "ALL Door Lock";
            service.identifier_list.Add(identifier);
            //15
            identifier = new uds_service.Identifier();
            identifier.id = "750F";
            identifier.name = "ALL Door Unlock";
            service.identifier_list.Add(identifier);
            //16
            identifier = new uds_service.Identifier();
            identifier.id = "7500";
            identifier.name = "Driver Door Unlock";
            service.identifier_list.Add(identifier);
            //17
            identifier = new uds_service.Identifier();
            identifier.id = "7500";
            identifier.name = "Hazard Control";
            identifier.parameter = "01";
            service.identifier_list.Add(identifier);
            //18
            identifier = new uds_service.Identifier();
            identifier.id = "7551";
            identifier.name = "Find My Car";
            identifier.parameter = "F0";
            service.identifier_list.Add(identifier);
            //19
            identifier = new uds_service.Identifier();
            identifier.id = "FF01";
            identifier.name = "Check Programming Dependecies";
            service.identifier_list.Add(identifier);
            //20
            identifier = new uds_service.Identifier();
            identifier.id = "FFF7";
            identifier.name = "Immo Unlock";
            service.identifier_list.Add(identifier);
            //21
            identifier = new uds_service.Identifier();
            identifier.id = "FFF8";
            identifier.name = "Test ABIC Phase";
            service.identifier_list.Add(identifier);
            //22
            identifier = new uds_service.Identifier();
            identifier.id = "FFF9";
            identifier.name = "DEBUG Mode";
            service.identifier_list.Add(identifier);
            //23
            identifier = new uds_service.Identifier();
            identifier.id = "FDF0";
            identifier.name = "Turn Calibration";
            service.identifier_list.Add(identifier);
            //24
            identifier = new uds_service.Identifier();
            identifier.id = "FFFA";
            identifier.name = "Valid All Key";
            identifier.parameter = "FFFF";
            service.identifier_list.Add(identifier);

            //@end_identifier
            services_list.Add(service);
            #endregion

            #region $3D Write Memory By Address
            service = new uds_service();
            service.sid = "3D";
            service.name = "Write Memory By Address";
            service.parameter = "00 00 00 00 00 01 00";
            service.sub_function_list = new List<uds_service.SubFunction>();
            //@sub_function
            //end_sub_function
            //@identifier
            identifier = new uds_service.Identifier();
            identifier.id = "42";
            identifier.name = "Address Lenght And Read Memory Lenght";
            service.identifier_list.Add(identifier);
            //@end_identifier
            services_list.Add(service);
            #endregion

            #region $3E Tester Present
            service = new uds_service();
            service.sid = "3E";
            service.name = "Tester Present";
            service.sub_function_list = new List<uds_service.SubFunction>();
            //@sub_function_1
            sub_function = new uds_service.SubFunction();
            sub_function.id = "00";
            sub_function.name = "Test Present";
            service.sub_function_list.Add(sub_function);
            //@sub_function_2
            sub_function = new uds_service.SubFunction();
            sub_function.id = "80";
            sub_function.name = "Test Present Suppress Pos Res";
            service.sub_function_list.Add(sub_function);
            services_list.Add(service);
            #endregion

            #region $85 Control DTC Setting
            service = new uds_service();
            service.sid = "85";
            service.name = "Control DTC Setting";
            service.sub_function_list = new List<uds_service.SubFunction>();
            //@sub_function_1
            sub_function = new uds_service.SubFunction();
            sub_function.id = "01";
            sub_function.name = "DTC Logging On";
            service.sub_function_list.Add(sub_function);
            //@sub_function_2
            sub_function = new uds_service.SubFunction();
            sub_function.id = "81";
            sub_function.name = "DTC Logging On Suppress Pos Res";
            service.sub_function_list.Add(sub_function);
            //@sub_function_3
            sub_function = new uds_service.SubFunction();
            sub_function.id = "02";
            sub_function.name = "DTC Logging Off";
            service.sub_function_list.Add(sub_function);
            //@sub_function_4
            sub_function = new uds_service.SubFunction();
            sub_function.id = "82";
            sub_function.name = "DTC Logging Off Suppress Pos Res";
            service.sub_function_list.Add(sub_function);
            services_list.Add(service);
            #endregion

            foreach (uds_service.SubFunction sub in service_10.sub_function_list)
            {
                comboBoxSession.Items.Add("$" + sub.id + " " + sub.name);
                comboBoxSession.SelectedIndex = 0;
            }
            foreach (uds_service.SubFunction sub in service_27.sub_function_list)
            {
                comboBoxSecurityLevel.Items.Add("$" + sub.id + " " + sub.name);
                comboBoxSecurityLevel.SelectedIndex = 0;
            }
            comboBoxServices.Text = "诊断服务";
            foreach (uds_service ss in services_list)
            {
                comboBoxServices.Items.Add("$" + ss.sid + " " + ss.name);
                groupBoxServices.Text += " $" + ss.sid;

            }
            comboBoxServices.SelectedIndex = 0;
        }

        private void uds_init()
        {
            driver = new can_driver();
            trans = new uds_trans();

            trans.tx_id = 0x7B0;
            trans.rx_id = 0x7B8;

            #region Trans Event
            /*使用事件委托传参*/
            driver.EventWriteData += new EventHandler(
                (sender1, e1) =>
                {
                    can_driver.WriteDataEventArgs TxFarme = (can_driver.WriteDataEventArgs)e1;
                    EventHandler TextBoxUpdate = delegate
                    {
                        textBoxStream.AppendText(TxFarme.ToString() + "\r\n");
                    };
                    try { Invoke(TextBoxUpdate); } catch { };
                }
                );
            trans.EventTxFarms += new EventHandler(
                (sender1, e1) =>
                {
                    uds_trans.FarmsEventArgs TxFarme = (uds_trans.FarmsEventArgs)e1;
                    EventHandler TextBoxUpdate = delegate
                    {
                        textBoxStream.AppendText(TxFarme.ToString() + "\r\n");
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
                        textBoxStream.AppendText(RxFarme.ToString() + "\r\n");
                    };
                    try { Invoke(TextBoxUpdate); } catch { };
                }
                );
            #endregion

            uds_seriver_init();
        }

        private void comboBoxSerivers_SelectedIndexChanged(object sender, EventArgs e)
        {
            now_service = services_list[comboBoxServices.SelectedIndex];
            now_service.sub_function_selectd = new uds_service.SubFunction();
            now_service.identifier_selected = new uds_service.Identifier();

            comboBoxSubFunction.Items.Clear();
            comboBoxIdentifier.Items.Clear();
            foreach (uds_service.SubFunction sub in now_service.sub_function_list)
            {
                comboBoxSubFunction.Items.Add("$" + sub.id + " " + sub.name);
                comboBoxSubFunction.SelectedIndex = 0;
            }
            foreach (uds_service.Identifier ident in now_service.identifier_list)
            {
                comboBoxIdentifier.Items.Add("$" + ident.id + " " + ident.name);
                comboBoxIdentifier.SelectedIndex = 0;
            }
            updateTransData();
        }

        private void comboBoxSubFunction_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (now_service.sub_function_list.Count != 0)
            {
                now_service.sub_function_selectd = now_service.sub_function_list[comboBoxSubFunction.SelectedIndex];
            }
            updateTransData();
        }

        private void comboBoxIdentifier_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (now_service.identifier_list.Count != 0)
            {
                now_service.identifier_selected = now_service.identifier_list[comboBoxIdentifier.SelectedIndex];
            }
            updateTransData();
        }

        void updateTransData()
        {
            textBoxTransData.Text = now_service.ToString();
        }

        private void textBoxParameter_TextChanged(object sender, EventArgs e)
        {
            updateTransData();
        }

        private void button_Click(object sender, EventArgs e)
        {
            trans.CanTrans_TxMsg(textBoxTransData.Text.StringToHex());
        }

        private bool delete_char_flag = false;
        private void trans_data_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox textbox = (TextBox)sender;
            string strings = textbox.Text;
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
                delete_char_flag = true;
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
            if (delete_char_flag == true)
            {
                delete_char_flag = false;
                if (strings.Length != 0 && strings[strings.Length - 1] != ' ')
                {
                    return;
                }
            }
            textbox.Text = strings.Replace(" ", "").InsertSpace(2);
            textbox.SelectionStart = textbox.Text.Length;
        }

        private void buttonSession_Click(object sender, EventArgs e)
        {
            trans.CanTrans_TxMsg(service_10.ToString().StringToHex());
        }

        private void comboBoxSession_SelectIndexChanged(object sender, EventArgs e)
        {
            service_10.sub_function_selectd = service_10.sub_function_list[comboBoxSession.SelectedIndex];
        }

        private void buttonSecurityAccess_Click(object sender, EventArgs e)
        {
            trans.CanTrans_TxMsg(service_27.ToString().StringToHex());
        }

        private void comboBoxSecurityLevel_SelectedIndexChanged(object sender, EventArgs e)
        {
            service_27.sub_function_selectd = service_27.sub_function_list[comboBoxSecurityLevel.SelectedIndex];
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

        private void main_FormResized(object sender, EventArgs e)
        {
            Text = "uds_test" + " Width:" + Size.Width.ToString("d3") + " Height:" + Size.Height.ToString("d3");
        }
    }
}
