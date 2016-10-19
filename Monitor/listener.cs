using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows.Controls;

namespace Monitor
{
    public class TextBoxTraceListener : TraceListener
    {
        private Action<string> _sendStringAction;

        public TextBoxTraceListener(TextBox target)
        {
            Target = target;

            _sendStringAction = delegate(string message)
            {
                // No need to lock text box as this function will only 
                // ever be executed from the UI thread

                Target.AppendText(message);
            };
        }

        public TextBox Target { get; private set; }

        public override void Write(string message)
        {
            try
            {
                Target.Dispatcher.Invoke(_sendStringAction, message);
                //Target.Invoke(_sendStringAction, message);
            }
            catch
            {
                return;
            }
        }

        public override void WriteLine(string message)
        {
            Write(message + Environment.NewLine);
        }

        public override void Write(string message, string category)
        {
            //message = String.Format("{0} - {1}", category, message);

            if ((TraceOutputOptions & TraceOptions.DateTime) == TraceOptions.DateTime)
                message = String.Format("{0:HH:mm:ss.fff} : {1}", DateTime.Now, message);

            Write(message);
        }
    }
}
