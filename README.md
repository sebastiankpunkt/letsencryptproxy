# letsencryptproxy
LetsEncryptProxy is small IIS web application for forwarding LetsEncrypt authorization to your LAN clients.

LetsEncryptProxy version 0.9, 22.05.2017

Feel free to modify it. It's not a big thing, so do whatever you want with it.


How it works:

LetsEncrypt usually can only be used on a web server which can be connected from the Internet on Port 80.
The ACME client creates a file on the web server containing a given key. After the creation the LetsEncrypt servers downloads the file for validation. Only if the token file is available and the key is correct it will deliver the certificate to the client.
If you want to use valid LetsEncrypt certificates in your own LAN you can use some workarounds. One of these workarounds is this small web application.
The LetsEncryptProxy accepts all well formed LetsEncrypt-Requests, checks the host name against a white list, will itself ask the client for the token file and deliver the key to the requesting LetsEncrypt-Server.

Normal LetsEncrypt validation:
Web server <-------> LetsEncrypt

LetsEncrypt validation over LetsEncryptProxy:
Client <-------> LetsEncryptProxy <-------> LetsEncrypt


What it needs: 

- IIS, accessible from Internet on HTTP port 80 with URL Rewrite module
- Domain with wildcard record (*.yourdomain) pointing to your IIS IP
- DNS server for your LAN using your public domain (*.yourdomain)
- LetsEncrypt client software as usual with outgoing internet access, together with a web server on the clients

I'm using it with several Windows clients and Synology NAS but it should work with every client based on ACME challenge response.

The whole LetsEncryptProxy directory is a Visual Studio 2015 project. 


Installation:

1. Save the content of the directory on your web server.

2. Create the Web application:

If the server is hosting a web site on the domain:
Create a new application directory for your default web site in IIS with the alias name ".well-known". Select the LetsEncrypt directory as path.

OR

If the server does not host a web site on the domain:
Create a new web site in IIS. Select LetsEncrypt directory as path


3. The application pool of the web application needs the correct permissions to the LetsEncryptProxy directory: Read access to the main directory and it's sub folders, Read and write access to the Logs directory.

4. Open the URL http://yourdomain/.well-known in your browser. The text "LetsEncryptProxy is running" should appear.

5. Enter all A records for your internal LAN PCs (Host.yourdomain pointing to the client PCs IP)

6. Enter all host names of your LAN host in the web.config file as value for DnsHostWhiteList. For more than one entry use semicolon (;) for separation. If you would want to allow all host names, use * (would not recommend that).

7. Logging is enabled by default. After testing you should consider disabling it or change the logging path due to security reasons.

Done. The LetsEncryptProxy should work now.

If you have problems, questions, remarks, feel free to contact me at letsencryptproxy@macmac.de
