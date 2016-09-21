using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uds
{
    class uds_can_trans
    {
        public byte fill_byte = 0x55;

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
            SF_TPDU = 0x00, /* Single frame                 */
            FF_TPDU = 0x10, /* First frame                  */
            CF_TPDU = 0x20, /* Consecutive frame            */
            FC_TPDU = 0x30, /* Flow control frame           */
            FC_OVFL_PDU = 0x32, /* Flow control frame           */

            /* LS bits - SF_DL */
            SF_DL_MAX_BYTES = 0x07, /* SF Max Data Length */
            SF_DL_MASK = 0x07, /* number diagnostic data bytes */
            SF_DL_MASK_LONG = 0x0F, /* change to check the 4 bits for testing, number diagnostic data bytes */

            /* LS bits - FF_DL */
            FF_EX_DL_MASK = 0x0F, /* Extended data length         */

            /* LS bits - CF_SN */
            CF_SN_MASK = 0x0F, /* Sequence number mask         */
            CF_SN_MAX_VALUE = 0x0F, /* Max value of sequence number */

            /* LS bits - FC Saatus */
            FC_STATUS_CONTINUE = 0x00, /* Flow control frame, CONTINUE */
            FC_STATUS_WAIT = 0x01, /* Flow control frame, WAIT */
            FC_STATUS_OVERFLOW = 0x02, /* Flow control frame, OVERFLOW */
            FC_STATUS_MASK = 0x0F,
        };

        public int N_As = 25;

        public int N_Ar = 25;

        public int N_Bs = 75;

        public int N_Cr = 150;

        public int FC_BS_MAX_VALUE = 0;
        public int FC_ST_MIN_VALUE = 20;

        public int CF_SN_MAX_VALUE = 15;
        public int SF_DL_MAX_BYTES = 7;

        private int N_Br;
        private int N_Cs;

        const int beginning_seq_number = 1;

        /*
        ** Time to wait for the tester to send a FC frame in response
        ** to a FF(wait for flow control frame time out).
        **  N_As + N_Bs = 25 +75 = 100ms
        */
        private int FC_WAIT_TIMEOUT = 100;//N_As + N_Bs + 50;

        /* wait for Consecutive frame time out
        ** N_Cr < 150ms
        */
        private int CF_WAIT_TIMEOUT = 150;//N_Cr; //(N_Cr - 10))

        private int TX_RX_MAX_TP_BYTES = 0x1FFF;

        readonly int TPCI = 0;
        readonly int DL = 1;
        readonly int BS = 1;
        readonly int STmin = 2;

        private struct Rx_Message
        {
            byte[] buffer;
            int length;
            int size;
            int sequence_num;
            int next_buff_offset;
            bool receive_finish;
        };

        public int tx_id;
        public int rx_id;
        private byte[] tx_frame = new byte[8];
        private byte[] rx_frame = new byte[8];

        private int _length;
        public int length
        {
            get { return length; }
        }

        private int next_tx_byte = 0;
        private int ptr_index = 0;
        private byte next_sequence_num;
        
        private int sequence_num;
        
        private byte[] _buffer;

        public byte[] buffer
        {
            get { return _buffer; }
        }

        private class tp_state
        {
            public bool TX_RX_IDLE = false;
            public bool TX_FC_TPDU = false;
            public bool TX_LAST_FRAME_ERROR = false;
            public bool RX_IN_PROGRESS = false;
            public bool TX_WAIT_FC = false;
            public bool TX_IN_PROGRESS = false;
        }

        private tp_state can_tp_sts = new tp_state();


        int s_u8_cf_stmin_time = 0x00;
        int s_u8_block_size = 0x00;             /* BS(Block Size) in a flow Control Frame */
        int s_u8_cf_stmin_wait_time = 0x00;     /* STmin Time in Flow Control Frame */
        int s_u8_fc_wait_time = 0x00;           /* Wait for FC when has sent FF */
        bool s_b_fc_wait_timeout_disable = false;
        int s_u8_cf_wait_time = 0x00;
        bool f_overflowFlag = false;

        can_driver can = new can_driver();

        public bool CanTrans_TxMsg(byte[] msg)
        {
            if (msg.Length == 0 || msg.Length > TX_RX_MAX_TP_BYTES - 2)
            {
                return false;
            }

            //if ((can_tp_sts & COMM_STATUS_MASK) != TX_RX_IDLE)
            //{
            //    return false;
            //}

            /*
            ** Set the tx_in_progress bit...it will be cleared when TX is done.
            */
            can_tp_sts.TX_IN_PROGRESS = true;
            can_tp_sts.TX_LAST_FRAME_ERROR = false;

            /*
            ** Assign fields in the control structure to initiate TX, then TX the
            ** appropriate frame type.
            */
            next_tx_byte = 1;
            _length = msg.Length;
            _buffer = new byte[_length];
            Array.Copy(msg, _buffer, _length);
            ptr_index = 0;

            if (_length > 7)
            {
                CanTrans_TxFrame(FrameType.TX_FF);
            }
            else
            {
                CanTrans_TxFrame(FrameType.TX_SF);
            }
            return true;
        }

        private void CanTrans_TxFrame(FrameType frame_type)
        {
            int tx_farme_index = 0;
            int tx_data_bytes = 0;

            if (can_tp_sts.TX_LAST_FRAME_ERROR == false)
            {
                tx_frame = new byte[8] { fill_byte, fill_byte, fill_byte, fill_byte, fill_byte, fill_byte, fill_byte, fill_byte };

                /*
                ** Place control bytes into the frame.
                */
                switch (frame_type)
                {
                    case FrameType.TX_SF: /* single frame */
                        tx_frame[TPCI] = (byte)((byte)PCI.SF_TPDU | _length);
                        tx_data_bytes = _length;
                        tx_farme_index = 1;
                        break;

                    case FrameType.TX_FF: /* first frame */
                        tx_frame[TPCI] = (byte)((byte)PCI.FF_TPDU | (_length >> 8) & 0x0F);
                        tx_frame[DL] = (byte)(_length & 0xFF);
                        tx_data_bytes = SF_DL_MAX_BYTES - 1;
                        tx_farme_index = 2;
                        next_sequence_num = beginning_seq_number;
                        s_b_fc_wait_timeout_disable = false;
                        break;

                    case FrameType.TX_CF: /* conscutive frame */
                        tx_frame[TPCI] = (byte)((byte)PCI.CF_TPDU | next_sequence_num);
                        tx_farme_index = 1;
                        tx_data_bytes = (_length - next_tx_byte + 1);
                        if (tx_data_bytes > SF_DL_MAX_BYTES)
                        {
                            tx_data_bytes = SF_DL_MAX_BYTES;
                        }
                        next_sequence_num = (byte)((next_sequence_num + 1) % (CF_SN_MAX_VALUE + 1));

                        break;

                    case FrameType.TX_FC: /* single frame */
                        if (f_overflowFlag == true)
                        {
                            tx_frame[TPCI] = (byte)PCI.FC_OVFL_PDU;
                        }
                        else
                        {
                            tx_frame[TPCI] = (byte)PCI.FC_TPDU;
                        }
                        tx_frame[BS] = (byte)FC_BS_MAX_VALUE;
                        tx_frame[STmin] = (byte)FC_ST_MIN_VALUE;
                        tx_data_bytes = 0;
                        break;

                    default:
                        return;
                }

                while (tx_data_bytes != 0)
                {
                    tx_frame[tx_farme_index++] = _buffer[ptr_index++];
                    tx_data_bytes--;
                }

            }
            if (can.WriteData(tx_id, tx_frame, 8) == true)
            {
                can_tp_sts.TX_LAST_FRAME_ERROR = false;
                rx_frame[TPCI] = 0;
                /*
                ** Verify if the data has been completely transfered. If not, set flag to
                ** transfer CF frames. (For FC frames, s_cantp_tx_info is not used and there
                ** should not be a CF frame after a FC frame.)
                */
                if (_length >= next_tx_byte && frame_type != FrameType.TX_FC)
                {
                    can_tp_sts.TX_IN_PROGRESS = true;

                    if (frame_type == FrameType.TX_FF)
                    {
                        can_tp_sts.TX_WAIT_FC = true;
                        s_u8_fc_wait_time = FC_WAIT_TIMEOUT; /* start flow control wait timer */
                    }
                }
                else
                {
                    can_tp_sts.TX_IN_PROGRESS = false;
                }
            }
            else
            {
                /* user specific action incase transmission request is not successful */
                can_tp_sts.TX_LAST_FRAME_ERROR = true;
            }
        }
    }


}
