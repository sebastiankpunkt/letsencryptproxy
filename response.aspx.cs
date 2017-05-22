using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;
using System.Text;
using System.Net;
using System.Configuration;

namespace LetsEncryptProxy
{
	public partial class response : System.Web.UI.Page
	{
		List<string> WhiteListedHosts = new List<string>();
		static string LetsEncryptDirectories = "/.well-known/acme-challenge/";
		static bool EnableLogging = false;
		protected void Page_Load(object sender, EventArgs e)
		{
			// Get the list of whitelisted host names from web.config
			WhiteListedHosts = ConfigurationManager.AppSettings["DnsHostWhiteList"].Split(';').ToList();

			// Get from web.config if logging is enabled. If true, you might consider change the logging directory (see notes in WriteLog())
			bool.TryParse(ConfigurationManager.AppSettings["EnableLogging"].ToString(), out EnableLogging);

			string userHost = string.Empty;
			string rawUrl = string.Empty;
			string dnsSafeHost = string.Empty;
			bool isLetsEncrypt = false;

			if (Request.Url != null)
			{
				if (!string.IsNullOrEmpty(Request.Url.DnsSafeHost))
				{
					dnsSafeHost = Request.Url.DnsSafeHost;
				}

				if (!string.IsNullOrEmpty(Request.RawUrl))
				{
					rawUrl = Request.RawUrl;
				}

				if(!string.IsNullOrEmpty(Request.UserHostAddress))
				{
					userHost = Request.UserHostAddress;
				}

				// check if rawurl begins with /.well-known/acme-challenge/ 
				isLetsEncrypt = rawUrl.ToLower().StartsWith(LetsEncryptDirectories);
			}
			string ResponseString = "";

			// check if dnsSafeHost is whitelisted
			bool hostNameIsEnabled = HostNameIsEnabled(dnsSafeHost);
			if (isLetsEncrypt && hostNameIsEnabled)
			{
				if (rawUrl.IndexOf('/') > -1)
				{
					string token = rawUrl.Substring(rawUrl.LastIndexOf('/'));
					if (!string.IsNullOrEmpty(token))
					{
						token = token.Replace("/", "").Trim();
						//check if url token length is 43
						if (token.Length == 43)
						{
							// reads the Key from the token file on client
							ResponseString = GetKey(dnsSafeHost, token);
						}
						else
						{
							WriteLog("Token has wrong length (" + token.Length + ", should be 43): " + token, true);
						}
					}
					else
					{
						WriteLog("Token is null or empty", true);
					}
				}
				else
				{
					WriteLog("rawurl is has no /", true);
				}
			}
			else if(!hostNameIsEnabled)
			{
				WriteLog("Host " + dnsSafeHost + " is not whitelisted in web.config", true);
			}

			LogVisit(dnsSafeHost, rawUrl, userHost, isLetsEncrypt, ResponseString);
			if(isLetsEncrypt)
			{
				Response.Write(ResponseString);
				Response.End();
			}
		}

		/// <summary>
		/// Checks if hostname is whitelisted (in WhiteListedHosts from web.config) 
		/// </summary>
		/// <param name="hostName">host name to check (case ignored)</param>
		/// <returns></returns>
		bool HostNameIsEnabled(string hostName)
		{
			if (!string.IsNullOrEmpty(hostName))
			{
				hostName = hostName.ToLower();

				foreach (string host in WhiteListedHosts)
				{
					if (host=="*")
					{
						return true;
					}
					if(hostName==host.ToLower())
					{
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Gets the token file content from the client
		/// </summary>
		/// <param name="host">Host name</param>
		/// <param name="token">Token (file name)</param>
		/// <returns></returns>
		string GetKey(string host, string token)
		{
			string key = string.Empty;

			// the LAN internal path to the token file on the client
			string url = "http://" + host + LetsEncryptDirectories + token;

			using (System.Net.WebClient wc = new System.Net.WebClient())
			{
				try
				{
					// try to fetch the token file content from the client via HTTP
					key = wc.DownloadString(url);
				}
				catch (Exception e)
				{
					WriteLog(e.ToString() +" "+ url, true);
				}

				// Some web server configurations only allow HTTP connections. So if HTTP doesn't work, try HTTPS.
				if(string.IsNullOrEmpty(key))
				{
					url = url.Replace("http://", "https://");

					try
					{
						// try to fetch the token file content from the client via HTTPS
						key = wc.DownloadString(url);
					}
					catch (Exception e)
					{
						WriteLog(e.ToString() + " " + url, true);
					}
				}

				//check if key has a length other than 87 (which means it would be invalid)
				if (string.IsNullOrEmpty(key) || key.Length != 87)
				{
					key = string.Empty;
					WriteLog("Key has wrong length (" + key.Length + ", should be 87): " + key, true);
				}
			}

			return key;
		}

		void LogVisit(string DnsSafeHost, string RawUrl, string UserHost, bool IsLetsEncrypt, string ResponseString = "")
		{
			StringBuilder sb = new StringBuilder();

			sb.Append(DnsSafeHost);
			sb.Append("\t");

			sb.Append(RawUrl);
			sb.Append("\t");

			sb.Append(UserHost);

			if(!string.IsNullOrEmpty(ResponseString))
			{
				sb.Append("\t");
				sb.Append(ResponseString);
			}

			string toLog = sb.ToString();
			if(!string.IsNullOrEmpty(toLog))
			{
				WriteLog(toLog, IsLetsEncrypt);
			}
		}

		static object loglock = new object();

		void WriteLog(string s, bool IsLetsEncrypt)
		{
			if (EnableLogging)
			{
				string prefix = string.Empty;
				if (IsLetsEncrypt)
				{
					prefix = "letsencrypt_";
				}

				// For security reasons you should not use a public log file path. Change it to a non relative. The Application of course needs write access to that directory.
				string dirPath = Server.MapPath("~/Logs/");

				// Static path example:
				//string dirPath = @"C:\inetpub\logs\letsencryptproxy\";

				string fileName = prefix + "log_" + DateTime.Now.ToString("ddMMyyyy") + ".txt";
				string fullPath = Path.Combine(dirPath, fileName);
				if (!string.IsNullOrEmpty(s))
				{
					lock (loglock)
					{
						using (System.IO.TextWriter writeFile = new StreamWriter(fullPath, true))
						{
							writeFile.WriteLine(DateTime.Now.ToString() + "\t" + s);
							writeFile.Flush();
							writeFile.Close();
						}
					}
				}
			}
		}
	}
}