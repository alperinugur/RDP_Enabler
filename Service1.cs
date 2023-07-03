using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using Microsoft.Win32;
using System.Net.Mail;
using System.Threading;
using System.Net.Http;
using System.IO;

namespace RDP_Enabler
{
    public partial class Service1 : ServiceBase
    {
        private System.Timers.Timer _timer;
        private System.Timers.Timer _informTimer;

        public System.DateTime _timerStartTime = DateTime.Now;

        private readonly int _port = 10010; // Replace with your desired port number
        private readonly string email_smtp_server = "smtp.yourserver.com";  // Replace this with your E-Mail SMTP server 
        private readonly string email_sending_address = "yourname@yourserver.com";  // Replace this with your E-mail address which you will use to send
        private readonly string email_username = "yourname@yourserver.com";  // Replace this with your E-mail username to authenticate which you will use to send
        private readonly string email_password = "yourpassword";  // Replace this with your password which you will use to send
        private readonly int SMTP_PORT = 587;  // This is 587 or 25 depending on your ISP.
        private readonly string recivers_emailaddress = "reciver@domain.com";  // Replace this with the email address of the reciever. You can use the same sending address if you want to recieve in the same mailbox.


        private TcpListener _tcpListener;

        public bool couldNotSendMail = false;

        public Service1()
        {
            InitializeComponent();
        }

        protected override async void OnStart(string[] args)
        {
            await Task.Delay(3000);
            // Thread.Sleep(3000);
            _tcpListener = new TcpListener(IPAddress.Any, _port);
            _tcpListener.Start();
            _tcpListener.BeginAcceptTcpClient(HandleTcpClient, _tcpListener);

            _timer = new System.Timers.Timer();
            _timer.Interval = 10 * 60 * 1000; // 10 minutes in milliseconds
            _timer.AutoReset = true;
            _timer.Elapsed += OnTimerElapsed;
            _timer.Start();

            _informTimer = new System.Timers.Timer();
            _informTimer.Interval = 10 * 1000; // 10 seconds to try sending Inform
            _informTimer.AutoReset = true;
            _informTimer.Elapsed += InformOnTimerElapsed;
            _informTimer.Start();

            DisableRdpAccess();
            InformStart();
        }

        private void InformOnTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // Restart timer
            InformStart();
        }

        private async void InformStart()
        {
            string publicIpAddress = await GetPublicIPaddress();
            Console.WriteLine($"Public IP address: {publicIpAddress}");
            if (publicIpAddress == null) { publicIpAddress = "NO EXTERNAL IP"; }

            IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress serverIpAddress = hostEntry.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);

