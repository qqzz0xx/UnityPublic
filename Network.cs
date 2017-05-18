using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

enum NetState
{
    CLOSED,
    CONNECTING,
    CONNECTED,
    DISCONNECTED,
    TIMEOUT,
    ERROR,
}


class Network : IDisposable
{
    public event Action<NetState> NetStateChangeListener;
    public event Action<int, object> MsgResListener;

    private Socket m_Socket;
    private string m_IP;
    private int m_Port;
    private NetState m_NeState;
    private ManualResetEvent timeoutEvent = new ManualResetEvent(false);
    private int timeoutMSec = 8000;    //connect timeout count in millisecond

    private Transports m_Transports;

    public string IP
    {
        get;
    }

    public int Port
    {
        get;
    }

    public Network(string server, int port)
    {
    }

    public void Init(string server, int port)
    {
        m_IP = server;
        m_Port = port;

    }

    public void Connet()
    {
        timeoutEvent.Reset();
        OnChangeNetState(NetState.CONNECTING);

        IPHostEntry hostEntry = Dns.GetHostEntry(m_IP);
        IPAddress ipAddress = null;
        try
        {
            foreach (IPAddress address in hostEntry.AddressList)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    ipAddress = address;
                    break;
                }
            }
        }
        catch (System.Exception ex)
        {
            OnChangeNetState(NetState.ERROR);
        }
        IPEndPoint ie = new IPEndPoint(ipAddress, m_Port);
        m_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        m_Socket.BeginConnect(ie, new AsyncCallback((result) =>
        {

            try
            {
                m_Socket.EndConnect(result);
                m_Transports = new Transports(m_Socket, Disconnet, Dispatch);
                OnChangeNetState(NetState.CONNECTED);
                StartRecv();
            }
            catch (System.Exception ex)
            {
                OnChangeNetState(NetState.ERROR);
                Dispose();
            }
            finally
            {
                timeoutEvent.Set();
            }


        }), m_Socket);

        if (timeoutEvent.WaitOne(timeoutMSec, false))
        {
            if (m_NeState == NetState.CONNECTING)
            {
                OnChangeNetState(NetState.TIMEOUT);
                Dispose();
            }
        }
    }

    public void StartRecv()
    {
        if (m_NeState != NetState.DISCONNECTED) return;
        m_Transports.Recv();
    }
    public void Send(int msgid, byte[] data)
    {
        if (m_NeState != NetState.DISCONNECTED) return;
        m_Transports.Send(msgid, data);
    }

    private void Dispatch(int msgid, byte[] body)
    {
        if (MsgResListener != null)
            MsgResListener.Invoke(msgid, body);
    }

    public void Disconnet()
    {
        OnChangeNetState(NetState.DISCONNECTED);
        Dispose();
    }

    private void OnChangeNetState(NetState state)
    {
        m_NeState = state;
        if (NetStateChangeListener != null)
        {
            NetStateChangeListener.Invoke(state);
        }
    }

    #region IDisposable Support
    private bool disposedValue = false; // 要检测冗余调用

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: 释放托管状态(托管对象)。
                try
                {
                    m_Transports.Close();
                    m_Socket.Shutdown(SocketShutdown.Both);
                    m_Socket.Close();
                    m_Socket = null;
                }
                catch (Exception)
                {
                    //todo : 有待确定这里是否会出现异常，这里是参考之前官方github上pull request。emptyMsg
                }
            }

            // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
            // TODO: 将大型字段设置为 null。

            disposedValue = true;
        }
    }

    // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
    // ~Network() {
    //   // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
    //   Dispose(false);
    // }

    // 添加此代码以正确实现可处置模式。
    public void Dispose()
    {
        // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
        Dispose(true);
        // TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
        // GC.SuppressFinalize(this);
    }
    #endregion
}

