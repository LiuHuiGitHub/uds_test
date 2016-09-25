using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MyFormat;

namespace Uds
{
    class uds_trans
    {
        public byte fill_byte = 0x55;

        public int tx_id;
        public int rx_id;

        /*
        ** Frame types arranged in numerical order for efficient switch statement
        ** jump tables.
        */
        private enum FrameType
        {
            TX_SF = 0, /* Single Frame */
            TX_FF = 1, /* First Frame */
            TX_CF = 2, /* Consecutive Frame */
            TX_FC = 3 /* Flow Control Frame */
        };

        /*
        ** Masks for the PCI(Protcol Control Information) byte. 
        ** The MS bit contains the frame type.
        ** The LS bit is mapped differently, depending on frame type, as follows:
        **  SF: DL (number of diagnostic bytes NOT including the PCI byte; only the
        **       3 LS bits are used).
        **  FF: XDL (extended data length; always be 0.)
        **  CF: Sequence number,4 bits, max value:15.
        **  FC: Flow Status. The value of FS shall be set to zero that means that
        **      the tester is ready to receive a maximum number of CF.
        */
        private enum PCI /* Don't change these values, these must be  */
        {
            /* MS bits -  Frame Type */
            FRAME_TYPE_MASK = 0xF0,
            SF_TPDU = 0x00,             /* Single frame                 */
            FF_TPDU = 0x10,             /* First frame                  */
            CF_TPDU = 0x20,             /* Consecutive frame            */
            FC_TPDU = 0x30,             /* Flow control frame           */
            FC_OVFL_PDU = 0x32,         /* Flow control frame           */

            /* LS bits - SF_DL */
            SF_DL_MAX_BYTES = 0x07,     /* SF Max Data Length */
            SF_DL_MASK = 0x07,          /* number diagnostic data bytes */
            SF_DL_MASK_LONG = 0x0F,     /* change to check the 4 bits for testing, number diagnostic data bytes */

            /* LS bits - FF_DL */
            FF_EX_DL_MASK = 0x0F,       /* Extended data length         */

            /* LS bits - CF_SN */
            CF_SN_MASK = 0x0F,          /* Sequence number mask         */
            CF_SN_MAX_VALUE = 0x0F,     /* Max value of sequence number */

            /* LS bits - FC Saatus */
            FC_STATUS_CONTINUE = 0x00,  /* Flow control frame, CONTINUE */
            FC_STATUS_WAIT = 0x01,      /* Flow control frame, WAIT */
            FC_STATUS_OVERFLOW = 0x02,  /* Flow control frame, OVERFLOW */
            FC_STATUS_MASK = 0x0F,
        };

        private int N_As = 25;

        private int N_Ar = 25;

        private int N_Bs = 75;

        private int N_Cr = 150;

        private int FC_BS_MAX_VALUE = 0;
        private int FC_ST_MIN_VALUE = 20;

        private int CF_SN_MAX_VALUE = 15;
        private int SF_DL_MAX_BYTES = 7;

        private int N_Br;
        private int N_Cs;

        /*
        ** Time to wait for the tester to send a FC frame in response
        ** to a FF(wait for flow control frame time out).
        **  N_As + N_Bs = 25 +75 = 100ms
        */
        private int FC_WAIT_TIMEOUT;//N_As + N_Bs + 50;

        /* 
        ** wait for Consecutive frame time out
        ** N_Cr < 150ms
        */
        private int CF_WAIT_TIMEOUT;//N_Cr; //(N_Cr - 10))

        private int RX_MAX_TP_BYTES = 0xFFF;


        public uds_trans()
        {
            can_rx_info.frame = new byte[8];
            can_tx_info.frame = new byte[8];

            FC_WAIT_TIMEOUT = N_As + N_Bs + 50; /* N_As + N_Bs + 50 */
            CF_WAIT_TIMEOUT = N_Cr;             /* (N_Cr - 10)) */
        }

        readonly int beginning_seq_number = 1;
        readonly int TPCI_Byte = 0;
        readonly int DL_Byte = 1;
        readonly int BS_Byte = 1;
        readonly int STminByte = 2;
        
