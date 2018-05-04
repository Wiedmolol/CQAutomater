using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Forms;
namespace CQFollowerAutoclaimer
{
    static class ExtensionMethods
    {
        public static void SynchronizedInvoke(this ISynchronizeInvoke sync, Action action)
        {
            // If the invoke is not required, then invoke here and get out.
            if (!sync.InvokeRequired)
            {
                // Execute action.
                action();

                // Get out.
                return;
            }

            // Marshal to the required context.
            sync.Invoke(action, new object[] { });
        }

        public static T Pop<T>(this List<T> l)
        {
            var temp = l[0];
            l.RemoveAt(0);
            return temp;
        }

        public static string getText(this Control c)
        {
            if (c.InvokeRequired)
            {
                return (string)c.Invoke(new Func<String>(() => getText(c)));
            }
            else
            {
                string varText = c.Text;
                return varText;
            }
        }

        public static int getValue(this NumericUpDown c)
        {
            if (c.InvokeRequired)
            {
                return (int)c.Invoke(new Func<int>(() => getValue(c)));
            }
            else
            {
                int val = (int)c.Value;
                return val;
            }
        }

        public static void setText(this Control c, string s)
        {
            if (c.InvokeRequired)
            {
                c.Invoke((MethodInvoker)(() => c.Text = s));
            }
            else
            {
                c.Text = s;
            }
        }

        public static bool getCheckState(this CheckBox c)
        {
            if (c.InvokeRequired)
            {
                return (bool)c.Invoke(new Func<bool>(() => getCheckState(c)));
            }
            else
            {
                bool varText = c.Checked;
                return varText;
            }
        }


    }
}
