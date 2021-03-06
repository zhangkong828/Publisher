﻿using SuperSocket.SocketBase.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Publisher.Core.Server
{
    public class NetReceiveFilter : IReceiveFilter<NetRequestInfo>
    {
        public int LeftBufferSize { get; }

        public IReceiveFilter<NetRequestInfo> NextReceiveFilter { get; }

        public FilterState State { get; }

        /// <summary>
        /// 数据包解析
        /// </summary>
        /// <param name="readBuffer">接收缓冲区</param>
        /// <param name="offset">接收到的数据在缓冲区的起始位置</param>
        /// <param name="length">本轮接收到的数据长度</param>
        /// <param name="toBeCopied">为接收到的数据重新创建一个备份而不是直接使用接收缓冲区</param>
        /// <param name="rest">接收缓冲区未被解析的数据</param>
        /// <returns></returns>
        public NetRequestInfo Filter(byte[] readBuffer, int offset, int length, bool toBeCopied, out int rest)
        {
            //当你在接收缓冲区中找到一条完整的请求时，你必须返回一个你的请求类型的实例.
            //当你在接收缓冲区中没有找到一个完整的请求时, 你需要返回 NULL.
            //当你在接收缓冲区中找到一条完整的请求, 但接收到的数据并不仅仅包含一个请求时，设置剩余数据的长度到输出变量 "rest". 输出参数 "rest", 如果它大于 0, 此 Filter 方法 将会被再次执行, 参数 "offset" 和 "length" 会被调整为合适的值.

            Encoding encoding = Encoding.UTF8;

            rest = 0;
            //6+1+4+2+2+1+4 小于协议头，不是完整数据包
            if (length <= 20)
                return null;

            byte[] data = new byte[length];
            Buffer.BlockCopy(readBuffer, offset, data, 0, length);

            NetPacket myData = new NetPacket();
            myData.Start = encoding.GetString(data, 0, 6);//开始符号 6字节
            myData.Type = data[6];//消息类型 1字节
            myData.Lenght = BitConverter.ToUInt32(data, 7);//主体消息长度 4字节  6 + 1

            if (length < myData.Lenght + 20)
                return null;

            myData.PacketIndex = BitConverter.ToUInt16(data, 11);//数据包索引 2字节 6+1+4
            myData.PacketCount = BitConverter.ToUInt16(data, 13);//数据包总数 2字节 6+1+4+2
            myData.Flag = data[15];//flag 1字节
            myData.Body = new byte[myData.Lenght];//主体消息
            Buffer.BlockCopy(data, 16, myData.Body, 0, (int)myData.Lenght);

            myData.End = encoding.GetString(data, (int)(16 + myData.Lenght), 4);//结束符号 4字节

            if (myData.Start != "!Start" || myData.End != "$End")
                return null;

            rest = (int)(length - (20 + myData.Lenght));//未处理数据

            return new NetRequestInfo(myData.Type.ToString(), myData);
        }

        public void Reset()
        {
        }
    }
}
