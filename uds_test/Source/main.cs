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
                        if (checkBoxTesterPresent.Checked)
                        {
                            if (++time_cnt > 4)
                            {
                                time_cnt = 0;
                                driver.WriteData(0x7DF, new byte[] { 0x02, 0x3E, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00 }, 8);
                            }
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
                //UpdateAndTransmit();
            };
            try { Invoke(TextBoxUpdate); } catch { };
            int id;
            int dlc;
            long time;
            byte[] dat = new byte[8];
            while (driver.ReadData(out id, ref dat, out dlc, out time) == true)
            {
                trans.Can_Trans_RxFrams(id, dat, dlc);
            }
            uds_rx_handler();
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
        
        private void AnalyzeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AnalyzeToolStripMenuItem.Checked = !AnalyzeToolStripMenuItem.Checked;
        }

        private void AutoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AutoToolStripMenuItem.Checked = !AutoToolStripMenuItem.Checked;
        }

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

        #region DTC

        public class Dtc
        {
            public string id;
            public string name;

            public Dtc(string Id, string Name)
            {
                id = Id;
                name = Name;
            }
        }

        List<Dtc> dtc_list = new List<Dtc>
        {
            new Dtc("D10087", "Lost_BCAN_Communication"),
            new Dtc("D20000", "BCAN_Bus_Off"),
            new Dtc("E13087", "ICM1_the_message_timeout_error"),
            new Dtc("E13187", "ICM2_the_message_timeout_error"),
            new Dtc("E13287", "ICM_VIN_the_message_timeout_error"),
            new Dtc("E11087", "CLM1_the_message_timeout_error"),
            new Dtc("E14587", "MENUSET1_the_message_timeout_error"),
            new Dtc("E16087", "PLG1_the_message_timeout_error"),
            new Dtc("E09087", "PEPS1_the_message_timeout_error"),
            new Dtc("E02087", "GWB_SDM1_the_message_timeout_error"),
            new Dtc("E00187", "GWB_EMS2_the_message_timeout_error"),
            new Dtc("E00287", "GWB_EMS3_the_message_timeout_error"),
            new Dtc("E00487", "GWB_EMS5_the_message_timeout_error"),
            new Dtc("E01087", "GWB_TCM1_the_message_timeout_error"),
            new Dtc("E04087", "GWB_BRAKE1_the_message_timeout_error"),
            new Dtc("E05087", "GWB_EPB1_the_message_timeout_error"),
            new Dtc("C42387", "Implausible_signal_from_SDM1"),
            new Dtc("C46987", "Implausible_signal_from_EMS5"),
            new Dtc("C45687", "Implausible_signal_from_BRAKE1"),
            new Dtc("D10287", "Lost_LIN_Communication_with_DDM"),
            new Dtc("D10387", "Lost_LIN_Communication_with_PDM"),
            new Dtc("D10487", "Lost_LIN_Communication_with_RLDM"),
            new Dtc("D10587", "Lost_LIN_Communication_with_RRDM"),
            new Dtc("D10787", "Lost_LIN_Communication_with_RS"),
            new Dtc("D10887", "Lost_LIN_Communication_with_SCM"),
            new Dtc("D10987", "Lost_LIN_Communication_with_DCM"),
            new Dtc("910000", "BCM_EEPROM_Error"),
            new Dtc("910100", "VIN_Mismatch_with_other_ECU"),
            new Dtc("910216", "Circuit_voltage_below9"),
            new Dtc("910217", "Circuit_voltage_above16"),
            new Dtc("910311", "Door_Open_Illumination_short_to_Ground"),
            new Dtc("910313", "Door_Open_Illumination_open"),
            new Dtc("910411", "Welcome_Lamps_short_to_Ground"),
            new Dtc("910413", "Welcome_Lamps_open"),
            new Dtc("910511", "Battery_saver_short_to_Ground"),
            new Dtc("910513", "Battery_saver_open"),
            new Dtc("910611", "Power_mirror_heat_short_to_Ground"),
            new Dtc("910613", "Power_mirror_heat_open"),
            new Dtc("910711", "Reverse_Lamps_short_to_Ground"),
            new Dtc("910713", "Reverse_Lamps_open"),
            new Dtc("910811", "Brake_Lamps_short_to_Ground"),
            new Dtc("910813", "Brake_Lamps_open"),
            new Dtc("910911", "CHMSL_short_to_Ground"),
            new Dtc("910913", "CHMSL_open"),
            new Dtc("911011", "Turn_Lamps_L_short_to_Ground"),
            new Dtc("911013", "Turn_Lamps_L_open"),
            new Dtc("911111", "Turn_Lamps_R_short_to_Ground"),
            new Dtc("911113", "Turn_Lamps_R_open"),
            new Dtc("911211", "Low_Beam_L_short_to_Ground"),
            new Dtc("911213", "Low_Beam_L_open"),
            new Dtc("911311", "Low_Beam_R_short_to_Ground"),
            new Dtc("911313", "Low_Beam_R_open"),
            new Dtc("911411", "Assist_HI_BM_Corner_Lamp_L_short_to_Ground"),
            new Dtc("911413", "Assist_HI_BM_Corner_Lamp_L_open"),
            new Dtc("911511", "Assist_HI_BM_Corner_Lamp_R_short_to_Ground"),
            new Dtc("911513", "Assist_HI_BM_Corner_Lamp_R_open"),
            new Dtc("911611", "Front_Fog_Lamp_L_short_to_Ground"),
            new Dtc("911613", "Front_Fog_Lamp_L_open"),
            new Dtc("911711", "Front_Fog_Lamp_R_short_to_Ground"),
            new Dtc("911713", "Front_Fog_Lamp_R_open"),
            new Dtc("911811", "Rear_Fog_Lamps_short_to_Ground"),
            new Dtc("911813", "Rear_Fog_Lamps_open"),
            new Dtc("911911", "Daytime_Running_Lamps_short_to_Ground"),
            new Dtc("911913", "Daytime_Running_Lamps_open"),
            new Dtc("912011", "Key_Locked_Solenoid_short_to_Ground"),
            new Dtc("912013", "Key_Locked_Solenoid_open"),
            new Dtc("912111", "Rear_Defroster_Relay_short_to_Ground"),
            new Dtc("912113", "Rear_Defroster_Relay_open"),
            new Dtc("912211", "Shifter_Lock_Solenoid_short_to_Ground"),
            new Dtc("912213", "Shifter_Lock_Solenoid_open"),
            new Dtc("912413", "Starter_Solenoid_Relay_open"),
            new Dtc("912511", "Position_Lamps_L_short_to_Ground"),
            new Dtc("912513", "Position_Lamps_L_open"),
            new Dtc("912611", "Position_Lamps_R_short_to_Ground"),
            new Dtc("912613", "Position_Lamps_R_open"),
            new Dtc("912711", "License_plate_lamps_short_to_Ground"),
            new Dtc("912713", "License_plate_lamps_open"),
            new Dtc("912811", "IP_Illumination_short_to_Ground"),
            new Dtc("912813", "IP_Illumination_open"),
            new Dtc("912912", "Dome_Lamps_short_to_Battery"),
            new Dtc("913012", "Window_Lift_Enable1_short_to_Battery"),
            new Dtc("913014", "Window_Lift_Enable1_short_to_Groundoropen"),
            new Dtc("913112", "Window_Lift_Enable2_short_to_Battery"),
            new Dtc("913114", "Window_Lift_Enable2_short_to_Groundoropen"),
            new Dtc("913212", "Passenger_Window_Up_short_to_Battery"),
            new Dtc("913214", "Passenger_Window_Up_short_to_Groundopen"),
            new Dtc("913312", "Passenger_Window_Down_short_to_Battery"),
            new Dtc("913314", "Passenger_Window_Down_short_to_Groundopen"),
            new Dtc("913412", "RL_Window_Up_short_to_Battery"),
            new Dtc("913414", "RL_Window_Up_short_to_Groundoropen"),
            new Dtc("913512", "RL_Window_Down_short_to_Battery"),
            new Dtc("913514", "RL_Window_Down_short_to_Groundoropen"),
            new Dtc("913612", "RR_Window_Up_short_to_Battery"),
            new Dtc("913614", "RR_Window_Up_short_to_Groundoropen"),
            new Dtc("913712", "RR_Window_Down_short_to_Battery"),
            new Dtc("913714", "RR_Window_Down_short_to_Groundoropen"),
            new Dtc("913812", "Wiper_Motor_High_short_to_Battery"),
            new Dtc("913912", "Wiper_Motor_Low_short_to_Battery"),
            new Dtc("913914", "Wiper_Motor_Low_short_to_Groundoropen"),
            new Dtc("914012", "Front_Washer_Pump_short_to_Battery"),
            new Dtc("914014", "Front_Washer_Pump_short_to_Groundoropen"),
            new Dtc("914112", "Rear_Washer_Pump_short_to_Battery"),
            new Dtc("914114", "Rear_Washer_Pump_short_to_Groundoropen"),
            new Dtc("914212", "Rear_Wiper_short_to_Battery"),
            new Dtc("914411", "Window_Lock_Indicator_short_to_Ground"),
            new Dtc("914413", "Window_Lock_Indicator_open"),
            new Dtc("914500", "Wiper_Intermittent_SW_Invaild"),
            new Dtc("914600", "WasherorRear_Wiper_SW_Invaild"),
            new Dtc("914700", "Window_SW_Stick"),
            new Dtc("915011", "High_Beam_Solenoid_short_to_Ground"),
            new Dtc("915013", "High_Beam_Solenoid_open"),
            new Dtc("915111", "Atmosphere_Lamp_short_to_Ground"),
            new Dtc("915113", "Atmosphere_Lamp_open"),
            new Dtc("915211", "RearDoor_Lamp_short_to_Ground"),
            new Dtc("915213", "RearDoor_Lamp_open"),
            new Dtc("970000", "Invalid_key_present"),
            new Dtc("970100", "ABIC_or_antenna_Fault"),
            new Dtc("970200", "TP_FaultNo_TP_Responds"),
            new Dtc("970300", "TP_Respond_Authentication_fail"),
            new Dtc("970400", "No_EMS_challenge_Rx"),
            new Dtc("970500", "Invalid_Challenge"),
            new Dtc("900000", "DDM_F_ECU"),
            new Dtc("900100", "DDM_F_DriveSwitch"),
            new Dtc("900500", "DDM_F_Sensor"),
            new Dtc("900600", "DDM_F_Response"),
            new Dtc("900700", "PDM_F_ECU"),
            new Dtc("900800", "PDM_F_PassSwitch"),
            new Dtc("900900", "PDM_F_Sensor"),
            new Dtc("901000", "PDM_F_Response"),
            new Dtc("901100", "RLDM_F_ECU"),
            new Dtc("901200", "RLDM_F_RLSwitch"),
            new Dtc("901300", "RLDM_F_Sensor"),
            new Dtc("901400", "RLDM_F_Response"),
            new Dtc("901500", "RRDM_F_ECU"),
            new Dtc("901600", "RRDM_F_RRSwitch"),
            new Dtc("901700", "RRDM_F_Sensor"),
            new Dtc("901800", "RRDM_F_Response"),
            new Dtc("901900", "SCM_F_LevelSensor"),
            new Dtc("902000", "SCM_F_BackrestSensor"),
            new Dtc("902100", "SCM_F_HeightSeatSensor"),
            new Dtc("902200", "SCM_F_CushionSeatSensor"),
            new Dtc("902400", "SCM_F_R_HeightMirrorSensor"),
            new Dtc("902600", "SCM_F_R_LevelMirrorSensor"),
            new Dtc("902700", "SCM_F_SeatAdjSwitch"),
            new Dtc("902800", "SCM_F_MemorySwitch"),
            new Dtc("902300", "SCM_F_BRCTCOMRelay"),
            new Dtc("902500", "SCM_F_CTLVRelay"),
            new Dtc("902900", "SCM_F_HILVCOMRelay"),
            new Dtc("903200", "SCM_F_Eeprom"),
            new Dtc("903300", "SCM_F_Communication"),
            new Dtc("903600", "RS_F_LightSensor"),
            new Dtc("903700", "RS_F_RainSensor"),
            new Dtc("903800", "RS_F_Volatage"),
            new Dtc("903900", "RS_F_Communication"),
            new Dtc("903000", "DCM_F_LLevelMirrorSensor"),
            new Dtc("903100", "DCM_F_LHeightMirrorSensor"),
            new Dtc("903400", "DCM_F_Response"),
        };

        public partial class DtcFinder
        {
            private string Id = string.Empty;

            public string id
            {
                get { return Id; }
                set { this.Id = value; }
            }

            private string Name = string.Empty;

            public string name
            {
                get { return this.Name; }
                set { this.Name = value; }
            }

            public DtcFinder()
            {

            }

            /// <summary>
            /// 通过ID查找
            /// </summary>
            /// <param name="Dtc"></param>
            /// <returns></returns>
            public bool FindDtcById(Dtc dtc)
            {
                return Id == dtc.id;
            }

            /// <summary>
            /// 通过名称查找
            /// </summary>
            /// <param name="Dtc"></param>
            /// <returns></returns>
            public bool FindDtcByName(Dtc dtc)
            {
                return Name == dtc.name;
            }
        }
        #endregion

        #region Analyze
        private void Analyze()
        {
            string strings = string.Empty;
            if (trans.can_rx_info.buffer[0] == 0x7F)
            {
                switch (trans.can_rx_info.buffer[2])
                {
                    case 0x11:
                        strings = "-->NRC11, Service Not Supported";
                        break;
                    case 0x12:
                        strings = "-->NRC12, Sub Function Not Supported Or Invalid Format";
                        break;
                    case 0x13:
                        strings = "-->NRC13, Incorrect Message Length Or Invalid Format";
                        break;
                    case 0x14:
                        strings = "-->NRC14, Response Too Long";
                        break; 
                    case 0x21:
                        strings = "-->NRC21, Busy Repeat Request";
                        break;
                    case 0x22:
                        strings = "-->NRC22, Conditions Not Correct";
                        break;
                    case 0x24:
                        strings = "-->NRC24, Request Sequence Error";
                        break;
                    case 0x31:
                        strings = "-->NRC31, Request Out Of Range";
                        break;
                    case 0x33:
                        strings = "-->NRC33, Security Access Denied";
                        break;
                    case 0x35:
                        strings = "-->NRC35, Invalid Key";
                        break;
                    case 0x36:
                        strings = "-->NRC36, Seed Number Of Attempts";
                        break;
                    case 0x37:
                        strings = "-->NRC37, Required Time Delay Not Expired";
                        break;
                    case 0x71:
                        strings = "-->NRC37, Transfer Data Suspended";
                        break;
                    case 0x72:
                        strings = "-->NRC72, General Programming Failure";
                        break;
                    case 0x73:
                        strings = "-->NRC72, Wrong Block Sequence Counter";
                        break;
                    case 0x78:
                        strings = "-->NRC78, Request Correctly Received Response Pending";
                        break; 
                    case 0x7E:
                        strings = "-->NRC7E, Sub Function Not Supported In Active Session";
                        break;
                    case 0x7F:
                        strings = "-->NRC7F, Serivce Not Support In Active Session";
                        break; 
                    case 0x87:
                        strings = "-->NRC87, Defect While Writing";
                        break;
                    case 0x92:
                        strings = "-->NRC92, Serivce Not Support Vvoltage Too High";
                        break;
                    case 0x93:
                        strings = "-->NRC93, Serivce Not Support Voltage TooLow";
                        break;
                    default:
                        break;
                }
                strings += "\r\n";
            }
            else if (trans.can_rx_info.buffer[0] == 0x59)
            {
                if (trans.can_rx_info.buffer[1] == 0x01)
                {
                    if(trans.can_rx_info.buffer.Length >= 6)
                    {
                        int dtc_num = (int)trans.can_rx_info.buffer[4] << 8
                                            | (int)trans.can_rx_info.buffer[5];
                        strings += "-->DTC Analyze\r\n";
                        strings += "-->DTC Number:" + dtc_num.ToString() + "\r\n";
                    }
                }
                else if (trans.can_rx_info.buffer[1] == 0x04)
                {
                    if (trans.can_rx_info.buffer.Length >= 7)
                    {
                        byte[] bytes = new byte[5];
                        Array.Copy(trans.can_rx_info.buffer, 2, bytes, 0, 5);
                        strings += "-->DTC Analyze\r\n";
                        strings += "-->DTC Snop Shot:" + bytes.HexToStrings(" ") + "\r\n";
                        bytes = new byte[trans.can_rx_info.buffer.Length - 7];
                        Array.Copy(trans.can_rx_info.buffer, 7, bytes, 0, bytes.Length);
                        strings += "-->DTC Snop Shot:" + bytes.HexToStrings(" ") + "\r\n";
                    }
                }
                else if (trans.can_rx_info.buffer[1] == 0x06)
                {
                    if (trans.can_rx_info.buffer.Length >= 2)
                    {
                        byte[] bytes = new byte[trans.can_rx_info.buffer.Length - 2];
                        Array.Copy(trans.can_rx_info.buffer, 2, bytes, 0, bytes.Length);
                        strings += "-->DTC Analyze\r\n";
                        strings += "-->DTC Extended Record:" + bytes.HexToStrings(" ") + "\r\n";
                    }
                }
                else
                {
                    strings += "-->DTC Analyze\r\n";
                    DtcFinder dtc_finder = new DtcFinder();
                    Dtc dtc = new Dtc("", "");
                    int dtc_num = (trans.can_rx_info.buffer.Length - 3) / 4;
                    int dtc_sts;
                    int index;
                    strings += "-->DTC Number:" + dtc_num .ToString() + "\r\n";
                    for (index = 0; index < dtc_num; index++)
                    {
                        dtc_finder.id = ((int)trans.can_rx_info.buffer[3 + index * 4 + 0] << 16
                                | (int)trans.can_rx_info.buffer[3 + index * 4 + 1] << 8
                                | (int)trans.can_rx_info.buffer[3 + index * 4 + 2]).ToString("X6");
                        dtc_sts = trans.can_rx_info.buffer[3 + index * 4 + 3];
                        dtc = dtc_list.Find(dtc_finder.FindDtcById);
                        if (dtc != null)
                        {
                            strings += "$" + dtc.id + " " + dtc_sts.ToString("X2") + " " + dtc.name + "\r\n";
                        }
                        else
                        {
                            strings += "$" + dtc_finder.id + " " + dtc_sts.ToString("X2") + " " + "\r\n";
                        }
                    }
                }
            }
            if(strings != string.Empty)
            {
                EventHandler TextBoxUpdate = delegate
                {
                    textBoxStream.AppendText(strings);
                };
                try { Invoke(TextBoxUpdate); } catch { };
            }
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
            //@end_sub_function
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
            identifier.name = "All DTC";
            service.identifier_list.Add(identifier);
            foreach (Dtc dtc in dtc_list)
            {
                identifier = new uds_service.Identifier();
                identifier.id = dtc.id;
                identifier.name = dtc.name;
                service.identifier_list.Add(identifier);
            }
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
            sub_function.identifier_enabled = false;
            service.sub_function_list.Add(sub_function);
            //@sub_function_2
            sub_function = new uds_service.SubFunction();
            sub_function.id = "02";
            sub_function.name = "Report DTC By Status Mask";
            sub_function.parameter = "09";
            sub_function.identifier_enabled = false;
            service.sub_function_list.Add(sub_function);
            //@sub_function_3
            sub_function = new uds_service.SubFunction();
            sub_function.id = "04";
            sub_function.name = "Report DTC Snapshot Record By DTC Number";
            sub_function.parameter = "01";
            service.sub_function_list.Add(sub_function);
            //@sub_function_4
            sub_function = new uds_service.SubFunction();
            sub_function.id = "06";
            sub_function.name = "Report DTC Extended Datar Record By DTC Number";
            sub_function.parameter = "01";
            service.sub_function_list.Add(sub_function);
            //@sub_function_5
            sub_function = new uds_service.SubFunction();
            sub_function.id = "0A";
            sub_function.name = "Report Supported DTC";
            sub_function.identifier_enabled = false;
            service.sub_function_list.Add(sub_function);
            //@identifier
            foreach(Dtc dtc in dtc_list)
            {
                identifier = new uds_service.Identifier();
                identifier.id = dtc.id;
                identifier.name = dtc.name;
                service.identifier_list.Add(identifier);
            }
            //@end_identifier
            services_list.Add(service);
            #endregion

            #region $22 Read Data By Identifier
            service = new uds_service();
            service.sid = "22";
            service.name = "Read Data By Identifier";
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
            //@end_sub_function
            //@identifier
            //@end_identifier
            #endregion

            #region $23 Read Memory By Address
            service = new uds_service();
            service.sid = "23";
            service.name = "Read Memory By Address";
            service.sub_function_list = new List<uds_service.SubFunction>();
            //@sub_function
            //end_sub_function
            //@identifier
            identifier = new uds_service.Identifier();
            identifier.id = "42";
            identifier.name = "Address Lenght And Read Memory Lenght";
            identifier.parameter = "00 00 00 00 00 01";
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
            identifier.parameter = "FFFFFFFFFFFFFFFFFFFF";
            service.identifier_list.Add(identifier);
            //@identifier_8
            identifier = new uds_service.Identifier();
            identifier.id = "F199";
            identifier.name = "Programming Date";
            identifier.parameter = "FFFFFFFFFFFF";
            service.identifier_list.Add(identifier);
            services_list.Add(service);
            #endregion

            #region $2F Input Output Control By Identifier
            service = new uds_service();
            service.sid = "2F";
            service.name = "Input Output Control By Identifier";
            service.sub_function_list = new List<uds_service.SubFunction>();
            //@sub_function
            //1
            sub_function = new uds_service.SubFunction();
            sub_function.id = "01";
            sub_function.name = "Return Control To ECU";
            service.sub_function_list.Add(sub_function);
            //2
            sub_function = new uds_service.SubFunction();
            sub_function.id = "02";
            sub_function.name = "Reset To Default";
            service.sub_function_list.Add(sub_function);
            //3
            sub_function = new uds_service.SubFunction();
            sub_function.id = "03";
            sub_function.name = "Freeze Current State";
            service.sub_function_list.Add(sub_function);
            //4
            sub_function = new uds_service.SubFunction();
            sub_function.id = "04";
            sub_function.name = "Short Term Adjustment";
            service.sub_function_list.Add(sub_function);
            //@end_sub_function
            //@identifier
            //1
            identifier = new uds_service.Identifier();
            identifier.id = "D500";
            identifier.name = "Digital Input 1";
            identifier.parameter = "FFFFFFFFFFFFFFFFFFFF";
            service.identifier_list.Add(identifier);
            //2
            identifier = new uds_service.Identifier();
            identifier.id = "D501";
            identifier.name = "Digital Input 2";
            identifier.parameter = "FFFFFFFFFFFFFFFFFFFF";
            service.identifier_list.Add(identifier);
            //3
            identifier = new uds_service.Identifier();
            identifier.id = "D502";
            identifier.name = "Out Put 1";
            identifier.parameter = "FFFFFFFFFFFFFFFFFFFF";
            service.identifier_list.Add(identifier);
            //4
            identifier = new uds_service.Identifier();
            identifier.id = "D503";
            identifier.name = "Out Put 2";
            identifier.parameter = "FFFFFFFFFFFFFFFFFFFF";
            service.identifier_list.Add(identifier);
            //5
            identifier = new uds_service.Identifier();
            identifier.id = "D504";
            identifier.name = "Out Put 3";
            identifier.parameter = "FFFFFFFFFFFFFFFFFFFF";
            service.identifier_list.Add(identifier);
            //@end_identifier
            services_list.Add(service);
            #endregion

            #region $31 Routine Control
            service = new uds_service();
            service.sid = "31";
            service.name = "Routine Control";
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
            service.sub_function_list = new List<uds_service.SubFunction>();
            //@sub_function
            //end_sub_function
            //@identifier
            identifier = new uds_service.Identifier();
            identifier.id = "42";
            identifier.name = "Address Lenght And Read Memory Lenght";
            identifier.parameter = "00 00 00 00 00 01 00";
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
            }
            foreach (uds_service.SubFunction sub in service_27.sub_function_list)
            {
                comboBoxSecurityLevel.Items.Add("$" + sub.id + " " + sub.name);
            }
            comboBoxServices.Text = "诊断服务";
            foreach (uds_service ss in services_list)
            {
                comboBoxServices.Items.Add("$" + ss.sid + " " + ss.name);
                groupBoxServices.Text += " $" + ss.sid;
            }
            comboBoxSession.SelectedIndex = 2;
            comboBoxSecurityLevel.SelectedIndex = 0;
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
                        if (checkBoxTesterPresentShow.Checked == false
                            && TxFarme.dat[0] == 0x02
                            && TxFarme.dat[1] == 0x3E)
                        {
                            return;
                        }
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
                        if (checkBoxTesterPresentShow.Checked == false
                            && TxFarme.dat[0] == 0x02
                            && TxFarme.dat[1] == 0x3E)
                        {
                            return;
                        }
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
                        if (checkBoxTesterPresentShow.Checked == false
                            && RxFarme.dat[0] == 0x02
                            && RxFarme.dat[1] == 0x7E)
                        {
                            return;
                        }
                        textBoxStream.AppendText(RxFarme.ToString() + "\r\n");
                    };
                    try { Invoke(TextBoxUpdate); } catch { };
                }
                );
            #endregion

            uds_seriver_init();
        }

        private void uds_rx_handler()
        {
            if (trans.can_rx_info.rx_msg_rcvd == true)
            {
                trans.can_rx_info.rx_msg_rcvd = false;
                if (trans.can_rx_info.buffer[0] == 0x67)
                {
                    uint seed = 0;
                    uint level;
                    uint result = 0;
                    if (trans.can_rx_info.buffer.Length == 4)
                    {
                        seed = (uint)trans.can_rx_info.buffer[2] << 8
                            | (uint)trans.can_rx_info.buffer[3];
                    }
                    else if (trans.can_rx_info.buffer.Length == 6)
                    {
                        seed = (uint)trans.can_rx_info.buffer[2] << 24
                            | (uint)trans.can_rx_info.buffer[3] << 16
                            | (uint)trans.can_rx_info.buffer[4] << 8
                            | (uint)trans.can_rx_info.buffer[5];
                    }
                    level = trans.can_rx_info.buffer[1];
                    if (seed != 0 && level % 2 != 0)
                    {
                        result = SecurityAccess(0, seed, level);
                        if (trans.can_rx_info.buffer.Length == 4)
                        {
                            result &= 0xFFFF;
                            trans.CanTrans_TxMsg(("27" + (level + 1).ToString("x2") + result.ToString("x4")).StringToHex());
                        }
                        else if (trans.can_rx_info.buffer.Length == 6)
                        {
                            trans.CanTrans_TxMsg(("27" + (level + 1).ToString("x2") + result.ToString("x8")).StringToHex());
                        }
                    }
                }
                if(AnalyzeToolStripMenuItem.Checked)
                {
                    Analyze();
                }
            }
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
            comboBoxIdentifier.Enabled = now_service.sub_function_selectd.identifier_enabled;
            updateTransData();
        }

        private void comboBoxSubFunction_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (now_service.sub_function_list.Count != 0)
            {
                now_service.sub_function_selectd = now_service.sub_function_list[comboBoxSubFunction.SelectedIndex];
                comboBoxIdentifier.Enabled = now_service.sub_function_selectd.identifier_enabled;
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
            if (AutoToolStripMenuItem.Checked)
            {
                textBoxStream.Text = "";
            }
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
                if(AutoToolStripMenuItem.Checked)
                {
                    textBoxStream.Text = "";
                }
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
    }
}
