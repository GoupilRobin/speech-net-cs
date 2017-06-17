using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Marvin
{
    public class Utils
    {
        public static byte[] Serialize(object obj)
        {
            MemoryStream mStream = new MemoryStream();
            BinaryFormatter binFormatter = new BinaryFormatter();

            binFormatter.Serialize(mStream, obj);

            return mStream.ToArray();
        }

        public static object Deserialize(byte[] data)
        {
            MemoryStream mStream = new MemoryStream();
            BinaryFormatter binFormatter = new BinaryFormatter();
            
            mStream.Write(data, 0, data.Length);
            mStream.Position = 0;

            return binFormatter.Deserialize(mStream);
        }

        public static T Deserialize<T>(byte[] data)
        {
            return (T) Deserialize(data);
        }
    }
}