        private class tx_info
        {
            public bool tx_rx_idle = false;
            public bool tx_fc_tpdu = false;
            public bool tx_last_frame_error = false;
            public bool rx_in_progress = false;
            public bool tx_wait_fc = false;
            public bool tx_in_progress = false;

            public int tx_block_size = 0;           /* BS(Block Size) in a flow Control Frame */
            public int tx_stmin_time = 20;
            public int tx_cf_stmin_wait_time = 20;   /* STmin Time in Flow Control Frame */
            public int tx_fc_wait_time = 0;         /* Wait for FC when has sent FF */
            
            public int lenght;
            public int offset;
            public int next_seq_num;
            public byte[] buffer;
            public byte[] frame;
        }

        private class rx_info
        {
            public bool rx_info_init = false;
            public bool rx_msg_rcvd = false;
            public bool rx_msg_invl_tpci = false;
            public bool rx_msg_pad_not_zero = false;
            public bool tx_aborted = false;
            public bool rx_msg_invl_len = false;
            public bool rcv_msg_is_msng = false;    /* the message has been declared missing */
            public bool msg_never_rcvd = false;     /* if the message has never been received to be used by application level software */
            public bool rcv_msg_is_new = false;     /* the message is new */

            public int rx_cf_wait_time = 0;
            public bool rx_fc_wait_timeout_disable = false;
            public bool rx_overflow = false;

            public int lenght;
            public int offset;
            public int next_seq_num;
            public byte[] buffer;
            public byte[] frame;
        }

        private tx_info can_tx_info = new tx_info();
        private rx_info can_rx_info = new rx_info();
        
        public class FarmsEventArgs : EventArgs
        {
            public int id = 0;
            public int dlc = 0;
            public byte[] dat = new byte[8];
            public override string ToString()
            {
                return id.ToString("X3") + " "
                           + dlc.ToString("X1") + " "
                           + dat.HexToStrings(" ");
            }
        }

        private FarmsEventArgs e_args = new FarmsEventArgs();
        public event EventHandler EventTxFarms;
        public event EventHandler EventRxFarms;

        private void WriteData(int id, byte[] dat, int dlc)
        {
            if(EventTxFarms != null)
            {
                e_args.id = id;
                e_args.dlc = dlc;
                Array.Copy(dat, e_args.dat, dlc);
                EventTxFarms(this, e_args);
            }
        }

        private void ReadData(int id, byte[] dat, int dlc)
        {
            if (EventRxFarms != null)
            {
                e_args.id = id;
                e_args.dlc = dlc;
                Array.Copy(dat, e_args.dat, dlc);
                EventRxFarms(this, e_args);
            }
        }

        can_driver can = new can_driver();

        public void Can_Trans_RxFrams(int id, byte[] dat, int dlc)
        {
            if (id == rx_id && dlc == 8)
            {
                Array.Copy(dat, can_rx_info.frame, 8);
                ReadData(id, dat, dlc);
            }
        }

        byte[] tx_msg = new byte[0];

        public bool CanTrans_TxMsg(byte[] msg)
        {
            if (msg.Length == 0 
                || msg.Length > RX_MAX_TP_BYTES - 2
                || tx_msg.Length != 0
                )
            {
                return false;
            }
            tx_msg = msg;
            tx_msg = new byte[msg.Length];
            Array.Copy(msg, tx_msg, msg.Length);
            return true;
        }

        private void Tx_Msg()
        {
            if(tx_msg.Length == 0)
            {
                return;
            }
            /*
            ** Set the tx_in_progress bit...it will be cleared when TX is done.
            */
            can_tx_info.tx_in_progress = true;
            can_tx_info.tx_last_frame_error = false;

            /*
            ** Assign fields in the control structure to initiate TX, then TX the
            ** appropriate frame type.
            */
            can_tx_info.offset = 0;
            can_tx_info.lenght = tx_msg.Length;
            can_tx_info.buffer = new byte[tx_msg.Length];
            Array.Copy(tx_msg, can_tx_info.buffer, can_tx_info.lenght);
            can_tx_info.offset = 0;
            tx_msg = new byte[0];

            if (can_tx_info.lenght <= SF_DL_MAX_BYTES)
            {
                CanTrans_TxFrame(FrameType.TX_SF);
            }
            else
            {
                CanTrans_TxFrame(FrameType.TX_FF);
            }
        }

