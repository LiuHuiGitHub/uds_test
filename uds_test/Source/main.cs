﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
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

        uds_trans.AddressingModes mode = uds_trans.AddressingModes.Physical_Addressing;

        can_driver driver = new can_driver();
        uds_trans trans = new uds_trans();
        List<uds_service> services_list = new List<Uds.uds_service>();

        public main()
        {
            InitializeComponent();

            SetStyle(
                     ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.ResizeRedraw
                     | ControlStyles.Selectable
                     | ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.UserPaint
                     | ControlStyles.SupportsTransparentBackColor,
                     true);
            BusParamsInit();
            uds_init();

            textBoxTransData.KeyPress += textBoxTransData_KeyPress;

            size = this.Size;
            groupBox4Location = groupBoxSeriverRoutine.Location;
        }

        Size size = new Size();
        Point groupBox4Location = new Point();
        private void main_Resize(object sender, EventArgs e)
        {
            Point point = new Point();
            point.X = groupBox4Location.X;
            point.Y = groupBox4Location.Y + this.Size.Height - size.Height;
            groupBoxSeriverRoutine.Location = point;
            this.Text = this.Size.Height.ToString() + " " + this.Size.Width.ToString();
        }

        #region Bus Params Page

        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();

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
                    trans.Start();
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
                    };
                    timer.Enabled = true;
                    trans.testerPresentCheckd = checkBoxTesterPresent.Checked;

                    tabControl.SelectedTab = tabPageDiagnosis;
                }
            }
            else
            {
                driver.CloseChannel();
                buttonBusSwitch.Text = "打开";
                trans.Stop();
                comboBoxBaud.Enabled = true;
                comboBoxChannel.Enabled = true;
                trans.testerPresentCheckd = false;

                timer.Enabled = false;
                progressBarBusLoad.Value = 0;
            }
        }

        private void main_FormClosing(object sender, FormClosingEventArgs e)
        {
            trans.Stop();
        }

        private void checkBoxTesterPresent_Click(object sender, EventArgs e)
        {
            trans.testerPresentCheckd = checkBoxTesterPresent.Checked;
        }
        #endregion

        #region File

        readonly string IdentifierProject = "$Project";
        readonly string IdentifierCmd = "$Cmd";
        readonly string IdentifierTxId = "$Tx Id";
        readonly string IdentifierRxId = "$Rx Id";
        readonly string IdentifierDtc = "$19 Dtc";
        readonly string IdentifierRead = "$22 Read";
        readonly string IdentifierWrite = "$22 Write";
        readonly string IdentifierIoCtrl = "$2F IoCtrl";
        readonly string IdentifierRoutine = "$31 Routine";

        private void FileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem t = (ToolStripMenuItem)sender;
            if (t == OpenToolStripMenuItem)
            {
                StreamReader myStream;
                openFileDialog.Title = "打开文件";
                openFileDialog.Filter = "ini文件 (*.ini)|*.ini|map文件 (*.map)|*.map";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    if(openFileDialog.FilterIndex == 1)
                    {
                        myStream = new StreamReader(openFileDialog.FileName);
                        string line;
                        int lineNumber = 0;
                        cmd_list = new List<Cmd>();
                        comboBoxCmd.Items.Clear();
                        dtc_list = new List<Dtc>();
                        service_14.identifier_list = new List<uds_service.Identifier>();
                        service_19.identifier_list = new List<uds_service.Identifier>();
                        service_22.identifier_list = new List<uds_service.Identifier>();
                        service_2E.identifier_list = new List<uds_service.Identifier>();
                        service_2F.identifier_list = new List<uds_service.Identifier>();
                        routine_list = new List<Routine>();
                        comboBoxRoutineIdent.Items.Clear();

                        while ((line = myStream.ReadLine()) != null)
                        {
                            try
                            {
                                string[] format = line.Split(new char[] { ':' });
                                if (format[0] == IdentifierProject)
                                {
                                    this.Text = format[1];
                                }
                                else if (format[0] == IdentifierTxId)
                                {
                                    trans.tx_id = int.Parse(format[1].Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);
                                }
                                else if (format[0] == IdentifierRxId)
                                {
                                    trans.rx_id = int.Parse(format[1].Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);
                                }
                                else if (format[0] == IdentifierCmd)
                                {
                                    string[] s = format[1].Split(new char[] { ',' });
                                    Cmd cmd = new Cmd();
                                    cmd.cmd = s[0];
                                    cmd.name = s[1];
                                    cmd_list.Add(cmd);
                                    comboBoxCmd.Items.Add(cmd.name);
                                    comboBoxCmd.SelectedIndex = 0;
                                }
                                else if (format[0] == IdentifierDtc)
                                {
                                    string[] s = format[1].Split(new char[] { ',' });
                                    uds_service.Identifier identifier = new uds_service.Identifier();
                                    identifier.id = s[0];
                                    identifier.name = s[1];
                                    Dtc dtc = new Dtc(identifier.id, identifier.name);
                                    dtc_list.Add(dtc);
                                    service_14.identifier_list.Add(identifier);
                                    service_19.identifier_list.Add(identifier);
                                }
                                else if (format[0] == IdentifierRead)
                                {
                                    string[] s = format[1].Split(new char[] { ',' });
                                    uds_service.Identifier identifier = new uds_service.Identifier();
                                    identifier.id = s[0];
                                    identifier.name = s[1];
                                    service_22.identifier_list.Add(identifier);
                                }
                                else if (format[0] == IdentifierWrite)
                                {
                                    string[] s = format[1].Split(new char[] { ',' });
                                    uds_service.Identifier identifier = new uds_service.Identifier();
                                    identifier.id = s[0];
                                    identifier.name = s[2];
                                    identifier.parameter = s[1];
                                    service_2E.identifier_list.Add(identifier);
                                }
                                else if (format[0] == IdentifierIoCtrl)
                                {
                                    string[] s = format[1].Split(new char[] { ',' });
                                    uds_service.Identifier identifier = new uds_service.Identifier();
                                    identifier.id = s[0];
                                    identifier.name = s[2];
                                    identifier.parameter = s[1];
                                    service_2F.identifier_list.Add(identifier);
                                }
                                else if (format[0] == IdentifierRoutine)
                                {
                                    string[] s = format[1].Split(new char[] { ',' });
                                    Routine routine = new Routine();
                                    routine.id = s[0];
                                    routine.start = s[1];
                                    routine.stop = s[2];
                                    routine.result = s[3];
                                    routine.name = s[4];
                                    routine_list.Add(routine);
                                    comboBoxRoutineIdent.Items.Add("$" + routine.id + " " + routine.name);
                                    comboBoxRoutineIdent.SelectedIndex = 0;
                                }
                            }
                            catch
                            {
                                DialogResult result = MessageBox.Show("第" + lineNumber.ToString() + "行格式有误:\r\n" + line + "\r\n是否继续？"
                                                        , "配置文件错误"
                                                        , MessageBoxButtons.YesNoCancel
                                                        , MessageBoxIcon.Error);
                                if(result != DialogResult.Yes)
                                {
                                    break;
                                }
                            }
                            lineNumber++;
                        }

                        myStream.Close();
                    }
                    else if(openFileDialog.FilterIndex == 2)
                    {
                        if (OpenMapFileForSpc(openFileDialog.FileName) == true)
                        {
                            ReadWriteVariableInit(mapList);
                        }
                    }
                }
            }
            else if (t == SaveToolStripMenuItem)
            {
                StreamWriter myStream;
                saveFileDialog.Title = "保存设置文件";
                saveFileDialog.FileName = "uds_config";
                saveFileDialog.Filter = "ini文件 (*.ini)|*.ini";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    myStream = new StreamWriter(saveFileDialog.FileName);
                    string save = string.Empty;
                    save += "///配置说明：\r\n";
                    save += "///每行以$xx yyy:起始的为有效行，$xx yyy:请勿修改\r\n";
                    save += "///每行$xx yyy:之后的为该服务的数据，数据中以','分开\r\n";
                    save += "///每行以///起始的为注释行，说明之后的数据存放格式，请勿删除\r\n\r\n";

                    save += IdentifierProject + ":" + this.Text + "\r\n";
                    save += "\r\n";

                    save += IdentifierTxId + ":" + trans.tx_id.ToString("X3") + "\r\n";
                    save += IdentifierRxId + ":" + trans.rx_id.ToString("X3") + "\r\n";
                    save += "\r\n";

                    save += "///" + IdentifierCmd + ": + cmd + ',' + Name\r\n";
                    foreach (Cmd cmd in cmd_list)
                    {
                        save += IdentifierCmd + ":" + cmd.cmd + "," + cmd.name + "\r\n";
                    }
                    save += "\r\n";

                    save += "///" + IdentifierDtc + ": + Ident + ',' + Name\r\n";
                    foreach (Dtc dtc in dtc_list)
                    {
                        save += IdentifierDtc + ":" + dtc.id + "," + dtc.name + "\r\n";
                    }
                    save += "\r\n";

                    save += "///" + IdentifierRead + ": + Ident + ',' + Name\r\n";
                    foreach (uds_service.Identifier id in service_22.identifier_list)
                    {
                        save += IdentifierRead + ":" + id.id + "," + id.name + "\r\n";
                    }
                    save += "\r\n";


                    save += "///" + IdentifierWrite + ": + Ident + ',' + Data + ',' Name\r\n";
                    foreach (uds_service.Identifier id in service_2E.identifier_list)
                    {
                        save += IdentifierWrite + ":" + id.id + "," + id.parameter + "," + id.name + "\r\n";
                    }
                    save += "\r\n";

                    save += "///" + IdentifierIoCtrl + ": + Ident + ',' + Data + ',' Name\r\n";
                    foreach (uds_service.Identifier id in service_2F.identifier_list)
                    {
                        save += IdentifierIoCtrl + ":" + id.id + "," + id.parameter + "," + id.name + "\r\n";
                    }
                    save += "\r\n";

                    save += "///" + IdentifierRoutine + ": + Ident + ',' + StartParameter + ',' + StopParameter + ',' + ResultParameter + ',' Name\r\n";
                    foreach (Routine r in routine_list)
                    {
                        save += IdentifierRoutine + ":" + r.id + "," + r.start + "," + r.stop + "," + r.result + "," + r.name + "\r\n";
                    }
                    save += "\r\n";

                    myStream.Write(save);
                    myStream.Close();
                }
            }
            else if (t == ExitToolStripMenuItem)
            {
                this.Close();
            }
        }
        #endregion

        #region Setting

        private void checkBoxPhysicalAddressing_Click(object sender, EventArgs e)
        {
            if (checkBoxPhysicalAddressing.Checked)
            {
                mode = uds_trans.AddressingModes.Physical_Addressing;
            }
            else
            {
                mode = uds_trans.AddressingModes.Functional_Addressing;
            }
        }

        private void toolStripMenuItemSetting_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem s = (ToolStripMenuItem)sender;
            s.Checked = !s.Checked;
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
            if (textBoxStream.SelectionLength != 0)
            {
                Clipboard.SetDataObject(textBoxStream.SelectedText);
            }
            else
            {
                Clipboard.SetDataObject(textBoxStream.Text);
            }
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

        private void NrcAnalyze(byte[] dat)
        {
            string strings = string.Empty;
            if (dat[0] == 0x7F)
            {
                switch (dat[2])
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
            if (strings != string.Empty)
            {
                EventHandler TextBoxUpdate = delegate
                {
                    textBoxStream.AppendText(strings);
                };
                try { Invoke(TextBoxUpdate); } catch { };
            }
        }

        private void DtcAnalyze(byte[] dat)
        {
            string strings = string.Empty;
            if (DTCCodeAnalyzeToolStripMenuItem.Checked && dat[0] == 0x59)
            {
                if (dat[1] == 0x01)
                {
                    if (dat.Length >= 6)
                    {
                        int dtc_num = (int)dat[4] << 8
                                            | (int)dat[5];
                        strings += "-->DTC Analyze\r\n";
                        strings += "-->DTC Number:" + dtc_num.ToString() + "\r\n";
                    }
                }
                else if (dat[1] == 0x04)
                {
                    if (dat.Length >= 7)
                    {
                        byte[] bytes = new byte[5];
                        Array.Copy(dat, 2, bytes, 0, 5);
                        strings += "-->DTC Analyze\r\n";
                        strings += "-->DTC Snop Shot:" + bytes.HexToStrings(" ") + "\r\n";
                        bytes = new byte[dat.Length - 7];
                        Array.Copy(dat, 7, bytes, 0, bytes.Length);
                        strings += "-->DTC Snop Shot:" + bytes.HexToStrings(" ") + "\r\n";
                    }
                }
                else if (dat[1] == 0x06)
                {
                    if (dat.Length >= 2)
                    {
                        byte[] bytes = new byte[dat.Length - 2];
                        Array.Copy(dat, 2, bytes, 0, bytes.Length);
                        strings += "-->DTC Analyze\r\n";
                        strings += "-->DTC Extended Record:" + bytes.HexToStrings(" ") + "\r\n";
                    }
                }
                else
                {
                    strings += "-->DTC Analyze\r\n";
                    DtcFinder dtc_finder = new DtcFinder();
                    Dtc dtc = new Dtc("", "");
                    int dtc_num = (dat.Length - 3) / 4;
                    int dtc_sts;
                    int index; 
                    strings += "-->DTC Number:" + dtc_num.ToString() + "\r\n";
                    for (index = 0; index < dtc_num; index++)
                    {
                        dtc_finder.id = ((int)dat[3 + index * 4 + 0] << 16
                                | (int)dat[3 + index * 4 + 1] << 8
                                | (int)dat[3 + index * 4 + 2]).ToString("X6");
                        dtc_sts = dat[3 + index * 4 + 3];
                        dtc = dtc_list.Find(dtc_finder.FindDtcById);
                        if (DTCCodeToolStripMenuItem.Checked)
                        {
                            if (dtc != null)
                            {
                                strings += "$" + dtc.id.DtcEscape() + " " + dtc_sts.ToString("X2") + " " + dtc.name + "\r\n";
                            }
                            else
                            {
                                strings += "$" + dtc_finder.id.DtcEscape() + " " + dtc_sts.ToString("X2") + " " + "\r\n";
                            }
                        }
                        else
                        {
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
            }
            if (strings != string.Empty)
            {
                EventHandler TextBoxUpdate = delegate
                {
                    textBoxStream.AppendText(strings);
                };
                try { Invoke(TextBoxUpdate); } catch { };
            }
        }

        private void Analyze(byte[] dat)
        {
            NrcAnalyze(dat);
            DtcAnalyze(dat);
        }

        #endregion

        #region UDS

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
            trans.EventRxMsgs += new EventHandler(
                (sender1, e1) =>
                {
                    uds_trans.RxMsgEventArgs RxMsg = (uds_trans.RxMsgEventArgs)e1;
                    uds_rx_msg(RxMsg.dat);
                }
                );

            trans.EventError += new EventHandler(
                (sender1, e1) =>
                {
                    uds_trans.ErrorEventArgs Error = (uds_trans.ErrorEventArgs)e1;
                    EventHandler TextBoxUpdate = delegate
                    {
                        textBoxStream.AppendText(Error.ToString() + "\r\n");
                    };
                    try { Invoke(TextBoxUpdate); } catch { };
                }
                );
            #endregion

            uds_seriver_init();
        }

        public class Cmd
        {
            public string cmd;
            public string name;
        };

        List<Cmd> cmd_list = new List<Cmd>();

        public class Routine
        {
            public string id;
            public string start;
            public string stop;
            public string result;
            public string name;
        };

        List<Routine> routine_list = new List<Routine>();

        uds_service now_service = new uds_service();
        uds_service service_10 = new uds_service();
        uds_service service_11 = new uds_service();
        uds_service service_14 = new uds_service();
        uds_service service_19 = new uds_service();
        uds_service service_22 = new uds_service();
        uds_service service_23 = new uds_service();
        uds_service service_27 = new uds_service();
        uds_service service_28 = new uds_service();
        uds_service service_2E = new uds_service();
        uds_service service_2F = new uds_service();
        uds_service service_3D = new uds_service();
        uds_service service_3E = new uds_service();
        uds_service service_85 = new uds_service();

        private void uds_seriver_init()
        {
            services_list = new List<Uds.uds_service>();
            uds_service.SubFunction sub_function = new uds_service.SubFunction();
            uds_service.Identifier identifier = new uds_service.Identifier();

            #region Cmd
            Cmd cmd = new Cmd();
            cmd = new Cmd();
            cmd.cmd = "1101";
            cmd.name = "ECU Reset";
            cmd_list.Add(cmd);
            #endregion

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
            //services_list.Add(service_10);
            #endregion

            #region $11 ECU Reset
            service_11 = new uds_service();
            service_11.sid = "11";
            service_11.name = "ECU Reset";
            service_11.sub_function_list = new List<uds_service.SubFunction>();
            //@parameter
            //@sub_function_1
            sub_function = new uds_service.SubFunction();
            sub_function.id = "01";
            sub_function.name = "Hard Reset";
            service_11.sub_function_list.Add(sub_function);
            //@sub_function_2
            sub_function = new uds_service.SubFunction();
            sub_function.id = "02";
            sub_function.name = "Key Off On Reset";
            service_11.sub_function_list.Add(sub_function);
            //@sub_function_3
            sub_function = new uds_service.SubFunction();
            sub_function.id = "03";
            sub_function.name = "Soft Reset";
            service_11.sub_function_list.Add(sub_function);
            //@sub_function_4
            sub_function = new uds_service.SubFunction();
            sub_function.id = "04";
            sub_function.name = "Enable Rapid Power Shut Down";
            service_11.sub_function_list.Add(sub_function);
            //@sub_function_5
            sub_function = new uds_service.SubFunction();
            sub_function.id = "05";
            sub_function.name = "Disable Rapid Power Shut Down";
            service_11.sub_function_list.Add(sub_function);
            services_list.Add(service_11);
            #endregion

            #region $14 Clear Diagnostic Information
            service_14 = new uds_service();
            service_14.sid = "14";
            service_14.name = "Clear Diagnostic Information";
            service_14.sub_function_list = new List<uds_service.SubFunction>();
            //@sub_function
            //end_sub_function
            //@identifier
            identifier = new uds_service.Identifier();
            identifier.id = "FFFFFF";
            identifier.name = "All DTC";
            service_14.identifier_list.Add(identifier);
            foreach (Dtc dtc in dtc_list)
            {
                identifier = new uds_service.Identifier();
                identifier.id = dtc.id;
                identifier.name = dtc.name;
                service_14.identifier_list.Add(identifier);
            }
            //@end_identifier
            services_list.Add(service_14);
            #endregion

            #region $19 Read DTC Information
            service_19 = new uds_service();
            service_19.sid = "19";
            service_19.name = "Read DTC Information";
            service_19.sub_function_list = new List<uds_service.SubFunction>();
            //@sub_function_1
            sub_function = new uds_service.SubFunction();
            sub_function.id = "01";
            sub_function.name = "Report Number Of DTC By Status Mask";
            sub_function.parameter = "09";
            sub_function.identifier_enabled = false;
            service_19.sub_function_list.Add(sub_function);
            //@sub_function_2
            sub_function = new uds_service.SubFunction();
            sub_function.id = "02";
            sub_function.name = "Report DTC By Status Mask";
            sub_function.parameter = "09";
            sub_function.identifier_enabled = false;
            service_19.sub_function_list.Add(sub_function);
            //@sub_function_3
            sub_function = new uds_service.SubFunction();
            sub_function.id = "04";
            sub_function.name = "Report DTC Snapshot Record By DTC Number";
            sub_function.parameter = "01";
            service_19.sub_function_list.Add(sub_function);
            //@sub_function_4
            sub_function = new uds_service.SubFunction();
            sub_function.id = "06";
            sub_function.name = "Report DTC Extended Datar Record By DTC Number";
            sub_function.parameter = "01";
            service_19.sub_function_list.Add(sub_function);
            //@sub_function_5
            sub_function = new uds_service.SubFunction();
            sub_function.id = "0A";
            sub_function.name = "Report Supported DTC";
            sub_function.identifier_enabled = false;
            service_19.sub_function_list.Add(sub_function);
            //@identifier
            foreach (Dtc dtc in dtc_list)
            {
                identifier = new uds_service.Identifier();
                identifier.id = dtc.id;
                identifier.name = dtc.name;
                service_19.identifier_list.Add(identifier);
            }
            //@end_identifier
            services_list.Add(service_19);
            #endregion

            #region $22 Read Data By Identifier
            service_22 = new uds_service();
            service_22.sid = "22";
            service_22.name = "Read Data By Identifier";
            service_22.sub_function_list = new List<uds_service.SubFunction>();
            //@sub_function
            //end_sub_function
            //@identifier
            identifier = new uds_service.Identifier();
            identifier.id = "F183";
            identifier.name = "ECU Bootloader Software Number";
            service_22.identifier_list.Add(identifier);
            identifier = new uds_service.Identifier();
            identifier.id = "F184";
            identifier.name = "ECU Application Software Number";
            service_22.identifier_list.Add(identifier);
            identifier = new uds_service.Identifier();
            identifier.id = "F18E";
            identifier.name = "ECU Assembly Number(Part Number)";
            service_22.identifier_list.Add(identifier);
            identifier = new uds_service.Identifier();
            identifier.id = "F18A";
            identifier.name = "System Supplier Identifier";
            service_22.identifier_list.Add(identifier);
            identifier = new uds_service.Identifier();
            identifier.id = "F190";
            identifier.name = "Vin";
            service_22.identifier_list.Add(identifier);
            identifier = new uds_service.Identifier();
            identifier.id = "F191";
            identifier.name = "ECU Hardware Number";
            service_22.identifier_list.Add(identifier);
            identifier = new uds_service.Identifier();
            identifier.id = "F1A0";
            identifier.name = "Vehicle Network Number";
            service_22.identifier_list.Add(identifier);
            identifier = new uds_service.Identifier();
            identifier.id = "F198";
            identifier.name = "Repair Shop Code/Tester Serial Number";
            service_22.identifier_list.Add(identifier);
            identifier = new uds_service.Identifier();
            identifier.id = "F199";
            identifier.name = "Programming Date";
            service_22.identifier_list.Add(identifier);
            identifier = new uds_service.Identifier();
            identifier.id = "F284";
            identifier.name = "ATECH Application Software Number";
            service_22.identifier_list.Add(identifier);
            //@end_identifier
            services_list.Add(service_22);
            #endregion

            #region $23 Read Memory By Address
            service_23 = new uds_service();
            service_23.sid = "23";
            service_23.name = "Read Memory By Address";
            service_23.sub_function_list = new List<uds_service.SubFunction>();
            //@sub_function
            //end_sub_function
            //@identifier
            identifier = new uds_service.Identifier();
            identifier.id = "24";
            identifier.name = "Address Lenght And Read Memory Lenght";
            identifier.parameter = "00 00 00 00 00 01";
            service_23.identifier_list.Add(identifier);
            //@end_identifier
            //services_list.Add(service_23);
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
            //services_list.Add(service_27);
            #endregion

            #region $28 Communication Control
            service_28 = new uds_service();
            service_28.sid = "28";
            service_28.name = "Communication Control";
            service_28.sub_function_list = new List<uds_service.SubFunction>();
            //@sub_function
            sub_function = new uds_service.SubFunction();
            sub_function.id = "00";
            sub_function.name = "Enable Rx And Tx";
            service_28.sub_function_list.Add(sub_function);
            sub_function = new uds_service.SubFunction();
            sub_function.id = "80";
            sub_function.name = "Enable Rx And Tx Suppress Pos Res";
            service_28.sub_function_list.Add(sub_function);
            sub_function = new uds_service.SubFunction();
            sub_function.id = "03";
            sub_function.name = "Disable Rx And Tx";
            service_28.sub_function_list.Add(sub_function);
            sub_function = new uds_service.SubFunction();
            sub_function.id = "83";
            sub_function.name = "Rx And Tx Suppress Pos Res";
            service_28.sub_function_list.Add(sub_function);
            //end_sub_function
            //@identifier
            identifier = new uds_service.Identifier();
            identifier.id = "01";
            identifier.name = "Normal Communication Messages";
            service_28.identifier_list.Add(identifier);
            identifier = new uds_service.Identifier();
            identifier.id = "02";
            identifier.name = "Network Management Communication Messages";
            service_28.identifier_list.Add(identifier);
            identifier = new uds_service.Identifier();
            identifier.id = "03";
            identifier.name = "Normal and Network Management Communication Messages";
            service_28.identifier_list.Add(identifier);
            //@end_identifier
            services_list.Add(service_28);
            #endregion

            #region $2E "Write Data By Identifier
            service_2E = new uds_service();
            service_2E.sid = "2E";
            service_2E.name = "Write Data By Identifier";
            service_2E.sub_function_list = new List<uds_service.SubFunction>();
            //@identifier_7
            identifier = new uds_service.Identifier();
            identifier.id = "F198";
            identifier.name = "Repair Shop Code/Tester Serial Number";
            identifier.parameter = "FFFFFFFFFFFFFFFFFFFF";
            service_2E.identifier_list.Add(identifier);
            //@identifier_8
            identifier = new uds_service.Identifier();
            identifier.id = "F199";
            identifier.name = "Programming Date";
            identifier.parameter = "FFFFFFFFFFFF";
            service_2E.identifier_list.Add(identifier);
            services_list.Add(service_2E);
            #endregion

            #region $2F Input Output Control By Identifier
            service_2F = new uds_service();
            service_2F.sid = "2F";
            service_2F.name = "Input Output Control By Identifier";
            service_2F.sub_function_list = new List<uds_service.SubFunction>();
            //@sub_function
            //1
            sub_function = new uds_service.SubFunction();
            sub_function.id = "00";
            sub_function.name = "Return Control To ECU";
            service_2F.sub_function_list.Add(sub_function);
            ////2
            //sub_function = new uds_service.SubFunction();
            //sub_function.id = "01";
            //sub_function.name = "Reset To Default";
            //service_2F.sub_function_list.Add(sub_function);
            ////3
            //sub_function = new uds_service.SubFunction();
            //sub_function.id = "02";
            //sub_function.name = "Freeze Current State";
            //service_2F.sub_function_list.Add(sub_function);
            //4
            sub_function = new uds_service.SubFunction();
            sub_function.id = "03";
            sub_function.name = "Short Term Adjustment";
            service_2F.sub_function_list.Add(sub_function);
            //@end_sub_function
            //@identifier
            //1
            identifier = new uds_service.Identifier();
            identifier.id = "D500";
            identifier.name = "Digital Input 1";
            identifier.parameter = "FFFFFFFFFFFFFFFF";
            service_2F.identifier_list.Add(identifier);
            //2
            identifier = new uds_service.Identifier();
            identifier.id = "D501";
            identifier.name = "Digital Input 2";
            identifier.parameter = "FFFFFFFFFFFFFFFF";
            service_2F.identifier_list.Add(identifier);
            //3
            identifier = new uds_service.Identifier();
            identifier.id = "D502";
            identifier.name = "Out Put 1";
            identifier.parameter = "FFFFFFFFFFFFFFFF";
            service_2F.identifier_list.Add(identifier);
            //4
            identifier = new uds_service.Identifier();
            identifier.id = "D503";
            identifier.name = "Out Put 2";
            identifier.parameter = "FFFFFFFFFFFFFFFF";
            service_2F.identifier_list.Add(identifier);
            //5
            identifier = new uds_service.Identifier();
            identifier.id = "D504";
            identifier.name = "Out Put 3";
            identifier.parameter = "FFFFFFFFFFFFFFFF";
            service_2F.identifier_list.Add(identifier);
            //@end_identifier
            services_list.Add(service_2F);
            #endregion

            #region $31 Routine Control
            Routine routine = new Routine();
            //1
            routine = new Routine();
            routine.id = "7501";
            routine.start = "AABBCCDD";
            routine.name = "Generate Random Secret Key";
            routine_list.Add(routine);
            //2
            routine = new Routine();
            routine.id = "7502";
            routine.start = "AABBCCDD";
            routine.name = "Lock ECU";
            routine_list.Add(routine);
            //3
            routine = new Routine();
            routine.id = "7503";
            routine.start = "AABBCCDD";
            routine.name = "Add Key";
            routine_list.Add(routine);
            //4
            routine = new Routine();
            routine.id = "7504";
            routine.start = "AABBCCDD";
            routine.name = "Delete Key";
            routine_list.Add(routine);
            //5
            routine = new Routine();
            routine.id = "7505";
            routine.start = "AABBCCDD";
            routine.name = "Learn Secret Key from EMS";
            routine_list.Add(routine);
            //6
            routine = new Routine();
            routine.id = "7506";
            routine.start = "AABBCCDD";
            routine.name = "Teach Secret key to EMS";
            routine_list.Add(routine);
            //7
            routine = new Routine();
            routine.id = "7507";
            routine.start = "AABBCCDD";
            routine.name = "Key Test";
            routine_list.Add(routine);
            //8
            routine = new Routine();
            routine.id = "7508";
            routine.start = "64000000";
            routine.name = "DD Windows Position";
            routine_list.Add(routine);
            //9
            routine = new Routine();
            routine.id = "7509";
            routine.start = "64000000";
            routine.name = "PD Windows Position";
            routine_list.Add(routine);
            //10
            routine = new Routine();
            routine.id = "750A";
            routine.start = "64000000";
            routine.name = "RLD Windows Position";
            routine_list.Add(routine);
            //11
            routine = new Routine();
            routine.id = "750B";
            routine.start = "64000000";
            routine.name = "RRD Windows Position";
            routine_list.Add(routine);
            //12
            routine = new Routine();
            routine.id = "750C";
            routine.start = "01";
            routine.name = "HornCtr";
            routine_list.Add(routine);
            //13
            routine = new Routine();
            routine.id = "750D";
            routine.start = "64646464";
            routine.name = "All Windows Position";
            routine_list.Add(routine);
            //14
            routine = new Routine();
            routine.id = "750E";
            routine.name = "ALL Door Lock";
            routine_list.Add(routine);
            //15
            routine = new Routine();
            routine.id = "750F";
            routine.name = "ALL Door Unlock";
            routine_list.Add(routine);
            //16
            routine = new Routine();
            routine.id = "7500";
            routine.name = "Driver Door Unlock";
            routine_list.Add(routine);
            //17
            routine = new Routine();
            routine.id = "7550";
            routine.start = "FF";
            routine.name = "Hazard Control";
            routine_list.Add(routine);
            //18
            routine = new Routine();
            routine.id = "7551";
            routine.start = "F0";
            routine.name = "Find My Car";
            routine_list.Add(routine);
            //19
            routine = new Routine();
            routine.id = "FF01";
            routine.name = "Check Programming Dependecies";
            routine_list.Add(routine);
            //20
            routine = new Routine();
            routine.id = "FFF7";
            routine.name = "Immo Unlock";
            routine_list.Add(routine);
            //21
            routine = new Routine();
            routine.id = "FFF8";
            routine.name = "Test ABIC Phase";
            routine_list.Add(routine);
            //22
            routine = new Routine();
            routine.id = "FFF9";
            routine.name = "DEBUG Mode";
            routine_list.Add(routine);
            //23
            routine = new Routine();
            routine.id = "FDF0";
            routine.name = "Turn Calibration";
            routine_list.Add(routine);
            //24
            routine = new Routine();
            routine.id = "FFFA";
            routine.start = "FFFF";
            routine.name = "Valid All Key";
            routine_list.Add(routine);
            
            #endregion

            #region $3D Write Memory By Address
            service_3D = new uds_service();
            service_3D.sid = "3D";
            service_3D.name = "Write Memory By Address";
            service_3D.sub_function_list = new List<uds_service.SubFunction>();
            //@sub_function
            //end_sub_function
            //@identifier
            identifier = new uds_service.Identifier();
            identifier.id = "24";
            identifier.name = "Address Lenght And Read Memory Lenght";
            identifier.parameter = "00 00 00 00 00 01 00";
            service_3D.identifier_list.Add(identifier);
            //@end_identifier
            //services_list.Add(service_3D);
            #endregion

            #region $3E Tester Present
            service_3E = new uds_service();
            service_3E.sid = "3E";
            service_3E.name = "Tester Present";
            service_3E.sub_function_list = new List<uds_service.SubFunction>();
            //@sub_function_1
            sub_function = new uds_service.SubFunction();
            sub_function.id = "00";
            sub_function.name = "Test Present";
            service_3E.sub_function_list.Add(sub_function);
            //@sub_function_2
            sub_function = new uds_service.SubFunction();
            sub_function.id = "80";
            sub_function.name = "Test Present Suppress Pos Res";
            service_3E.sub_function_list.Add(sub_function);
            services_list.Add(service_3E);
            #endregion

            #region $85 Control DTC Setting
            service_85 = new uds_service();
            service_85.sid = "85";
            service_85.name = "Control DTC Setting";
            service_85.sub_function_list = new List<uds_service.SubFunction>();
            //@sub_function_1
            sub_function = new uds_service.SubFunction();
            sub_function.id = "01";
            sub_function.name = "DTC Logging On";
            service_85.sub_function_list.Add(sub_function);
            //@sub_function_2
            sub_function = new uds_service.SubFunction();
            sub_function.id = "81";
            sub_function.name = "DTC Logging On Suppress Pos Res";
            service_85.sub_function_list.Add(sub_function);
            //@sub_function_3
            sub_function = new uds_service.SubFunction();
            sub_function.id = "02";
            sub_function.name = "DTC Logging Off";
            service_85.sub_function_list.Add(sub_function);
            //@sub_function_4
            sub_function = new uds_service.SubFunction();
            sub_function.id = "82";
            sub_function.name = "DTC Logging Off Suppress Pos Res";
            service_85.sub_function_list.Add(sub_function);
            services_list.Add(service_85);
            #endregion

            foreach(Cmd c in cmd_list)
            {
                comboBoxCmd.Items.Add(c.name);
                comboBoxCmd.SelectedIndex = 0;
            }

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

            foreach (Routine r in routine_list)
            {
                comboBoxRoutineIdent.Items.Add("$" + r.id + " " + r.name);
                comboBoxRoutineIdent.SelectedIndex = 0;
            }

            comboBoxRoutineSubFunction.Items.Add("$01 Start Routine");
            comboBoxRoutineSubFunction.Items.Add("$02 Stop Routine");
            comboBoxRoutineSubFunction.Items.Add("$03 Result Routine");
            comboBoxRoutineSubFunction.SelectedIndex = 0;

        }

        private void uds_rx_msg(byte[] dat)
        {
            if (dat[0] == 0x67)
            {
                uint seed = 0;
                uint level;
                uint result = 0;
                if (dat.Length == 4)
                {
                    seed = (uint)dat[2] << 8
                        | (uint)dat[3];
                }
                else if (dat.Length == 6)
                {
                    seed = (uint)dat[2] << 24
                        | (uint)dat[3] << 16
                        | (uint)dat[4] << 8
                        | (uint)dat[5];
                }
                level = dat[1];
                if (seed != 0 && level % 2 != 0)
                {
                    result = SecurityAccess(0, seed, level);
                    if (dat.Length == 4)
                    {
                        result &= 0xFFFF;
                        trans.CanTrans_TxMsg(mode, "27" + (level + 1).ToString("x2") + result.ToString("x4"));
                    }
                    else if (dat.Length == 6)
                    {
                        trans.CanTrans_TxMsg(mode, "27" + (level + 1).ToString("x2") + result.ToString("x8"));
                    }
                }
            }
            if (AnalyzeToolStripMenuItem.Checked)
            {
                Analyze(dat);
            }
        }

        private void uds_tx_msg(string strings)
        {
            if (AutoToolStripMenuItem.Checked)
            {
                textBoxStream.Text = "";
            }
            trans.CanTrans_TxMsg(mode, strings);
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
            updateServicesTransData();
        }

        private void comboBoxSubFunction_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (now_service.sub_function_list.Count != 0)
            {
                now_service.sub_function_selectd = now_service.sub_function_list[comboBoxSubFunction.SelectedIndex];
                comboBoxIdentifier.Enabled = now_service.sub_function_selectd.identifier_enabled;
            }
            updateServicesTransData();
        }

        private void comboBoxIdentifier_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (now_service.identifier_list.Count != 0)
            {
                now_service.identifier_selected = now_service.identifier_list[comboBoxIdentifier.SelectedIndex];
            }
            updateServicesTransData();
        }

        private void comboBoxRoutine_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxRoutineSubFunction.SelectedIndex == 0)
            {
                textBoxRoutineParameter.Text = routine_list[comboBoxRoutineIdent.SelectedIndex].start;
            }
            else if (comboBoxRoutineSubFunction.SelectedIndex == 1)
            {
                textBoxRoutineParameter.Text = routine_list[comboBoxRoutineIdent.SelectedIndex].stop;
            }
            else if (comboBoxRoutineSubFunction.SelectedIndex == 2)
            {
                textBoxRoutineParameter.Text = routine_list[comboBoxRoutineIdent.SelectedIndex].result;
            }
        }

        void updateServicesTransData()
        {
            textBoxTransData.Text = now_service.ToString();
        }

        private void textBoxParameter_TextChanged(object sender, EventArgs e)
        {
            updateServicesTransData();
        }

        private void button_Click(object sender, EventArgs e)
        {
            uds_tx_msg(textBoxTransData.Text);
        }

        private void buttonCmd_Click(object sender, EventArgs e)
        {
            uds_tx_msg(cmd_list[comboBoxCmd.SelectedIndex].cmd);
        }

        private void buttonRoutine_Click(object sender, EventArgs e)
        {
            string strings = "31";

            if (comboBoxRoutineSubFunction.SelectedIndex == 0)
            {
                strings += "01";
            }
            else if (comboBoxRoutineSubFunction.SelectedIndex == 1)
            {
                strings += "02";
            }
            else if (comboBoxRoutineSubFunction.SelectedIndex == 2)
            {
                strings += "03";
            }

            strings += routine_list[comboBoxRoutineIdent.SelectedIndex].id;
            strings += textBoxRoutineParameter.Text;

            uds_tx_msg(strings);
        }

        private void textBoxTransData_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox textbox = (TextBox)sender;
            if (e.KeyChar == '\r')
            {
                uds_tx_msg(textbox.Text);
            }
        }

        private bool delete_char_flag = false;
        private void textBoxHex_KeyPress(object sender, KeyPressEventArgs e)
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
        }

        private void textBoxHex_TextChanged(object sender, EventArgs e)
        {
            TextBox textbox = (TextBox)sender;
            string strings = textbox.Text;
            int i_idx = textbox.Text.Substring(0, textbox.SelectionStart).Replace(" ", "").Length;
            if (delete_char_flag == true)
            {
                delete_char_flag = false;
                if ((strings.Replace(" ", "").Length % 2) != 0)
                {
                    return;
                }
            }
            textbox.Text = strings.Replace(" ", "").InsertSpace(2);
            textbox.SelectionStart = i_idx + i_idx/2;
        }

        private void buttonSession_Click(object sender, EventArgs e)
        {
            trans.CanTrans_TxMsg(mode, service_10.ToString().StringToHex());
        }

        private void comboBoxSession_SelectIndexChanged(object sender, EventArgs e)
        {
            service_10.sub_function_selectd = service_10.sub_function_list[comboBoxSession.SelectedIndex];
        }

        private void buttonSecurityAccess_Click(object sender, EventArgs e)
        {
            trans.CanTrans_TxMsg(mode, service_27.ToString().StringToHex());
        }

        private void comboBoxSecurityLevel_SelectedIndexChanged(object sender, EventArgs e)
        {
            service_27.sub_function_selectd = service_27.sub_function_list[comboBoxSecurityLevel.SelectedIndex];
        }
        #endregion

        #region Variable
        public class mapClass
        {
            public string variable;
            public uint addr;
            public int size;
            public string type;
        }
        
        private List<mapClass> mapList = new List<mapClass>();

        /// <summary>
        /// 初始化变量列表
        /// </summary>
        /// <param name="list"></param>
        public void ReadWriteVariableInit(List<mapClass> list)
        {
            mapList = list;
            comboBoxVariable.Items.Clear(); 
            foreach (mapClass map in mapList)
            {
                comboBoxVariable.Items.Add(map.variable);
            }
            if(comboBoxVariable.Items.Count != 0)
            {
                comboBoxVariable.SelectedItem = 0;
            }
        }

        /// <summary>
        /// 选择变量改变事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboBoxVariable_SelectedIndexChanged(object sender, EventArgs e)
        {
            textBoxVariableAddress.Text = "0x" + mapList[comboBoxVariable.SelectedIndex].addr.ToString("X");
            textBoxVariableLength.Text = "0x" + mapList[comboBoxVariable.SelectedIndex].size.ToString("X");
            textBoxVariableType.Text = mapList[comboBoxVariable.SelectedIndex].type;
        }

        private void TextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != 8  /* 允许使用退格符 */
                && !Char.IsDigit(e.KeyChar)
                && e.KeyChar != 'x'
                && e.KeyChar != 'X'
                && e.KeyChar != ' '
                && !(((int)e.KeyChar >= 'A' && (int)e.KeyChar <= 'F'))
                && !(((int)e.KeyChar >= 'a' && (int)e.KeyChar <= 'f'))
                )
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// 打开并解析SPC的MAP文件
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private bool OpenMapFileForSpc(string file)
        {
            string start = string.Empty;
            string type = string.Empty;
            string stringsLast = string.Empty;
            StreamReader myStream = null;
            mapList.Clear();
            try
            {
                myStream = new StreamReader(file);
                string content = myStream.ReadToEnd();
                string[] str = content.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                foreach (string strings in str)
                {
                    string[] strArray = System.Text.RegularExpressions.Regex.Split((stringsLast + strings).Replace("\r\n", ""), @"\s+");
                    if (start == string.Empty)
                    {
                        if (checkBoxShowFunction.Checked && strings == " *(.text_vle.*)")
                        {
                            start = ".text_vle.";
                            type = "*(.text_vle.*)";
                        }
                        else if (checkBoxShowReadOnlyVariable.Checked && strings == " *(.rodata.*)")
                        {
                            start = ".rodata.";
                            type = "*(.rodata.*)";
                        }
                        else if (strings == " *(.data.*)")
                        {
                            start = ".data.";
                            type = "*(.data.*)";
                        }
                        else if (strings == " *(.bss.*)")
                        {
                            start = ".bss.";
                            type = "*(.bss.*)";
                        }
                    }
                    else if (strArray.Length <= 2)      //添加下一行
                    {
                        if (strings == " *(.gnu.linkonce.t_vle.*)")     //.text_vle. end
                        {
                            start = string.Empty;
                        }
                        else if (strings == " *(.rodata1)")             //.rodata. end
                        {
                            start = string.Empty;
                        }
                        else if (strings == " *(.gnu.linkonce.d.*)")     //.data. end
                        {
                            start = string.Empty;
                        }
                        else if (strings == " *(.gnu.linkonce.b.*)")     //.bss. end
                        {
                            start = string.Empty;
                        }
                        else
                        {
                            stringsLast = strings;
                        }
                    }
                    else
                    {
                        stringsLast = "";
                        try
                        {
                            mapClass mapItem = new mapClass();
                            if (strArray[1].Length > start.Length && strArray[1].Substring(0, start.Length) == start)
                            {
                                mapItem.variable = strArray[1].Remove(0, start.Length).Split(new char[] { '.'})[0];
                                mapItem.addr = Convert.ToUInt32(strArray[2].Replace("0x", ""), 16);
                                mapItem.type = type;
                                mapItem.size = Convert.ToInt32(strArray[3].Replace("0x", ""), 16);
                                mapList.Add(mapItem);
                            }
                        }
                        catch
                        {
                            start = string.Empty;
                        }
                    }
                }
                myStream.Close();
            }
            catch
            {
                return false;
            }
            return true;
        }
        
        private void buttonReadVariableValue_Click(object sender, EventArgs e)
        {
            string strings = string.Empty;
            try
            {
                strings += "2324";
                strings += Convert.ToUInt32(textBoxVariableAddress.Text.Replace("0x", ""), 16).ToString("x8");
                strings += Convert.ToUInt16(textBoxVariableLength.Text.Replace("0x", ""), 16).ToString("x4");

                trans.CanTrans_TxMsg(mode, strings.StringToHex());
            }
            catch
            {
                MessageBox.Show("输入参数错误！");
            }
        }

        private void buttonWriteVariableValue_Click(object sender, EventArgs e)
        {
            string strings = string.Empty;
            try
            {
                strings += "3D24";
                strings += Convert.ToUInt32(textBoxVariableAddress.Text.Replace("0x", ""), 16).ToString("x8");
                strings += Convert.ToUInt16(textBoxVariableLength.Text.Replace("0x", ""), 16).ToString("x4");
                strings += textBoxVariableValue.Text;
                trans.CanTrans_TxMsg(mode, strings.StringToHex());
            }
            catch
            {
                MessageBox.Show("输入参数错误！");
            }
        }
        #endregion
    }
}
