using System;
using System.Collections.Generic;
using System.Text;

namespace BtmI2p.MiscUtils
{
    public static class EnumException
    {
	    public static EnumException<T1> Create<T1>(
            T1 val,
            string message = "",
            object tag = null,
            Exception innerException = null
            ) where T1 : struct, IConvertible
        {
            return new EnumException<T1>(
                val,
                message,
                tag,
                innerException
            );
        }
    }
    public class EnumException<T1> : Exception 
        where T1 : struct, IConvertible
    {
        public T1 ExceptionCode { get; set; }
        public object Tag { get; set; }
		public string InnerMessage => _message;
        private readonly string _message;
        public EnumException(
            T1 val,
            string message = "",
            object tag = null,
            Exception innerException = null
        )
            : base(message, innerException)
        {
            if(!typeof(T1).IsEnum)
                throw new ArgumentException("T1 is not enum");
            if (string.IsNullOrWhiteSpace(message))
                message = $"{typeof(T1).Name}.{val}";
            ExceptionCode = val;
            Tag = tag;
            _message = message;
        }

        public override string Message
        {
            get
            {
                var s = new StringBuilder();
                s.Append(string.Format(
                    "EnumException<{0}> code {1}", 
                    typeof(T1), 
                    ExceptionCode
                ));
                if (!string.IsNullOrWhiteSpace(_message))
                    s.AppendFormat("; message '{0}'", _message);
                return s.ToString();
            }
        }

        public class EnvironmentInfo
        {
            public class VarInfo
            {
                public string VarName { get; set; }
                public Type VarType { get; set; }
                public object VarValue { get; set; }
            }
            public List<VarInfo> MethodParams = new List<VarInfo>();
            public List<VarInfo> LocalVariables = new List<VarInfo>();
        }
    }
}
