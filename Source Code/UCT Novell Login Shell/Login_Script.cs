using System;
using Novell.Directory.Ldap;


namespace UCT_Novell_Login_Shell
{
	/// <summary>
	/// Summary description for Login_Script.
	/// </summary>
	class Login_Script
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			//
			// TODO: Add code to start application here
			//
			if (args.Length != 5)
			{
				Console.WriteLine("Incorrect usage of this application has been detected.");
				Console.WriteLine();
				Console.WriteLine("[Usage: UCT Novell Login Shell.exe <username> <password> <context> <group membership> <error level>]");
				Console.WriteLine("where:");
				Console.WriteLine(" - username: CN value to search on (e.g. clotter)");
				Console.WriteLine(" - password: password related to the <username>");
				Console.WriteLine(" - context: context container to search within (e.g. com.main.uct)");
				Console.WriteLine(" - group membership: desired attribute (e.g. CF_ALLSTAFF)");
				Console.WriteLine(" - error level: (full) (minimal) (none) denotes the level of status reporting");
			}
			else
			{
				string result = "";
				Login_Script currentobject = new Login_Script();
				result = currentobject.Login_Function(args[0], args[1], args[2], args[3], args[4]);
				Console.Write(result);
			}
			
		}

		private string Login_Function(string username, string password, string context, string group, string error)
		{
			string[] result = new string[6];
			result[0] = "Result: 00 - Failure. Unspecified Reason";
			result[1] = "Result: 01 - Failure. Unable to Connect to any LDAP Server";
			result[2] = "Result: 02 - Failure. User does not exist in Given Context";
			result[3] = "Result: 03 - Failure. Incorrect or Expired Password for Given User";
			result[4] = "Result: 04 - Failure. User is not a member of Given Group";
			result[5] = "Result: 11 - Success. Authentication Complete";

			username = username.ToLower();
			password = password.ToLower();

			try
			{
				Message_Handler(error,"Retrieving Username");
				Message_Handler(error,"Retrieving Password");
				Message_Handler(error,"Retrieving Context");
				
				string tdn = "";
				string[] tempdn = context.Split('.');
				System.Collections.IEnumerator srunner = tempdn.GetEnumerator();
				while(srunner.MoveNext())
				{
					if (srunner.Current.Equals("uct") == false)
						tdn = tdn + "OU="+ srunner.Current + ",";
					else
						tdn = tdn + "O="+ srunner.Current + ",";
				}
				tdn = tdn.Remove(tdn.Length-1,1);

				
				
				Message_Handler(error,"Retrieving Group");

				string userDN = "CN=" + username + "," + tdn;
			
				string ldapHost;
				int ldapPort;
				int ldapVersion = LdapConnection.Ldap_V3;
				ldapPort = LdapConnection.DEFAULT_PORT;

				string[] server = {"",""};
				server[0] = "rep1.uct.ac.za";
				server[1] = "rep2.uct.ac.za";
				
				ldapHost = server[0];
				
				LdapConnection ldapConn;
					
				bool scb = false;
				try				
				{
					Message_Handler(error,"Attempting Primary LDAP Connection");
					ldapConn = new LdapConnection();
					ldapConn.Connect(ldapHost,ldapPort);
					//ldapConn.Bind(ldapVersion,userDN,password);
					scb = true;
				}
				catch(System.Exception except)
				{
					ldapConn = null;
					Error_Handler(error,except);
					scb = false;
				}
				if (scb == false)
				{
					try				
					{
						Message_Handler(error,"Attempting Secondary LDAP Connection");
						ldapConn = new LdapConnection();
						ldapHost = server[1];
						ldapConn.Connect(ldapHost,ldapPort);
						//ldapConn.Bind(ldapVersion,userDN,password);
						scb = true;
					}
					catch(System.Exception excepted)
					{
						ldapConn = null;
						Error_Handler(error,excepted);
						scb = false;
					}	
				}
				if (scb == false)
				{
					return result[1];		
				}
		Message_Handler(error,"Locating User Object in given Context");

				LdapSearchQueue queue=ldapConn.Search(tdn,LdapConnection.SCOPE_SUB,
					//"objectClass=*",
					//"CN=" + Username.Text.ToUpper(),				
					"CN=" + username,
					null,		
					false,
					(LdapSearchQueue) null,
					(LdapSearchConstraints) null );
LdapMessage message;
				bool gcorrect = false;
				bool userfound = false;
				while ((message = queue.getResponse()) != null)
				{
					if (message is LdapSearchResult)
					{
						LdapEntry entry = ((LdapSearchResult) message).Entry;
						if (entry.DN.ToLower().StartsWith("cn="+username) == true)
							userfound = true;

						LdapAttributeSet attributeSet =  entry.getAttributeSet();
												
												System.Collections.IEnumerator ienum = attributeSet.GetEnumerator();
												while(ienum.MoveNext())
												{
													LdapAttribute attribute=(LdapAttribute)ienum.Current;
													string attributeName = attribute.Name;
													if (attributeName.Equals("groupMembership") == true)
												{
													System.Collections.IEnumerator ienum2 = attribute.StringValues;
														
													while(ienum2.MoveNext())
													{
														if (ienum2.Current.ToString().ToLower().StartsWith("cn="+group.ToLower()) == true)
															gcorrect = true;
													}
												}
													
												}
					}
				}
				if (userfound == false)
				{				
					Message_Handler(error,"Disconnecting from LDAP server");
					ldapConn.Disconnect();
					return result[2];		
				}
				
		Message_Handler(error,"Checking User Password");

				LdapAttribute attr = new LdapAttribute("userPassword", password);
				bool correct = ldapConn.Compare(userDN, attr);

				if (correct == false)
				{
					Message_Handler(error,"Disconnecting from LDAP server");
					ldapConn.Disconnect();
					return result[3];		
				}
				ldapConn.Bind(ldapVersion,userDN,password);

				Message_Handler(error,"Checking Group Membership");



				if (gcorrect == false)
				{
					Message_Handler(error,"Disconnecting from LDAP server");
					ldapConn.Disconnect();
					return result[4];		
				}


				Message_Handler(error,"Disconnecting from LDAP server");
				ldapConn.Disconnect();
return result[5];
				




			}
			catch(System.Exception except)
			{
				Error_Handler(error,except);			
			}
			return result[0];
		}

		private void Error_Handler(string error_level, System.Exception except)
		{
			switch(error_level)
			{
				case "full":
					Console.WriteLine("Error: " + except.ToString());			
					break;
				case "minimal":
					Console.WriteLine("Error: " + except.Message.ToString());			
					break;
				case "none":					
					break;
				default:
					Console.WriteLine("Error: " + except.Message.ToString());			
					break;
			}
			
		}

		private void Message_Handler(string error_level, string except)
		{
			switch(error_level)
			{
				case "full":
					Console.WriteLine("Status: " + except.ToString());			
					break;
				case "minimal":
					Console.WriteLine("Status: " + except.ToString());			
					break;
				case "none":					
					break;
				default:
					Console.WriteLine("Status: " + except.ToString());			
					break;
			}
		}

		private void Error_Handler(string error_level, string except)
		{
			switch(error_level)
			{
				case "full":
					Console.WriteLine("Error: " + except.ToString());			
					break;
				case "minimal":
					Console.WriteLine("Error: " + except.ToString());			
					break;
				case "none":					
					break;
				default:
					Console.WriteLine("Error: " + except.ToString());			
					break;
			}
		}

	

	}
}
