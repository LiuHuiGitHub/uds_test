using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows.Forms;

using canlibCLSNET;
using MyFormat;

namespace Uds
{
    class can_driver
    {
        private int canHandler = 0;

        /// <summary>
        /// 选择CAN通道
        /// </summary>
        /// <param name="comboBoxChannel"></param>
        public void SelectChannel(ref ComboBox comboBoxChannel)
        {
            int channel_index = 0x00;
            Canlib.canInitializeLibrary();

            if (Canlib.canStatus.canOK == Canlib.canGetNumberOfChannels(out channel_index))
            {
                comboBoxChannel.Items.Clear();
                for (int i = 0; i < channel_index; i++)
                {
                    object canChannelType = new Object();
                    object canChannelName = new Object();

                    Canlib.canGetChannelData(i, Canlib.canCHANNELDATA_TRANS_TYPE, out canChannelType); /* get channel type */
                    Canlib.canGetChannelData(i, Canlib.canCHANNELDATA_CHANNEL_NAME, out canChannelName); /* get channel name */
                    if (Convert.ToInt32(canChannelType) != Canlib.canTRANSCEIVER_TYPE_LIN)
                    {
                        comboBoxChannel.Items.Add(Convert.ToString(canChannelName));
                    }
                }
                comboBoxChannel.SelectedIndex = 0;
            }
        }
        /// <summary>
        /// 打开CAN通道
        /// </summary>
        /// <param name="comboBoxChannel"></param>
        /// <param name="comboBoxBaud"></param>
        /// <returns></returns>
        public bool OpenChannel(ref ComboBox comboBoxChannel, ref ComboBox comboBoxBaud)
        {
            int canFreq;
            if (comboBoxChannel.Items.Count == 0)
            {
                MessageBox.Show("Don't Have Any One Channel!");
                return false;
            }

            canHandler = Canlib.canOpenChannel(comboBoxChannel.SelectedIndex, Canlib.canOPEN_ACCEPT_VIRTUAL);
            if (canHandler != (int)Canlib.canStatus.canOK)
            {
                MessageBox.Show("Can't Open This Channel!");
                return false;
            }

            switch (comboBoxBaud.Text)
            {
                case "50000":
                    canFreq = Canlib.BAUD_50K;
                    break;

                case "62000":
                    canFreq = Canlib.BAUD_62K;
                    break;

                case "83000":
                    canFreq = Canlib.BAUD_83K;
                    break;

                case "100000":
                    canFreq = Canlib.BAUD_100K;
                    break;

                case "125000":
                    canFreq = Canlib.BAUD_125K;
                    break;

                case "250000":
                    canFreq = Canlib.BAUD_250K;
                    break;

                case "500000":
                    canFreq = Canlib.BAUD_500K;
                    break;

                case "1000000":
                    canFreq = Canlib.BAUD_1M;
                    break;

                default:
                    canFreq = Canlib.BAUD_500K;
                    comboBoxBaud.SelectedIndex = 1;
                    break;
            }

            Canlib.canStatus canStatus = Canlib.canSetBusParams(canHandler, canFreq, 0x80, 0x3A, 1, 1, 0);
            if (canStatus != Canlib.canStatus.canOK)
            {
                MessageBox.Show("Can't Open This Channel!");
                return false;
            }

            canStatus = Canlib.canBusOn(canHandler);
            if (canStatus != Canlib.canStatus.canOK)
            {
                MessageBox.Show("Can't Open This Channel!");
                return false;
            }

            return true;
        }
        /// <summary>
        /// 关闭CAN通道
        /// </summary>
        /// <returns></returns>
        public bool CloseChannel()
        {
            Canlib.canBusOff(canHandler);
            Canlib.canClose(canHandler);
            return true;
        }

        /// <summary>
        /// CAN获取总线负载率
        /// </summary>
        /// <param name="busload"></param>
        /// <returns></returns>
        public bool BusLoad(ref int busload)
        {
            Canlib.canBusStatistics sss;
            if (Canlib.canRequestBusStatistics(canHandler) == Canlib.canStatus.canOK)
            {
                Canlib.canGetBusStatistics(canHandler, out sss);
                busload = (int)sss.busLoad / 100;
                return true;
            }
            return false;
        }

        public class WriteDataEventArgs : EventArgs
        {
            public int id = 0;
            public int dlc = 0;
            public byte[] dat = new byte[8];
            public int time = 0;
            public override string ToString()
            {
                time %= 1000000;
                return id.ToString("X3") + " "
                           + dlc.ToString("X1") + " "
                           + dat.HexToStrings(" ") + " "
                           + (time / 1000).ToString() + "." + (time % 1000).ToString("d3");
            }
        }

        private WriteDataEventArgs e_args = new WriteDataEventArgs();
        public event EventHandler EventWriteData;
        /// <summary>
        /// CAN发送一帧数据
        /// </summary>
        /// <param name="id"></param>
        /// <param name="dat"></param>
        /// <param name="dlc"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public bool WriteData(int id, byte[] dat, int dlc, out long time)
        {
            time = 0;
            Canlib.canStatus canStatus = Canlib.canWrite(canHandler, id, dat, dlc, 0);
            if (canStatus != Canlib.canStatus.canOK)
            {
                return false;
            }
            int ttime = 0;
            Canlib.kvReadTimer(canHandler, out ttime);
            time = (long)ttime;
            if (EventWriteData != null)
            {
                e_args.id = id;
                e_args.dlc = dlc;
                e_args.time = ttime;
                Array.Copy(dat, e_args.dat, dlc);
                EventWriteData(this, e_args);
            }
            return true;
        }
        /// <summary>
        /// CAN读取接收的一帧数据
        /// </summary>
        /// <param name="id"></param>
        /// <param name="dat"></param>
        /// <param name="dlc"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public bool ReadData(out int id, ref byte[] dat, out int dlc, out long time)
        {
            int flag = 0 ;
            if(Canlib.canRead(canHandler, out id, dat, out dlc, out flag, out time) == Canlib.canStatus.canOK)
            {
                return true;
            }
            return false;
        }
    }
}
