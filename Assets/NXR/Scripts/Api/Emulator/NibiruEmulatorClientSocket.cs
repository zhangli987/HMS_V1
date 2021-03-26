using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Nxr.Internal
{
    public class NibiruEmulatorClientSocket : MonoBehaviour
    {
        public const int TCP_HEAD_TAG = 0xABCDE;
        public const int TCP_HEAD_LENGTH = 8;

        public byte[] TCP_HEAD_BYTES;

        public const byte CLIENT_VERSION = 1;

        //=======================================
        public enum RD_MESSAGE
        {
            RD_MESSAGE_HMD_OPTICAL_PARAMETER = 1, // 光学参数+渠道号
            RD_MESSAGE_HMD_POSE = 2, // 旋转+位移
            RD_MESSAGE_CONTROLLER_POSE = 3, // 旋转+位移
            RD_MESSAGE_HMD_STATUS = 4,
            RD_MESSAGE_LIFECYCLE = 5, // 开始结束等生命周期
            RD_MESSAGE_CLIENT_INFO = 6
        }

        public enum ClientLifeCycle
        {
            CLIENT_START = 0,
            CLIENT_PAUSE = 1,
            CLIENT_RESUME = 2,
            CLIENT_DESTROY = 3
        }

        // 6 bytes
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        struct ClientInfoData
        {
            public static readonly int Size = Marshal.SizeOf(typeof(ClientInfoData));
            public int type;
            public byte clientVersion;
            public byte enableControllerDebug;
        }

        // 8 bytes
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        struct Point
        {
            float x;
            float y;
        }

        // 5 bytes
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        struct LifeCycleData
        {
            public static readonly int Size = Marshal.SizeOf(typeof(LifeCycleData));
            public int type;
            public byte lifecycleStatus;
        }

        // 12 bytes
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct HmdStatusData
        {
            public static readonly int Size = Marshal.SizeOf(typeof(HmdStatusData));
            public int type;
            public int controllerType; //0=左手柄，1=右手柄
            public int controllerStatus; // 0=没有手柄连接，1=有手柄连接
        };

        // 81 bytes
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct OpticalConfigData
        {
            public static readonly int Size = Marshal.SizeOf(typeof(OpticalConfigData));
            public int type; // 100

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 50, ArraySubType = UnmanagedType.R4)] // 4 字节有符号浮点
            public float[] opticalConfig; // 长度50

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 20)]
            public byte[] channel; // 长度20，MR0018,VR076,...

            public byte serverVersion; // 服务器端版本号，如：1,2,3.....
        };

        // 16 bytes
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct TrackingQuat
        {
            public static readonly int Size = Marshal.SizeOf(typeof(TrackingQuat));
            public float x;
            public float y;
            public float z;
            public float w;
        };

        // 12 bytes
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct TrackingVector3
        {
            public static readonly int Size = Marshal.SizeOf(typeof(TrackingVector3));
            public float x;
            public float y;
            public float z;
        };

        // 92 bytes
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct ControllerPoseData
        {
            public static readonly int Size = Marshal.SizeOf(typeof(ControllerPoseData));
            public int type;

            [MarshalAsAttribute(UnmanagedType.Struct)]
            public TrackingQuat left_controller_Pose_Orientation;

            [MarshalAsAttribute(UnmanagedType.Struct)]
            public TrackingVector3 left_controller_Pose_Position;

            public int leftControllerButton; // 左手柄键值
            public int leftControllerTouchAction;

            Point leftControllerTouchPosition;

            [MarshalAsAttribute(UnmanagedType.Struct)]
            public TrackingQuat right_controller_Pose_Orientation;

            [MarshalAsAttribute(UnmanagedType.Struct)]
            public TrackingVector3 right_controller_Pose_Position;

            public int rightControllerButton; // 右手柄键值
            public int rightControllerTouchAction;

            Point rightControllerTouchPosition;
        };

        // 32 bytes
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct HmdPoseData
        {
            public static readonly int Size = Marshal.SizeOf(typeof(HmdPoseData));
            public int type;

            [MarshalAsAttribute(UnmanagedType.Struct)]
            public TrackingQuat HeadPose_Pose_Orientation;

            [MarshalAsAttribute(UnmanagedType.Struct)]
            public TrackingVector3 HeadPose_Pose_Position;
        };

        /// <summary>
        /// Convert struct to byte[].
        /// </summary>
        public static byte[] StructToBytes(object structObj, int size)
        {
            IntPtr buffer = Marshal.AllocHGlobal(size);
            try //struct_bytes转换
            {
                Marshal.StructureToPtr(structObj, buffer, false);
                byte[] bytes = new byte[size];
                Marshal.Copy(buffer, bytes, 0, size);
                return bytes;
            }
            catch (Exception ex)
            {
                throw new Exception("Error in StructToBytes ! " + ex.Message);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        /// <summary>
        /// Restore byte[] to the specified struct.The generic type of this function is only used for custom structures.
        /// startIndex：The index of the starting position when copying the array.
        /// length：The number of array elements to be copied.
        /// </summary>
        public static T BytesToStruct<T>(byte[] bytes, int startIndex, int length)
        {
            if (bytes == null) return default(T);
            if (bytes.Length <= 0) return default(T);
            IntPtr buffer = Marshal.AllocHGlobal(length);
            try //struct_bytes转换
            {
                Marshal.Copy(bytes, startIndex, buffer, length);
                return (T) Marshal.PtrToStructure(buffer, typeof(T));
            }
            catch (Exception ex)
            {
                throw new Exception("Error in BytesToStruct ! " + ex.Message);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        public int BytesToInt(byte[] src, int offset)
        {
            int value;
            value = (int) ((src[offset] & 0xFF)
                           | ((src[offset + 1] & 0xFF) << 8)
                           | ((src[offset + 2] & 0xFF) << 16)
                           | ((src[offset + 3] & 0xFF) << 24));
            return value;
        }

        public byte[] IntToBytes(int value)
        {
            byte[] src = new byte[4];
            src[3] = (byte) ((value >> 24) & 0xFF);
            src[2] = (byte) ((value >> 16) & 0xFF);
            src[1] = (byte) ((value >> 8) & 0xFF);
            src[0] = (byte) (value & 0xFF);
            return src;
        }

        private void SendClientInfoData(ClientInfoData data)
        {
            byte[] ba_ClientInfoData = StructToBytes(clientInfoData, ClientInfoData.Size);
            SendDataToServer(ba_ClientInfoData);
        }

        // IP address of the phone, when connected to the PC via USB.
        public static readonly string USB_SERVER_IP = "127.0.0.1";
        private static readonly int kPhoneEventPort = 7004;

        private const int kSocketReadTimeoutMillis = 100;

        //  16ms 请求一次数据
        private const int kMinReconnectIntervalMS = 2;
        private TcpClient phoneMirroringSocket;
        private Thread phoneEventThread;

        private volatile bool shouldStop = false;

        // Flag used to limit connection state logging to initial failure and successful reconnects.
        private volatile bool lastConnectionAttemptWasSuccessful = true;
        private NibiruEmulatorManager phoneRemote;
        private ClientInfoData clientInfoData;

        public bool connected { get; private set; }

        public void Init(NibiruEmulatorManager remote)
        {
            TCP_HEAD_BYTES = IntToBytes(TCP_HEAD_TAG);

            phoneRemote = remote;

            clientInfoData = new ClientInfoData();
            clientInfoData.type = (int) RD_MESSAGE.RD_MESSAGE_CLIENT_INFO;
            clientInfoData.clientVersion = CLIENT_VERSION;
            clientInfoData.enableControllerDebug = NxrViewer.Instance.RemoteController ? (byte) 1 : (byte) 0;
            // Debug.Log("---" + ClientInfoData.Size);

            phoneEventThread = new Thread(phoneEventSocketLoop);
            phoneEventThread.Start();
        }

        private void phoneEventSocketLoop()
        {
            while (!shouldStop)
            {
                long lastConnectionAttemptTime = DateTime.Now.Ticks;
                try
                {
                    phoneConnect();
                }
                catch (Exception e)
                {
                    if (lastConnectionAttemptWasSuccessful)
                    {
                        Debug.LogWarningFormat("{0}\n{1}", e.Message, e.StackTrace);
                        // Suppress additional failures until we have successfully reconnected.
                        lastConnectionAttemptWasSuccessful = false;
                    }
                }

                // Wait a while in order to enforce the minimum time between connection attempts.
                TimeSpan elapsed = new TimeSpan(DateTime.Now.Ticks - lastConnectionAttemptTime);
                int toWaitMS = kMinReconnectIntervalMS - elapsed.Milliseconds;
                if (toWaitMS > 0)
                {
                    // Debug.Log("Thread.Sleep." + toWaitMS);
                    Thread.Sleep(toWaitMS);
                }
            }

            if (shouldStop)
            {
                DoStop();
            }

            if (tcpStream != null)
            {
                tcpStream.Close();
            }

            if (tcpClient != null)
            {
                tcpClient.Close();
            }

            Debug.Log("phoneEventSocketLoop exit");
        }

        TcpClient tcpClient;
        NetworkStream tcpStream;

        private void phoneConnect()
        {
            try
            {
                if (!connected)
                {
                    string addr = USB_SERVER_IP;
                    setupPortForwading(kPhoneEventPort);
                    tcpClient = new TcpClient(addr, kPhoneEventPort);
                    tcpStream = tcpClient.GetStream();
                    tcpStream.ReadTimeout = kSocketReadTimeoutMillis;
                    tcpClient.ReceiveTimeout = kSocketReadTimeoutMillis;

                    connected = true;

                    // 
                    SendClientInfoData(clientInfoData);
                    // Thread.Sleep(120);
                    DoStart();
                    Debug.Log("-------------connected to server");
                }

                ProcessConnection();
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
                connected = false;
                shouldStop = true;
            }
        }

        void HandleRecvData(int type, byte[] data)
        {
            if (type == (int) RD_MESSAGE.RD_MESSAGE_CONTROLLER_POSE)
            {
                ControllerPoseData controllerPoseData = BytesToStruct<ControllerPoseData>(data, 0, data.Length);
                if (phoneRemote.OnControllerPoseDataEvent != null)
                    phoneRemote.OnControllerPoseDataEvent(controllerPoseData);
            }
            else if (type == (int) RD_MESSAGE.RD_MESSAGE_HMD_OPTICAL_PARAMETER)
            {
                OpticalConfigData configData = BytesToStruct<OpticalConfigData>(data, 0, data.Length);
                if (phoneRemote.OnConfigDataEvent != null) phoneRemote.OnConfigDataEvent(configData);
            }
            else if (type == (int) RD_MESSAGE.RD_MESSAGE_HMD_POSE)
            {
                HmdPoseData hmdPoseData = BytesToStruct<HmdPoseData>(data, 0, data.Length);
                if (phoneRemote.OnHmdPoseDataEvent != null) phoneRemote.OnHmdPoseDataEvent(hmdPoseData);
            }
            else if (type == (int) RD_MESSAGE.RD_MESSAGE_HMD_STATUS)
            {
                HmdStatusData hmdStatusData = BytesToStruct<HmdStatusData>(data, 0, data.Length);
                if (phoneRemote.OnHmdStatusEvent != null) phoneRemote.OnHmdStatusEvent(hmdStatusData);
            }
        }

        private void ProcessConnection()
        {
            // 数据结构：头部 + 数据长度 + 数据内容
            try
            {
                // 指示在要读取的 NetworkStream 上是否有可用的数据。一般来说通过判断这个属性来判断NetworkStream中是否有数据
                if (tcpStream.DataAvailable)
                {
                    byte[] tcpHead = new byte[TCP_HEAD_LENGTH];
                    int realLength = tcpStream.Read(tcpHead, 0, TCP_HEAD_LENGTH);
                    if (realLength < TCP_HEAD_LENGTH)
                    {
                        Debug.LogError("TCP HEAD recv failed !!! " + realLength + "<" + TCP_HEAD_LENGTH);
                        return;
                    }

                    int headTag = BytesToInt(tcpHead, 0);
                    if (headTag != TCP_HEAD_TAG)
                    {
                        Debug.LogError("recv HEAD Tag Is Not Same  !!!");
                        return;
                    }

                    int contentLength = BytesToInt(tcpHead, 4);
                    byte[] contentBytes = new byte[contentLength];
                    realLength = tcpStream.Read(contentBytes, 0, contentLength);
                    int total = realLength;
                    while (total < contentLength)
                    {
                        int leftLength = contentLength - total;
                        int length = tcpStream.Read(contentBytes, total, leftLength);
                        total += length;
                    }

                    // 分发内容
                    int type = BytesToInt(contentBytes, 0);
                    HandleRecvData(type, contentBytes);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Recv timeout : " + e.Message.ToString());
            }

            // Debug.Log(DateTime.Now.Ticks + "_TCP=CanRead." + tcpStream.CanRead + " / CanWrite." + tcpStream.CanWrite + " / DataAvailable." + tcpStream.DataAvailable);
        }

        private void setupPortForwading(int port)
        {
            string adbCommand = string.Format("adb forward tcp:{0} tcp:{0}", port);
            System.Diagnostics.Process myProcess = new System.Diagnostics.Process();
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            string processFilename = "CMD.exe";
            string processArguments = @"/k " + adbCommand + " & exit";

            // Debug.Log(adbCommand);
            // See "Common Error Lookup Tool" (https://www.microsoft.com/en-us/download/details.aspx?id=985)
            // MSG_DIR_BAD_COMMAND_OR_FILE (cmdmsg.h)
            int kExitCodeCommandNotFound = 9009; // 0x2331

#else
      string processFilename = "bash";
      string processArguments = string.Format("-l -c \"{0}\"", adbCommand);

      // "command not found" (see http://tldp.org/LDP/abs/html/exitcodes.html)
      int kExitCodeCommandNotFound = 127;
#endif // UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

            System.Diagnostics.ProcessStartInfo myProcessStartInfo =
                new System.Diagnostics.ProcessStartInfo(processFilename, processArguments);
            myProcessStartInfo.UseShellExecute = false;
            myProcessStartInfo.RedirectStandardError = true;
            myProcessStartInfo.CreateNoWindow = true;
            myProcess.StartInfo = myProcessStartInfo;
            myProcess.Start();
            myProcess.WaitForExit();
            // Also wait for HasExited here, to avoid ExitCode access below occasionally throwing InvalidOperationException
            while (!myProcess.HasExited)
            {
                Thread.Sleep(1);
            }

            int exitCode = myProcess.ExitCode;
            string standardError = myProcess.StandardError.ReadToEnd();
            myProcess.Close();

            if (exitCode == 0)
            {
                // Port forwarding setup successfully.
                // Debug.Log("------------------Connect AR Device Successfully");
                return;
            }

            if (exitCode == kExitCodeCommandNotFound)
            {
                // Caught by phoneEventSocketLoop.
                throw new Exception(
                    "Android Debug Bridge (`adb`) command not found." +
                    "\nVerify that the Android SDK is installed and that the directory containing" +
                    " `adb` is included in your PATH environment variable.");
            }

            // Caught by phoneEventSocketLoop.
            throw new Exception(
                String.Format(
                    "Failed to setup port forwarding." +
                    " Exit code {0} returned by process: {1} {2}\n{3}",
                    exitCode, processFilename, processArguments, standardError));
        }


        private void SendDataToServer(byte[] data)
        {
            if (tcpStream == null || !tcpStream.CanWrite) return;

            byte[] realData = new byte[data.Length + TCP_HEAD_LENGTH];
            Array.Copy(TCP_HEAD_BYTES, 0, realData, 0, TCP_HEAD_BYTES.Length);
            int dataLength = data.Length;
            byte[] dataLengthBytes = IntToBytes(dataLength);
            Array.Copy(dataLengthBytes, 0, realData, 4, dataLengthBytes.Length);
            Array.Copy(data, 0, realData, 8, dataLength);

            tcpStream.Write(realData, 0, realData.Length);
            tcpStream.Flush();

            Debug.Log("SendDataToServer : " + data.Length);
        }

        private void DoStart()
        {
            LifeCycleData lifeCycleData = new LifeCycleData();
            lifeCycleData.type = (int) RD_MESSAGE.RD_MESSAGE_LIFECYCLE;
            lifeCycleData.lifecycleStatus = (byte) ClientLifeCycle.CLIENT_START;

            byte[] ba_lifedata = StructToBytes(lifeCycleData, LifeCycleData.Size);
            SendDataToServer(ba_lifedata);
            Debug.Log("DoStart");
        }

        private void DoStop()
        {
            Debug.Log("DoStop");
            LifeCycleData lifeCycleData = new LifeCycleData();
            lifeCycleData.type = (int) RD_MESSAGE.RD_MESSAGE_LIFECYCLE;
            lifeCycleData.lifecycleStatus = (byte) ClientLifeCycle.CLIENT_DESTROY;

            byte[] ba_lifedata = StructToBytes(lifeCycleData, LifeCycleData.Size);
            SendDataToServer(ba_lifedata);
        }

        void OnDestroy()
        {
            shouldStop = true;

            if (phoneMirroringSocket != null)
            {
                phoneMirroringSocket.Close();
                phoneMirroringSocket = null;
            }

            if (phoneEventThread != null)
            {
                phoneEventThread.Join();
            }

            Debug.Log("NibiruEmulator.OnDestroy");
        }
    }
}