        private void CanTrans_TxFrame(FrameType frame_type)
        {
            int tx_farme_index = 0;
            int tx_data_bytes = 0;

            if (can_tx_info.tx_last_frame_error == false)
            {
                can_tx_info.frame = new byte[8] { fill_byte, fill_byte, fill_byte, fill_byte, fill_byte, fill_byte, fill_byte, fill_byte };

                /*
                ** Place control bytes into the frame.
                */
                switch (frame_type)
                {
                    case FrameType.TX_SF: /* single frame */
                        can_tx_info.frame[TPCI_Byte] = (byte)((byte)PCI.SF_TPDU | can_tx_info.lenght);
                        tx_data_bytes = can_tx_info.lenght;
                        tx_farme_index = 1;
                        break;

                    case FrameType.TX_FF: /* first frame */
                        can_tx_info.frame[TPCI_Byte] = (byte)((byte)PCI.FF_TPDU | (can_tx_info.lenght >> 8) & 0x0F);
                        can_tx_info.frame[DL_Byte] = (byte)(can_tx_info.lenght & 0xFF);
                        tx_data_bytes = SF_DL_MAX_BYTES - 1;
                        tx_farme_index = 2;
                        can_tx_info.next_seq_num = beginning_seq_number;
                        can_rx_info.rx_fc_wait_timeout_disable = false;
                        break;

                    case FrameType.TX_CF: /* conscutive frame */
                        can_tx_info.frame[TPCI_Byte] = (byte)((byte)PCI.CF_TPDU | can_tx_info.next_seq_num);
                        tx_farme_index = 1;
                        tx_data_bytes = (can_tx_info.lenght - can_tx_info.offset);
                        if (tx_data_bytes > SF_DL_MAX_BYTES)
                        {
                            tx_data_bytes = SF_DL_MAX_BYTES;
                        }
                        can_tx_info.next_seq_num = (can_tx_info.next_seq_num + 1) % (CF_SN_MAX_VALUE + 1);

                        break;

                    case FrameType.TX_FC: /* single frame */
                        if (can_rx_info.rx_overflow == true)
                        {
                            can_tx_info.frame[TPCI_Byte] = (byte)PCI.FC_OVFL_PDU;
                        }
                        else
                        {
                            can_tx_info.frame[TPCI_Byte] = (byte)PCI.FC_TPDU;
                        }
                        can_tx_info.frame[BS_Byte] = (byte)FC_BS_MAX_VALUE;
                        can_tx_info.frame[STminByte] = (byte)FC_ST_MIN_VALUE;
                        tx_data_bytes = 0;
                        break;

                    default:
                        return;
                }

                while (tx_data_bytes != 0)
                {
                    can_tx_info.frame[tx_farme_index++] = can_tx_info.buffer[can_tx_info.offset++];
                    tx_data_bytes--;
                }

            }
            if (can.WriteData(tx_id, can_tx_info.frame, 8) == true)
            {
                WriteData(tx_id, can_tx_info.frame, 8);
                can_tx_info.tx_last_frame_error = false;
                can_rx_info.frame[TPCI_Byte] = 0;
                /*
                ** Verify if the data has been completely transfered. If not, set flag to
                ** transfer CF frames. (For FC frames, s_cantp_tx_info is not used and there
                ** should not be a CF frame after a FC frame.)
                */
                if (can_tx_info.lenght > can_tx_info.offset && frame_type != FrameType.TX_FC)
                {
                    can_tx_info.tx_in_progress = true;

                    if (frame_type == FrameType.TX_FF)
                    {
                        can_tx_info.tx_wait_fc = true;
                        can_tx_info.tx_fc_wait_time = FC_WAIT_TIMEOUT; /* start flow control wait timer */
                    }
                }
                else
                {
                    can_tx_info.tx_in_progress = false;
                }
            }
            else
            {
                /* user specific action incase transmission request is not successful */
                can_tx_info.tx_last_frame_error = true;
            }
        }