            if (serverIpAddress == null)
            {
                // Handle the case where the server's IP address could not be determined. You could also add a logger function here.
            }
            else
            {
                try
                {
                    SendStartupMail(serverIpAddress.ToString() + " -  PublicIP:  " + publicIpAddress.ToString(), _port);
                    _informTimer.Stop();
                }
                catch
                {
                    // Console.Write("CANNOT SEND EMAIL");
                    _informTimer.Start();
                }
            }
        }

        private void OnTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // Check if there has been any RDP activity in the past 15 minutes
            // If not, disable RDP access here
            DisableRdpAccess();
            // Restart timer
            _timer.Start();

        }

        protected override void OnStop()
        {
            _tcpListener.Stop();

        }

        private static async Task<string> GetPublicIPaddress()
        {
            var client = new HttpClient();
            try
            {
                var response = await client.GetAsync("https://api.ipify.org");  // You can use this or another site to get your public IP address
                response.EnsureSuccessStatusCode();
                var ipAddress = await response.Content.ReadAsStringAsync();
                return ipAddress;
            }
            catch
            {
                // hello
            }
            return null;
        }

        private void HandleTcpClient(IAsyncResult ar)
        {
            var tcpListener = (TcpListener)ar.AsyncState;
            var tcpClient = tcpListener.EndAcceptTcpClient(ar);

            // Extract client IP address
            var clientEndpoint = (IPEndPoint)tcpClient.Client.RemoteEndPoint;
            var clientIpAddress = clientEndpoint.Address;
            string clientIPstring = clientIpAddress.ToString();

            // Enable RDP access here
            EnableRdpAccess();

            var writer = new StreamWriter(tcpClient.GetStream());
            writer.WriteLine("HTTP/1.1 302 Found");
            writer.WriteLine("Location: http://www.google.com");    // This is to fake the attackers. They are redirected to this page but at the same time, the port is now open for 10 minutes for RDP
            writer.WriteLine("Connection: close");
            writer.Flush();
            // Close the TCP client connection
            tcpClient.Close();
            // Resume listening for incoming connections
            _tcpListener.BeginAcceptTcpClient(HandleTcpClient, _tcpListener);
            TimeSpan elapsedTime = DateTime.Now - _timerStartTime;
            int elapsedSeconds = (int)elapsedTime.TotalSeconds;
            if (elapsedSeconds >= 40)
            {
                try
                {
                    SendAlertemail(clientIPstring);
                }
                catch
                {
                    _timerStartTime = DateTime.Now;
                }
                _timerStartTime = DateTime.Now;
            }
            _timer.Stop();
            _timer.Start();
            tcpClient.Close();
            _tcpListener.BeginAcceptTcpClient(HandleTcpClient, _tcpListener);
        }

        private static void EnableRdpAccess()
        {
            const string keyName = @"SYSTEM\CurrentControlSet\Control\Terminal Server";
            const string valueName = "fDenyTSConnections";
            using (var key = Registry.LocalMachine.OpenSubKey(keyName, true))
            {
                key?.SetValue(valueName, 0, RegistryValueKind.DWord);
            }
        }

        private static void SendAlertemail(string clientIPstring)
        {
            var myVars = new Service1();
            var smtpClient = new SmtpClient(myVars.email_smtp_server, myVars.SMTP_PORT);       
            smtpClient.UseDefaultCredentials = false;
            smtpClient.EnableSsl = false;
            smtpClient.Credentials = new NetworkCredential(myVars.email_username, myVars.email_password);  
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            var mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(myVars.email_sending_address);    
            mailMessage.To.Add(myVars.recivers_emailaddress);                  
            mailMessage.Subject = "ATTENTION -  " + clientIPstring + "  - RDP Access Enabled for 10 minutes.";
            mailMessage.Body = "RDP access has been enabled for 10 minutes by IP:  " + clientIPstring;
            smtpClient.Send(mailMessage);
        }


        private static void SendStartupMail(string serverIPstring, int _portnumber)
        {
            var myVars = new Service1();
            var smtpClient = new SmtpClient(myVars.email_smtp_server, myVars.SMTP_PORT);
            smtpClient.UseDefaultCredentials = false;
            smtpClient.EnableSsl = false;
            smtpClient.Credentials = new NetworkCredential(myVars.email_username, myVars.email_password);
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            var mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(myVars.email_sending_address);
            mailMessage.To.Add(myVars.recivers_emailaddress);
            mailMessage.Subject = "RDP DISABLED by RESTART of Service. The PC is at:  " + serverIPstring + "  - RDP Access Disabled.";
            mailMessage.Body = "RDP access has been disabled in intenal IP:  " + serverIPstring + "\r\n\r\nEnable Port:  " + _portnumber + "\r\n\r\n";   // You are being informed about the IP in case of your PC is stolen :)
            smtpClient.Send(mailMessage);
        }

        private static void DisableRdpAccess()
        {
            const string keyName = @"SYSTEM\CurrentControlSet\Control\Terminal Server";
            const string valueName = "fDenyTSConnections";
            using (var key = Registry.LocalMachine.OpenSubKey(keyName, true))
            {
                key?.SetValue(valueName, 1, RegistryValueKind.DWord);
            }
        }

    }
}
