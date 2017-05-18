using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

class StateObject
{
    public const int BufferSize = 1024;
    public byte[] Buffer = new byte[BufferSize];
}

public class Transports
{
    enum TransportState
    {
        eClose,
        ePaserHead,
        ePaserBody,
    }

    //依赖注入
    private Action onDisconnect = null;
    private Action<int, byte[]> onDispatch = null;
    private Socket m_Socket;

    //
    private StateObject m_StateObject = new StateObject();
    private TransportState m_TransportState;
    private bool m_isOnSending = false;
    private bool m_isOnReceiving = false;

    //proto
    private const int s_MsgHeadLen = 6;
    private byte[] m_MsgHead = new byte[s_MsgHeadLen];
    private byte[] m_MsgBody;
    private int m_iHadCopyHead;
    private int m_iHadCopyBody;

    private int m_iMsgID;
    private int m_iSeqID;
    private int m_iMsgBodyLen;

    public Transports(Socket socket, Action onDisconnet, Action<int, byte[]> onDispatch)
    {
        this.m_Socket = socket;
        this.onDisconnect = onDisconnet;
        this.onDispatch = onDispatch;
        m_TransportState = TransportState.ePaserHead;
    }
    public void Send(int msgid, byte[] body)
    {
        if (m_TransportState == TransportState.eClose) return;
        m_isOnSending = true;
        byte[] buffer = new byte[body.Length + 4];
        buffer[0] = Convert.ToByte(msgid & 0xFF);
        buffer[1] = Convert.ToByte(msgid >> 8 & 0xFF);
        buffer[2] = Convert.ToByte(body.Length & 0xFF);
        buffer[3] = Convert.ToByte(body.Length >> 8 & 0xFF);
        WriteBytes(body, 0, buffer, 4, body.Length);
        m_Socket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(SendCallback), m_StateObject);
    }
    private void SendCallback(IAsyncResult result)
    {
        if (m_TransportState == TransportState.eClose) return;
        m_Socket.EndSend(result);
        m_isOnSending = false;
    }
    public void Recv()
    {
        m_isOnReceiving = true;
        m_Socket.BeginReceive(m_StateObject.Buffer, 0, m_StateObject.Buffer.Length, SocketFlags.None, 
            new AsyncCallback(RecvCallback), m_StateObject);
    }
    private void RecvCallback(IAsyncResult reslut)
    {
        if (m_TransportState == TransportState.eClose) return;

        StateObject state = (StateObject)reslut.AsyncState;
        Socket socket = m_Socket;

        try
        {
            int length = socket.EndReceive(reslut);
            m_isOnReceiving = false;
            if (length > 0)
            {
                ProBytes(state.Buffer, 0, length);

                if (m_TransportState != TransportState.eClose)
                    Recv();
            }
            else
            {
                if (onDisconnect != null) onDisconnect();
            }
        }
        catch (System.Exception ex)
        {
            if (onDisconnect != null) onDisconnect();
        }
    }

    private void ProBytes(byte[] buffer, int offset, int length)
    {
        if (buffer == null || length <= 0 || offset >= length) return;
        int iDataLen = length - offset;
        while (iDataLen > 0)
        {
            if (m_TransportState == TransportState.ePaserHead)
            {
                int iCopyHead = s_MsgHeadLen - m_iHadCopyHead;
                if (iDataLen >= iCopyHead)
                {
                    WriteBytes(buffer, offset, m_MsgHead, m_iHadCopyHead, iCopyHead);
                    offset += iCopyHead;
                    iDataLen -= iCopyHead;
                    m_iHadCopyHead = 0;

                    m_iMsgID = m_MsgHead[0] + (m_MsgHead[1] << 8);
                    m_iSeqID = m_MsgHead[2] + (m_MsgHead[3] << 8);
                    m_iMsgBodyLen = m_MsgHead[4] + (m_MsgHead[5] << 8);

                    if (m_iMsgBodyLen > 0)
                    {
                        m_iHadCopyBody = 0;
                        m_MsgBody = new byte[m_iMsgBodyLen];
                        m_TransportState = TransportState.ePaserBody;
                    }
                    else
                    {
                        if (onDispatch != null)
                        {
                            onDispatch(m_iMsgID, m_MsgBody);
                        }
                        ResetPaserState();
                    }
                }
                else
                {
                    WriteBytes(buffer, offset, m_MsgHead, m_iHadCopyHead, iDataLen);
                    m_iHadCopyHead += iDataLen;
                    return;
                }
            }
            else if (m_TransportState == TransportState.ePaserBody)
            {
                int iCopyBody = m_iMsgBodyLen - m_iHadCopyBody;
                if (iDataLen >= iCopyBody)
                {
                    WriteBytes(buffer, offset, m_MsgBody, m_iHadCopyBody, iCopyBody);
                    offset += iCopyBody;
                    iDataLen -= iCopyBody;
                    if (onDispatch != null)
                    {
                        onDispatch(m_iMsgID, m_MsgBody);
                    }
                    ResetPaserState();
                }
                else
                {
                    WriteBytes(buffer, offset, m_MsgBody, m_iHadCopyBody, iDataLen);
                    m_iHadCopyBody += iDataLen;
                    return;
                }

            }
        }
    }
    private void ResetPaserState()
    {
        m_TransportState = TransportState.ePaserHead;
        m_iHadCopyBody = 0;
        m_iHadCopyHead = 0;
    }
    private void WriteBytes(byte[] src, int srcOffset, byte[] dst, int dstOffset, int length)
    {
        Buffer.BlockCopy(src, srcOffset, dst, dstOffset, length);
    }

    public void Close()
    {
        m_TransportState = TransportState.eClose;
    }
}