        public void CanTrans_Manage(int tick)
        {
            Tx_Msg();

            CanTrans_Counter(tick);

            /*
            ** If new message has been received, process it.
            */
            if (can_rx_info.frame[TPCI_Byte] != 0)
            {
                CanTrans_RxStateAnalyse();
                /*clear first rx frame byte to check a new frame next time*/
                can_rx_info.frame[TPCI_Byte] = 0;
            }

            if (can_tx_info.tx_in_progress
                && !can_tx_info.tx_wait_fc
                )
            {
                if (0x00 == can_tx_info.tx_block_size)
                {
                    /* st_min time, received from tester*/
                    if (0x00 == can_tx_info.tx_stmin_time)
                    {
                        CanTrans_TxFrame(FrameType.TX_CF);
                    }
                    else
                    {
                        /* st_min time, received from tester is not 0 */
                        if (0x00 == can_tx_info.tx_cf_stmin_wait_time)
                        {
                            CanTrans_TxFrame(FrameType.TX_CF);
                            can_tx_info.tx_cf_stmin_wait_time = can_tx_info.tx_stmin_time;
                        }
                    }
                }
                else if (can_tx_info.tx_block_size > 1)
                {
                    if (0x00 == can_tx_info.tx_stmin_time)
                    {
                        CanTrans_TxFrame(FrameType.TX_CF);
                        if (!can_tx_info.tx_last_frame_error)
                        {
                            can_tx_info.tx_block_size--;
                        }
                    }
                    else
                    {
                        if (0x00 == can_tx_info.tx_cf_stmin_wait_time)
                        {
                            CanTrans_TxFrame(FrameType.TX_CF);
                            if (!can_tx_info.tx_last_frame_error)
                            {
                                can_tx_info.tx_block_size--;
                            }

                            /* start stmin time,interval of consecutive frame */
                            can_tx_info.tx_cf_stmin_wait_time = can_tx_info.tx_stmin_time;
                        }
                    }

                    if (can_tx_info.tx_block_size <= 1)
                    {
                        can_tx_info.tx_wait_fc = true;

                        /* start flow control wait timer */
                        can_tx_info.tx_fc_wait_time = FC_WAIT_TIMEOUT;
                    }
                }
            }
            else if (can_tx_info.tx_fc_tpdu)
            {
                CanTrans_TxFrame(FrameType.TX_FC);
                can_tx_info.tx_fc_tpdu = false;

                /*start to counter the CF wait time*/
                can_rx_info.rx_cf_wait_time = CF_WAIT_TIMEOUT;
            }

            if (can_tx_info.tx_in_progress
                && can_tx_info.tx_wait_fc
                )
            {
                /* wait for flow control frame time out! */
                if (can_tx_info.tx_fc_wait_time == 0)
                {
                    can_tx_info.tx_in_progress = false;
                    can_tx_info.tx_wait_fc = false;
                    can_tx_info.tx_last_frame_error = false;
                }
            }
            if (can_tx_info.rx_in_progress == true
                && !can_tx_info.tx_fc_tpdu
              )
            {
                if (0x00 == can_rx_info.rx_cf_wait_time)
                {
                    can_rx_info.tx_aborted = true;
                    /*
                    ** wait for consecutive frame Time out,abort Rx.
                    */
                    can_tx_info.rx_in_progress = false;

                    /* 
                    ** When Time out occurs, ECU has to send negative
                    ** resp(71) for the first frame.First frame is already copied in to
                    ** g_cantp_can_rx_info.msg buffer but message length is not yet copied.
                    ** So assign data length as First Frame length and set RX_MSG_RCVD
                    ** flag.This flag indicates to a new message has come.
                    */
                    can_rx_info.lenght = SF_DL_MAX_BYTES - 1;
                    can_rx_info.rx_msg_rcvd = true;
                }
            }
        }

