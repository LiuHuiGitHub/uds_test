using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Uds
{
    public class can_driver
    {
        /*------------兼容ZLG的数据类型---------------------------------*/

        //1.ZLGCAN系列接口卡信息的数据类型。
        public struct VCI_BOARD_INFO
        {
            public ushort hw_Version;
            public ushort fw_Version;
            public ushort dr_Version;
            public ushort in_Version;
            public ushort irq_Num;
            public byte can_Num;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public byte[] str_Serial_Num;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
            public byte[] str_hw_Type;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] Reserved;
        }

        /////////////////////////////////////////////////////
        //2.定义CAN信息帧的数据类型。
        unsafe public struct VCI_CAN_OBJ  //使用不安全代码
        {
            public uint ID;
            public uint TimeStamp;        //时间标识
            public byte TimeFlag;         //是否使用时间标识
            public byte SendType;         //发送标志。保留，未用
            public byte RemoteFlag;       //是否是远程帧
            public byte ExternFlag;       //是否是扩展帧
            public byte DataLen;

            public fixed byte Data[8];

            public fixed byte Reserved[3];
        }

        //3.定义CAN控制器状态的数据类型。
        public struct VCI_CAN_STATUS
        {
            public byte ErrInterrupt;
            public byte regMode;
            public byte regStatus;
            public byte regALCapture;
            public byte regECCapture;
            public byte regEWLimit;
            public byte regRECounter;
            public byte regTECounter;
            public uint Reserved;
        }

        //4.定义错误信息的数据类型。
        public struct VCI_ERR_INFO
        {
            public uint ErrCode;
            public byte Passive_ErrData1;
            public byte Passive_ErrData2;
            public byte Passive_ErrData3;
            public byte ArLost_ErrData;
        }

        //5.定义初始化CAN的数据类型
        public struct VCI_INIT_CONFIG
        {
            public uint AccCode;
            public uint AccMask;
            public uint Reserved;
            public byte Filter;   //1接收所有帧。2标准帧滤波，3是扩展帧滤波。
            public byte Timing0;
            public byte Timing1;
            public byte Mode;     //模式，0表示正常模式，1表示只听模式,2自测模式
        }

        /*------------其他数据结构描述---------------------------------*/
        //6.USB-CAN总线适配器板卡信息的数据类型1，该类型为VCI_FindUsbDevice函数的返回参数。
        public struct VCI_BOARD_INFO1
        {
            public ushort hw_Version;
            public ushort fw_Version;
            public ushort dr_Version;
            public ushort in_Version;
            public ushort irq_Num;
            public byte can_Num;
            public byte Reserved;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] str_Serial_Num;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] str_hw_Type;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[][] str_Usb_Serial;
        }

        //7.定义常规参数类型
        public struct VCI_REF_NORMAL
        {
            public byte Mode;     //模式，0表示正常模式，1表示只听模式,2自测模式
            public byte Filter;   //1接收所有帧。2标准帧滤波，3是扩展帧滤波。
            public uint AccCode;//接收滤波验收码
            public uint AccMask;//接收滤波屏蔽码
            public byte kBaudRate;//波特率索引号，0-SelfDefine,1-5Kbps(未用),2-18依次为：10kbps,20kbps,40kbps,50kbps,80kbps,100kbps,125kbps,200kbps,250kbps,400kbps,500kbps,666kbps,800kbps,1000kbps,33.33kbps,66.66kbps,83.33kbps
            public byte Timing0;
            public byte Timing1;
            public byte CANRX_EN;//保留，未用
            public byte UARTBAUD;//保留，未用
        }

        //8.定义波特率设置参数类型
        public struct VCI_BAUD_TYPE
        {
            public uint Baud;             //存储波特率实际值
            public byte SJW;                //同步跳转宽度，取值1-4
            public byte BRP;                //预分频值，取值1-64
            public byte SAM;                //采样点，取值0=采样一次，1=采样三次
            public byte PHSEG2_SEL;         //相位缓冲段2选择位，取值0=由相位缓冲段1时间决定,1=可编程
            public byte PRSEG;              //传播时间段，取值1-8
            public byte PHSEG1;             //相位缓冲段1，取值1-8
            public byte PHSEG2;             //相位缓冲段2，取值1-8
        }

        //9.定义Reference参数类型
        public struct VCI_REF_STRUCT
        {
            public VCI_REF_NORMAL RefNormal;
            public byte Reserved;
            public VCI_BAUD_TYPE BaudType;
        }

        /*------------数据结构描述完成---------------------------------*/

        public struct CHGDESIPANDPORT
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public byte[] szpwd;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public byte[] szdesip;
            public int desport;

            public void Init()
            {
                szpwd = new byte[10];
                szdesip = new byte[20];
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="DeviceType"></param>
        /// <param name="DeviceInd"></param>
        /// <param name="Reserved"></param>
        /// <returns></returns>
        /*------------兼容ZLG的函数描述---------------------------------*/
        [DllImport("controlcan.dll")]
        static extern uint VCI_OpenDevice(uint DeviceType, uint DeviceInd, uint Reserved);
        [DllImport("controlcan.dll")]
        static extern uint VCI_CloseDevice(uint DeviceType, uint DeviceInd);
        [DllImport("controlcan.dll")]
        static extern uint VCI_InitCAN(uint DeviceType, uint DeviceInd, uint CANInd, ref VCI_INIT_CONFIG pInitConfig);

        [DllImport("controlcan.dll")]
        static extern uint VCI_ReadBoardInfo(uint DeviceType, uint DeviceInd, ref VCI_BOARD_INFO pInfo);
        [DllImport("controlcan.dll")]
        static extern uint VCI_ReadErrInfo(uint DeviceType, uint DeviceInd, uint CANInd, ref VCI_ERR_INFO pErrInfo);
        [DllImport("controlcan.dll")]
        static extern uint VCI_ReadCANStatus(uint DeviceType, uint DeviceInd, uint CANInd, ref VCI_CAN_STATUS pCANStatus);

        [DllImport("controlcan.dll")]
        static extern uint VCI_GetReference(uint DeviceType, uint DeviceInd, uint CANInd, uint RefType, ref byte pData);
        [DllImport("controlcan.dll")]
        static extern uint VCI_SetReference(uint DeviceType, uint DeviceInd, uint CANInd, uint RefType, ref byte pData);

        [DllImport("controlcan.dll")]
        static extern uint VCI_GetReceiveNum(uint DeviceType, uint DeviceInd, uint CANInd);
        [DllImport("controlcan.dll")]
        static extern uint VCI_ClearBuffer(uint DeviceType, uint DeviceInd, uint CANInd);

        [DllImport("controlcan.dll")]
        static extern uint VCI_StartCAN(uint DeviceType, uint DeviceInd, uint CANInd);
        [DllImport("controlcan.dll")]
        static extern uint VCI_ResetCAN(uint DeviceType, uint DeviceInd, uint CANInd);

        [DllImport("controlcan.dll")]
        static extern uint VCI_Transmit(uint DeviceType, uint DeviceInd, uint CANInd, ref VCI_CAN_OBJ pSend, uint Len);

        [DllImport("controlcan.dll")]
        static extern uint VCI_Receive(uint DeviceType, uint DeviceInd, uint CANInd, ref VCI_CAN_OBJ pReceive, uint Len, int WaitTime);

        // [DllImport("controlcan.dll", CharSet = CharSet.Ansi)]
        //static extern uint VCI_Receive(uint DeviceType, uint DeviceInd, uint CANInd, IntPtr pReceive, uint Len, int WaitTime);

        /*------------其他函数描述---------------------------------*/
        [DllImport("controlcan.dll")]
        static extern uint VCI_GetReference2(uint DevType, uint DevIndex, uint CANIndex, uint Reserved, ref VCI_REF_STRUCT pRefStruct);
        [DllImport("controlcan.dll")]
        static extern uint VCI_SetReference2(uint DevType, uint DevIndex, uint CANIndex, uint RefType, ref byte pData);
        [DllImport("controlcan.dll")]
        static extern uint VCI_ResumeConfig(uint DevType, uint DevIndex, uint CANIndex);

        [DllImport("controlcan.dll")]
        static extern uint VCI_ConnectDevice(uint DevType, uint DevIndex);
        [DllImport("controlcan.dll")]
        static extern uint VCI_UsbDeviceReset(uint DevType, uint DevIndex, uint Reserved);
        [DllImport("controlcan.dll")]
        static extern uint VCI_FindUsbDevice(ref VCI_BOARD_INFO1 pInfo);
        /*------------函数描述结束---------------------------------*/

        const int DEV_USBCAN = 3;
        const int DEV_USBCAN2 = 4;

        static uint m_devtype = 4;//DEV_USBCAN2

        private uint m_devind = 0;
        private uint m_canind = 0;

        private bool m_bOpen = false;

        private VCI_CAN_OBJ m_recobj = new VCI_CAN_OBJ();

        private string[] channel = new string[] { "DEV_USBCAN2 Channel0", "DEV_USBCAN2 Channel1" };

        /// <summary>
        /// 获取CAN通道
        /// </summary>
        /// <returns></returns>
        public string[] GetChannel()
        {
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
            VCI_INIT_CONFIG config = new VCI_INIT_CONFIG();
            config.AccCode = 0x00000000;    //接收滤波验收码
            config.AccMask = 0xFFFFFFFF;    //接收滤波屏蔽码
            config.Timing0 = 0x00;
            config.Timing1 = 0x1C;
            config.Filter = 0x01;   //1接收所有帧。2标准帧滤波，3是扩展帧滤波。
            config.Mode = 0x00;     //模式，0表示正常模式，1表示只听模式,2自测模式

            m_canind = (uint)channel;

            switch (baud)
            {
                case "50000":
                    config.Timing0 = 0x09;
                    config.Timing1 = 0x1C;
                    break;

                case "100000":
                    config.Timing0 = 0x04;
                    config.Timing1 = 0x1C;
                    break;

                case "125000":
                    config.Timing0 = 0x03;
                    config.Timing1 = 0x1C;
                    break;

                case "250000":
                    config.Timing0 = 0x01;
                    config.Timing1 = 0x1C;
                    break;

                case "500000":
                    config.Timing0 = 0x00;
                    config.Timing1 = 0x1C;
                    break;

                case "1000000":
                    config.Timing0 = 0x00;
                    config.Timing1 = 0x14;
                    break;

                default:
                    break;
            }

            if (VCI_OpenDevice(m_devtype, m_devind, 0) == 0)
            {
                return false;
            }

            if (VCI_InitCAN(m_devtype, m_devind, m_canind, ref config) == 0)
            {
                return false;
            }

            if (VCI_StartCAN(m_devtype, m_devind, m_canind) == 0)
            {
                return false;
            }

            m_bOpen = true;
            return true;
        }

        /// <summary>
        /// 关闭CAN通道
        /// </summary>
        /// <returns></returns>
        public bool CloseChannel()
        {
            m_bOpen = false;
            VCI_CloseDevice(m_devtype, m_devind);
            return true;
        }

        /// <summary>
        /// CAN获取总线负载率
        /// </summary>
        /// <returns></returns>
        public int BusLoad()
        {
            int busload = 0;
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
        unsafe public bool WriteData(int id, byte[] dat, int dlc, out long time)
        {
            time = 0;
            if (!m_bOpen)
            {
                return false;
            }
            try
            {
                VCI_CAN_OBJ sendobj = new VCI_CAN_OBJ();
                sendobj.RemoteFlag = 0x00;
                sendobj.ExternFlag = 0x00;
                sendobj.ID = (uint)id;
                sendobj.DataLen = System.Convert.ToByte(dlc);
                for (int i = 0; i < dlc; i++)
                {
                    sendobj.Data[i] = dat[i];
                }
                if (VCI_Transmit(m_devtype, m_devind, m_canind, ref sendobj, 1) == 0)
                {
                    return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// CAN读取接收的一帧数据
        /// </summary>
        /// <param name="id"></param>
        /// <param name="dat"></param>
        /// <param name="dlc"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        unsafe public bool ReadData(out int id, ref byte[] dat, out int dlc, out long time)
        {
            id = 0;
            dlc = 0;
            time = 0;
            if (!m_bOpen)
            {
                return false;
            }
            try
            {
                if (VCI_Receive(m_devtype, m_devind, m_canind, ref m_recobj, 1, 0) == 0)
                {
                    return false;
                }
                id = (int)m_recobj.ID;
                dlc = m_recobj.DataLen;
                fixed (VCI_CAN_OBJ* m_recobj1 = &m_recobj)
                {
                    int j = 0;
                    while (j < dlc)
                    {
                        dat[j] = m_recobj1->Data[j];
                        j++;
                    }
                }
                time = m_recobj.TimeStamp / 10;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
