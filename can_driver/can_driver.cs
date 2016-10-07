using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using canlibCLSNET;

namespace Uds
{
    public class can_driver
    {
        private int canHandler = 0;
        private string[] channel = new string[0];

        /// <summary>
        /// 获取CAN通道
        /// </summary>
        /// <returns></returns>
        public string[] GetChannel()
        {
            int channel_num = 0x00;
            Canlib.canInitializeLibrary();

            if (Canlib.canStatus.canOK == Canlib.canGetNumberOfChannels(out channel_num))
            {
                channel = new string[channel_num];
                for (int i = 0; i < channel_num; i++)
                {
                    object canChannelType = new Object();
                    object canChannelName = new Object();

                    Canlib.canGetChannelData(i, Canlib.canCHANNELDATA_TRANS_TYPE, out canChannelType); /* get channel type */
                    Canlib.canGetChannelData(i, Canlib.canCHANNELDATA_CHANNEL_NAME, out canChannelName); /* get channel name */
                    channel[i] = Convert.ToString(canChannelName);
                }
            }
            return channel;
        }

        /// <summary>
        /// 打开CAN通道
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="baud"></param>
        /// <returns></returns>
        public bool OpenChannel(int channel, string baud)
        {
            int canFreq;

            canHandler = Canlib.canOpenChannel(channel, Canlib.canOPEN_ACCEPT_VIRTUAL);
            if (canHandler != (int)Canlib.canStatus.canOK)
            {
                return false;
            }

            switch (baud)
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
                    break;
            }

            Canlib.canStatus canStatus = Canlib.canSetBusParams(canHandler, canFreq, 0x80, 0x3A, 1, 1, 0);
            if (canStatus != Canlib.canStatus.canOK)
            {
                return false;
            }

            canStatus = Canlib.canBusOn(canHandler);
            if (canStatus != Canlib.canStatus.canOK)
            {
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
        /// <returns></returns>
        public int BusLoad()
        {
            int busload = 0;
            Canlib.canBusStatistics sss;
            if (Canlib.canRequestBusStatistics(canHandler) == Canlib.canStatus.canOK)
            {
                Canlib.canGetBusStatistics(canHandler, out sss);
                busload = (int)sss.busLoad / 100;
            }
            return busload;
        }

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
            int flag = 0;
            return Canlib.canRead(canHandler, out id, dat, out dlc, out flag, out time) == Canlib.canStatus.canOK;
        }
    }
}
