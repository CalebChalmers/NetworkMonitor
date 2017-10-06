using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkMonitor
{
    public class ClosingMessage
    {
    }

    public class FatalErrorMessage
    {
        public string Message { get; set; }

        public FatalErrorMessage(string message)
        {
            Message = message;
        }

        public override string ToString()
        {
            return Message;
        }
    }
}
