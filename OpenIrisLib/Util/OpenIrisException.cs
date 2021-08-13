using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace OpenIris
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class OpenIrisException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        public OpenIrisException()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public OpenIrisException(string message) : base(message)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public OpenIrisException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        protected OpenIrisException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
