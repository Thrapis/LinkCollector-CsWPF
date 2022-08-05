using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace NewSourceAdapter.Models
{
    public class LogMessage
    {
        public string Message { get; set; }
        public Brush Color { get; set; }

        public LogMessage(string message, Brush color)
        {
            this.Message = message;
            this.Color = color;
        }
    }
}
