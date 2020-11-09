using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace W3CLogReader.Core.Objects
{
    public class Log
    {
        public DateTime DateTime { get; set; }
        public string Method { get; set; }
        public string Uri { get; set; }
        public string UriQuery { get; set; }
        public string Port { get; set; }
        public string Username { get; set; }
        public string ClientIp { get; set; }
        public string UserAgent { get; set; }
        public string Status { get; set; }


        public TreeViewItem GetDetails()
        {
            TreeViewItem treeView = new TreeViewItem();
            treeView.Header = "Failed Exchange login on - " + DateTime;

            treeView.Items.Add("User Agent: " + UserAgent);
            treeView.Items.Add("Date Time: " + DateTime);
            treeView.Items.Add("Client IP: " + ClientIp);
            treeView.Items.Add("Status: " + Status);
            treeView.Items.Add("URL: " + Uri);
            treeView.Items.Add("URL Query: " + UriQuery);

            return treeView;
        }
    }
}