        private void CanTrans_Counter(int tick)
        {
            /* interval of consecutive frame, STmin = 10ms, separation time */
            if (can_tx_info.tx_cf_stmin_wait_time > 0)
            {
                if (can_tx_info.tx_cf_stmin_wait_time > tick)
                {
                    can_tx_info.tx_cf_stmin_wait_time -= tick;
                }
                else
                {
                    can_tx_info.tx_cf_stmin_wait_time = 0;
                }
            }

            /* N_Bs, flow control frame wait time out, 75ms*/
            if (can_tx_info.tx_fc_wait_time > 0)
            {
                if (can_tx_info.tx_fc_wait_time > tick)
                {
                    can_tx_info.tx_fc_wait_time -= tick;
                }
                else
                {
                    can_tx_info.tx_fc_wait_time = 0;
                    can_rx_info.rx_fc_wait_timeout_disable = true;
                }
            }

            /* N_Cr,consecutive frame wait time out, 75ms*/
            if (can_rx_info.rx_cf_wait_time > tick)
            {
                can_rx_info.rx_cf_wait_time -= tick;
            }
            else
            {
                can_rx_info.rx_cf_wait_time = 0;
            }
        }

        private void CanTrans_RxStateAnalyse()
        {
            PCI flow_control_sts;
            int data_length = 0x00;

            /* single frame */
            if ((can_rx_info.frame[TPCI_Byte] & (byte)PCI.FRAME_TYPE_MASK) == (byte)PCI.SF_TPDU)
            {
                /* As per 15765-2 network layer spec when SF_DL is 0 or greater
                ** than 7, just ignore it.
                */
                data_length = (can_rx_info.frame[TPCI_Byte] & (byte)PCI.SF_DL_MASK_LONG);

                can_tx_info.tx_in_progress = false;
                can_tx_info.tx_wait_fc = false;
                can_tx_info.rx_in_progress = false;
                can_tx_info.tx_last_frame_error = false;

                if ((data_length == 0)
                 || (data_length > SF_DL_MAX_BYTES)
                  )
                {
                    return;
                }
                can_rx_info.lenght = data_length;
                can_rx_info.buffer = new byte[can_rx_info.lenght];
                /*
                ** Copy the frame to the RX buffer. Clear the RX_IN_PROGRESS bit
                ** (SF frame) will abort multi-frame transfer.
                */
                Array.Copy(can_rx_info.frame, 1, can_rx_info.buffer, 0, can_rx_info.lenght);

                can_rx_info.rx_msg_rcvd = true;
            }
            /* first frame */
            else if ((can_rx_info.frame[TPCI_Byte] & (byte)PCI.FRAME_TYPE_MASK) == (byte)PCI.FF_TPDU)
            {
                data_length = ((int)(can_rx_info.frame[TPCI_Byte] & (byte)PCI.FF_EX_DL_MASK) << 8)
                                         + can_rx_info.frame[DL_Byte];

                can_rx_info.rx_fc_wait_timeout_disable = false;
                can_rx_info.lenght = data_length;
                can_rx_info.buffer = new byte[can_rx_info.lenght];

                /*
                ** Clear the RX buffer, copy first frame to RX buffer and initiate RX.
                */
                Array.Copy(can_rx_info.frame, 2, can_rx_info.buffer, 0, SF_DL_MAX_BYTES - 1);
                can_rx_info.next_seq_num = beginning_seq_number;
                can_rx_info.offset = SF_DL_MAX_BYTES - 1;

                can_tx_info.tx_in_progress = false;
                can_tx_info.tx_wait_fc = false;
                can_tx_info.rx_in_progress = true;

                /* set flag to send flow control frame */
                can_tx_info.tx_fc_tpdu = true;
            }
            /* Consecutive Frame */
            else if ((((can_rx_info.frame[TPCI_Byte] & (byte)PCI.FRAME_TYPE_MASK) == (byte)PCI.CF_TPDU)
                /* Don't accept consecutive frame until flow control frame sent by ECU */
                && (!can_tx_info.tx_fc_tpdu)
                /* Don't accept consecutive frame if we are sending CF*/
                && (!can_tx_info.tx_in_progress))
                )
            {
                /*
                ** Ignore frame unless RX in progress.
                */
                if (can_tx_info.rx_in_progress)
                {
                    /*
                    ** Verify the sequence number is as expected.
                    */
                    if ((can_rx_info.frame[TPCI_Byte] & (byte)PCI.CF_SN_MASK) == can_rx_info.next_seq_num)
                    {
                        data_length = can_rx_info.lenght - can_rx_info.offset;
                        /*
                        **  Last frame in message?
                        */
                        if (data_length <= SF_DL_MAX_BYTES)
                        {
                            Array.Copy(can_rx_info.frame, 1, can_rx_info.buffer, can_rx_info.offset, data_length);

                            can_tx_info.rx_in_progress = false;
                            can_rx_info.rx_msg_rcvd = true;
                        }
                        else
                        {
                            /*
                            ** not the last frame,copy bytes to RX buffer and
                            ** continue RXing.
                            */
                            Array.Copy(can_rx_info.frame, 1, can_rx_info.buffer, can_rx_info.offset, SF_DL_MAX_BYTES);

                            can_rx_info.next_seq_num = (can_rx_info.next_seq_num + 1) % (CF_SN_MAX_VALUE + 1);
                            can_rx_info.offset += SF_DL_MAX_BYTES;
                            can_rx_info.rx_cf_wait_time = CF_WAIT_TIMEOUT;
                        }
                    }
                    else
                    {
                        /*
                        ** Invalid sequence number...abort Rx.As a diagnostic measure, 
                        ** consideration was given to send an FC frame here, but not done.
                        */
                        can_tx_info.rx_in_progress = false;
                        /* 
                        ** When Invalid sequence number is received, ECU has to send 
                        ** negative resp for the first frame.so set RX_MSG_RCVD flag.
                        ** This flag indicates to DiagManager as new message has come.
                        */
                        can_rx_info.tx_aborted = true;
                        can_rx_info.rx_msg_rcvd = true;
                    }
                }
            }
            /* flow control frame */
            else if ((can_rx_info.frame[TPCI_Byte] & (byte)PCI.FRAME_TYPE_MASK) == (byte)PCI.FC_TPDU)
            {
                if (can_tx_info.tx_wait_fc)
                {
                    /*
	                ** Receive Flow Status(FS) for Transmiting the CF Frames.
	                ** The value of FS shall be set to zero that means that the
	                ** tester is ready to receive a maximum number of CF.
	                */
                    flow_control_sts = PCI.FC_STATUS_CONTINUE;
                    if ((can_rx_info.frame[TPCI_Byte] & (byte)PCI.FC_STATUS_MASK) != 0x00)
                    {
                        /* Flow Status(FS)
	                    ** 0: Continue to send(CTS)
	                    ** 1: wait(WT)
	                    ** 2: Overflow(OVFLW)
	                    */

                        flow_control_sts = (PCI)(can_rx_info.frame[TPCI_Byte] & (byte)PCI.FC_STATUS_MASK);
                    }

                    /*
                    ** Receive the BS and ST min time for Transmiting the CF Frames.
                    */
                    if (can_rx_info.frame[BS_Byte] != 0x00)
                    {
                        can_tx_info.tx_block_size = can_rx_info.frame[BS_Byte] + 1;
                    }
                    else
                    {
                        can_tx_info.tx_block_size = 0x00;
                    }

                    if ((can_rx_info.frame[STminByte] & 0x7F) != 0x00)
                    {
                        /* 
                        ** Valid Range for STMin timeout is 0 - 127ms.
                        */
                        can_tx_info.tx_stmin_time = (can_rx_info.frame[STminByte] & 0x7F) + 5;   /* extend the delay time */
                    }
                    else
                    {
                        can_tx_info.tx_stmin_time = 20;
                    }
                    if ((flow_control_sts == PCI.FC_STATUS_CONTINUE)
                     && (can_rx_info.rx_fc_wait_timeout_disable == false)
                      )
                    {
                        can_tx_info.tx_wait_fc = false;
                        can_tx_info.tx_fc_wait_time = 0;
                    }
                    else if (flow_control_sts == PCI.FC_STATUS_WAIT)
                    {
                        can_tx_info.tx_fc_wait_time = FC_WAIT_TIMEOUT;  /* if wait, we will wait another time */
                    }
                    else if (flow_control_sts == PCI.FC_STATUS_OVERFLOW)
                    {
                        /* do nothing here, if over flow, we will stop sending 
                           any message until we got new cmd */
                        can_tx_info.tx_fc_wait_time = 1;   /* exit after 10ms */
                    }
                    else
                    {
                        /* do nothing here, if over flow, we will stop sending 
                           any message until we got new cmd */
                        can_tx_info.tx_fc_wait_time = 1;   /* exit after 10ms */
                    }
                }
            }
        }
    }
}
