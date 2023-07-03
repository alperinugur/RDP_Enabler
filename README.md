# RDP_Enabler
A service to trigger Remote Desktop Protocol in Windows 10 / Windows 11, to prevent hacking.<br>

Two way authentication, so that RDP attackers will be blocked. <br>


# Requirements
This is tested on Windows 11. I guess it will work fine with Windows 10 also, and maybe lower versions.

# Remote Desktop Protocol Security
This small service works like a 2 step authentication for RDP. When started, it will disable the RDP access to the computer, and wait for a port trigger. <br>
Once the port is triggered, the RDP is accessible for 10 minutes. <br>
To fake the hackers, if the port is reached from a HTTP request, it will redirect to a site. i.e. "www.google.com"

# INSTALLATION
The service is able to send e-mail messages when it is triggered (either by service-start, or port-trigger)<br>
To achieve this, you have to change the variables in Service1.cs file (code view), lines 27-33<br>

Once you build your RDP_Enabler.exe.exe file, run the "install_service.bat" file as administrator.<br>

Alternatively, you can open a CMD prompt as administrator and put the command as follows: <br>

sc create RDP_Enabler binpath=".\bin\Release\RDP_Enabler.exe"    <br>

Change the path as needed. <br>


# USAGE

Install the service and make it "Delayed Start" in service properties. <br>
Use the System credentials (as default)<br>

Whenever you want to connect to your computer via another computer or mobile app (like RD Client in iPhobne), first open a browser.<br>
In the browser window, navigate to address:  http://yourcomputer.yourdomain.com:10010  (change the 10010 if you changed that in the code)<br>
Make sure your port is open for Incoming Ports in Windows Defender.<br>
Also make sure the port is redirected to your computer by your Modem (Port mapping)<br><br>

If all works fine, you will just see the google homepage on your browser, and you will recieve an email at the same time, warning you that the RDP is enabled.<br><br>

Now you have 10 minutes to connect to your computer via RDP service. After 10 minutes, it will disable automatically and warn you that RDP is disabled agian.<br><br>


