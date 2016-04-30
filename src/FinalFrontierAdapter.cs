using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace MyPlugin
{
	public class FinalFrontierAdapter
	{
		private const int DEFAULT_PRESTIGE = -10000;

		// ExternalInterface in Final Frontier
		private object instanceExternalInterface = null;
		// methods of the ExternalInterface
		private MethodInfo methodGetVersion = null;
		private MethodInfo methodRegisterRibbon = null;
		private MethodInfo methodRegisterCustomRibbon = null;
		private MethodInfo methodAwardRibbonToKerbalByCode = null;
		private MethodInfo methodAwardRibbonToKerbalByRibbon = null;
		private MethodInfo methodAwardRibbonToKerbalsByCode = null;
		private MethodInfo methodAwardRibbonToKerbalsByRibbon = null;
		private MethodInfo methodIsRibbonAwardedToKerbalByCode = null;
		private MethodInfo methodIsRibbonAwardedToKerbalByRibbon = null;
		private MethodInfo methodGetMissionsFlownForKerbal = null;
		private MethodInfo methodGetDockingsForKerbal = null;
		private MethodInfo methodResearchForKerbal = null;
		private MethodInfo methodTotalMissionTimeForKerbal = null;
		private MethodInfo methodGetContractsCompletedForKerbal = null;

		protected Type GetType(string name)
		{
			return AssemblyLoader.loadedAssemblies
			   .SelectMany(x => x.assembly.GetExportedTypes())
			   .SingleOrDefault(t => t.FullName == name);
		}

		private MethodInfo GetMethod(Type type, string name, Type[] parameterTypes)
		{
			MethodInfo info = type.GetMethod(name, parameterTypes);
			if (info == null)
			{
				string signature = "";
				foreach (Type t in parameterTypes)
				{
					if (signature.Length > 0) signature = signature + ",";
					signature = signature + t.Name;
				}
				Debug.LogError("failed to register method " + name + "(" + signature + ")");
			}
			return info;
		}

		private MethodInfo GetMethod(Type type, string name)
		{
			MethodInfo info = type.GetMethod(name, BindingFlags.Public | BindingFlags.Instance);
			if (info == null)
			{
				Debug.LogError("failed to register method " + name + "()");
			}
			return info;
		}

		private void RegisterMethods(Type type)
		{
			methodGetVersion = GetMethod(type, "GetVersion");
			methodRegisterRibbon = GetMethod(type, "RegisterRibbon");
			methodRegisterCustomRibbon = GetMethod(type, "RegisterCustomRibbon");
			methodAwardRibbonToKerbalByCode = GetMethod(type, "AwardRibbonToKerbal", new Type[] { typeof(string), typeof(ProtoCrewMember) });
			methodAwardRibbonToKerbalByRibbon = GetMethod(type, "AwardRibbonToKerbal", new Type[] { typeof(object), typeof(ProtoCrewMember) });
			methodAwardRibbonToKerbalsByCode = GetMethod(type, "AwardRibbonToKerbals", new Type[] { typeof(string), typeof(ProtoCrewMember[]) });
			methodAwardRibbonToKerbalsByRibbon = GetMethod(type, "AwardRibbonToKerbals", new Type[] { typeof(object), typeof(ProtoCrewMember[]) });
			methodIsRibbonAwardedToKerbalByCode = GetMethod(type, "IsRibbonAwardedToKerbal", new Type[] { typeof(string), typeof(ProtoCrewMember) });
			methodIsRibbonAwardedToKerbalByRibbon = GetMethod(type, "IsRibbonAwardedToKerbal", new Type[] { typeof(object), typeof(ProtoCrewMember) });
			methodGetMissionsFlownForKerbal = GetMethod(type, "GetMissionsFlownForKerbal");
			methodGetDockingsForKerbal = GetMethod(type, "GetDockingsForKerbal");
			methodResearchForKerbal = GetMethod(type, "GetResearchForKerbal");
			methodTotalMissionTimeForKerbal = GetMethod(type, "GetTotalMissionTimeForKerbal");
			methodGetContractsCompletedForKerbal = GetMethod(type, "GetContractsCompletedForKerbal");
		}

		private void LogFailedMethodAccess(string name, Exception e)
		{
			Debug.LogError("failed to access method '" + name + "' [" + e.GetType() + "]:" + e.Message);
		}

		#region Public Methods

		/// <summary>
		/// Checks if the Final Frontier plugin is installed.
		/// </summary>
		/// <returns>True if the plugin was found and loaded successfully.</returns>
		public bool IsInstalled()
		{
			return instanceExternalInterface != null;
		}

		/// <summary>
		/// Gets the version of the installed Final Frontier plugin.
		/// </summary>
		/// <returns>The Final Frontier plugin's version number.</returns>
		public string GetVersion()
		{
			if (!IsInstalled()) return "not installed";
			if (methodGetVersion != null)
			{
				try
				{
					return (string)methodGetVersion.Invoke(instanceExternalInterface, new object[] { });
				}
				catch (Exception e)
				{
					LogFailedMethodAccess("GetVersion", e);
				}
			}
			return "unknown";
		}

		/**
		 * Connect this adapter to Final Frontier.
		 */
		public void TryInstallPlugin()
		{
			try
			{
				Type typeExternalInterface = GetType("Nereid.FinalFrontier.ExternalInterface");
				if (typeExternalInterface != null)
				{
					// create an instance for the external interface
					instanceExternalInterface = Activator.CreateInstance(typeExternalInterface);
					if (instanceExternalInterface != null)
					{
						RegisterMethods(typeExternalInterface);
						Debug.Log("Successfully connected to Final Frontier plugin");
						return;
					}
				}
				instanceExternalInterface = null;
				Debug.Log("Final Frontier not installed");
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to connect to Final Frontier plugin: " + e.GetType() + " ('" + e.Message + "')");
				instanceExternalInterface = null;
			}
		}

		/// <summary>Register a ribbon. 
		/// <para/>
		/// Important: Do not register the same code twice!</summary>
		/// <param name="code">Unique code for the ribbon.</param>
		/// <param name="pathToRibbonTexture">Path to ribbon PNG file (without suffix) relative to GameData folder.</param>
		/// <param name="name">Name of the ribbon.</param>
		/// <param name="description">The ribbon's description text.</param>
		/// <param name="first">If true, this ribbon will just be awarded as a "first time" ribbon.</param>
		/// <param name="prestige">The ribbon's prestige level (currently just used for ribbon ordering).</param>
		/// <returns>Registered ribbon or null, if failed</returns>
		/// <exception cref="ArgumentException">Thrown if the specified ribbon code was already registered.</exception>
		public object RegisterRibbon(string code, string pathToRibbonTexture, string name, string description = "", bool first = false, int prestige = DEFAULT_PRESTIGE)
		{
			if (IsInstalled() && methodRegisterRibbon != null)
			{
				try
				{
					return methodRegisterRibbon.Invoke(instanceExternalInterface, new object[] { code, pathToRibbonTexture, name, description, first, prestige });
				}
				catch (ArgumentException e)
				{
					LogFailedMethodAccess("RegisterRibbon", e);
				}
			}
			return null;
		}

		/// <summary>Register a ribbon. 
		/// <para/>
		/// Important: Do not register the same code twice!</summary>
		/// <param name="id">Unique ID for ribbon (valid IDs start at 1001).</param>
		/// <param name="pathToRibbonTexture">Path to ribbon PNG file (without suffix) relative to GameData folder.</param>
		/// <param name="name">Name of the ribbon.</param>
		/// <param name="description">The ribbon's description text.</param>
		/// <param name="prestige">The ribbon's prestige level (currently just used for ribbon ordering).</param>
		/// <returns>Registered ribbon or null, if failed</returns>
		/// <exception cref="ArgumentException">Thrown if the specified ID was already registered.</exception>
		public object RegisterCustomRibbon(int id, string pathToRibbonTexture, string name, string description = "", int prestige = DEFAULT_PRESTIGE)
		{
			if (IsInstalled() && methodRegisterCustomRibbon != null)
			{
				try
				{
					return methodRegisterCustomRibbon.Invoke(instanceExternalInterface, new object[] { id, pathToRibbonTexture, name, description, prestige });
				}
				catch (ArgumentException e)
				{
					LogFailedMethodAccess("RegisterCustomRibbon", e);
				}
			}
			return null;
		}

		/// <summary>Award a ribbon to a Kerbal. If a ribbon is awarded twice, only the first award counts.</summary>
		/// <param name="code">The code of the ribbon to award.</param>
		/// <param name="kerbal">The Kerbal that will receive the ribbon.</param>
		public void AwardRibbonToKerbal(string code, ProtoCrewMember kerbal)
		{
			if (IsInstalled() && methodAwardRibbonToKerbalByCode != null)
			{
				try
				{
					methodAwardRibbonToKerbalByCode.Invoke(instanceExternalInterface, new object[] { code, kerbal });
				}
				catch (Exception e)
				{
					LogFailedMethodAccess("AwardRibbonToKerbal", e);
				}
			}
		}

		/// <summary>Award a ribbon to a Kerbal. If a ribbon is awarded twice, only the first award counts.</summary>
		/// <param name="ribbon">The ribbon to award.</param>
		/// <param name="kerbal">The Kerbal that will receive the ribbon.</param>
		public void AwardRibbonToKerbal(object ribbon, ProtoCrewMember kerbal)
		{
			if (IsInstalled() && methodAwardRibbonToKerbalByRibbon != null)
			{
				try
				{
					methodAwardRibbonToKerbalByRibbon.Invoke(instanceExternalInterface, new object[] { ribbon, kerbal });
				}
				catch (Exception e)
				{
					LogFailedMethodAccess("AwardRibbonToKerbal", e);
				}
			}
		}

		/// <summary>Award a ribbon to a Kerbal. If a ribbon is awarded twice, only the first award counts.</summary>
		/// <param name="code">The code of the ribbon to award.</param>
		/// <param name="kerbals">The Kerbals that will receive the ribbon.</param>
		public void AwardRibbonToKerbals(string code, ProtoCrewMember[] kerbals)
		{
			if (IsInstalled() && methodAwardRibbonToKerbalsByCode != null)
			{
				try
				{
					methodAwardRibbonToKerbalsByCode.Invoke(instanceExternalInterface, new object[] { code, kerbals });
				}
				catch (Exception e)
				{
					LogFailedMethodAccess("AwardRibbonToKerbals", e);
				}
			}
		}

		/// <summary>Award a ribbon to a Kerbal. If a ribbon is awarded twice, only the first award counts.</summary>
		/// <param name="ribbon">The ribbon to award.</param>
		/// <param name="kerbals">The Kerbals that will receive the ribbon.</param>
		public void AwardRibbonToKerbals(object ribbon, ProtoCrewMember[] kerbals)
		{
			if (IsInstalled() && methodAwardRibbonToKerbalsByRibbon != null)
			{
				try
				{
					methodAwardRibbonToKerbalsByRibbon.Invoke(instanceExternalInterface, new object[] { ribbon, kerbals });
				}
				catch (Exception e)
				{
					LogFailedMethodAccess("AwardRibbonToKerbals", e);
				}
			}
		}

		/// <summary>Checks whether a specific ribbon has been awarded to a given Kerbal.</summary>
		/// <param name="code">Code of the ribbon to check.</param>
		/// <param name="kerbal">The Kerbal that will be checked.</param>
		/// <returns>Whether the ribbon has been awarded to the specified Kerbal.</returns>
		public bool IsRibbonAwardedToKerbal(string code, ProtoCrewMember kerbal)
		{
			if (IsInstalled() && methodIsRibbonAwardedToKerbalByCode != null)
			{
				try
				{
					return (bool)methodIsRibbonAwardedToKerbalByCode.Invoke(instanceExternalInterface, new object[] { code, kerbal });
				}
				catch (Exception e)
				{
					LogFailedMethodAccess("IsRibbonAwardedToKerbal", e);
				}
			}
			return false;
		}

		/// <summary>Checks whether a specific ribbon has been awarded to a given Kerbal.</summary>
		/// <param name="ribbon">The ribbon to check.</param>
		/// <param name="kerbal">The Kerbal that will be checked.</param>
		/// <returns>Whether the ribbon has been awarded to the specified Kerbal.</returns>
		public bool IsRibbonAwardedToKerbal(object ribbon, ProtoCrewMember kerbal)
		{
			if (IsInstalled() && methodIsRibbonAwardedToKerbalByRibbon != null)
			{
				try
				{
					return (bool)methodIsRibbonAwardedToKerbalByRibbon.Invoke(instanceExternalInterface, new object[] { ribbon, kerbal });
				}
				catch (Exception e)
				{
					LogFailedMethodAccess("IsRibbonAwardedToKerbal", e);
				}
			}
			return false;
		}

		/// <summary>Gets the number of completed missions for a Kerbal.</summary>
		/// <param name="kerbal">The Kerbal that will be checked.</param>
		/// <returns>The number of missions the given Kerbal has completed.</returns>
		public int GetMissionsFlownForKerbal(ProtoCrewMember kerbal)
		{
			if (IsInstalled() && methodGetMissionsFlownForKerbal != null)
			{
				try
				{
					return (int)methodGetMissionsFlownForKerbal.Invoke(instanceExternalInterface, new object[] { kerbal });
				}
				catch (Exception e)
				{
					LogFailedMethodAccess("GetMissionsFlownForKerbal", e);
				}
			}
			return 0;
		}
		
		/// <summary>Gets the number of successful dockings a Kerbal has performed.</summary>
		/// <param name="kerbal">The Kerbal that will be checked.</param>
		/// <returns>The number of dockings the given Kerbal has completed.</returns>
		public int GetDockingsForKerbal(ProtoCrewMember kerbal)
		{
			if (IsInstalled() && methodGetDockingsForKerbal != null)
			{
				try
				{
					return (int)methodGetDockingsForKerbal.Invoke(instanceExternalInterface, new object[] { kerbal });
				}
				catch (Exception e)
				{
					LogFailedMethodAccess("GetDockingsForKerbal", e);
				}
			}
			return 0;
		}
		
		/// <summary>Gets the number of completed contracts for a Kerbal.</summary>
		/// <param name="kerbal">The Kerbal that will be checked.</param>
		/// <returns>The number of contracts the given Kerbal has completed.</returns>
		public int GetContractsCompletedForKerbal(ProtoCrewMember kerbal)
		{
			if (IsInstalled() && methodGetContractsCompletedForKerbal != null)
			{
				try
				{
					return (int)methodGetContractsCompletedForKerbal.Invoke(instanceExternalInterface, new object[] { kerbal });
				}
				catch (Exception e)
				{
					LogFailedMethodAccess("GetContractsCompletedForKerbal", e);
				}
			}
			return 0;
		}

		/**
		 * Get the accumulated research points for a kerbal. 
		 * Parameter
		 *   kerbal: kerbal that we are interested in
		 * Returns:
		 *   accumulated research points of this kerbal
		 */
		/// <summary>Gets the number of research points a Kerbal has accumulated.</summary>
		/// <param name="kerbal">The Kerbal that will be checked.</param>
		/// <returns>The number of research points the Kerbal has accumulated.</returns>
		public double GetResearchForKerbal(ProtoCrewMember kerbal)
		{
			if (IsInstalled() && methodResearchForKerbal != null)
			{
				try
				{
					return (double)methodResearchForKerbal.Invoke(instanceExternalInterface, new object[] { kerbal });
				}
				catch (Exception e)
				{
					LogFailedMethodAccess("GetResearchForKerbal", e);
				}
			}
			return 0;
		}

		/// <summary>Gets the total amount of mission time for a Kerbal.</summary>
		/// <param name="kerbal">The Kerbal that will be checked.</param>
		/// <returns>The total mission time of the given Kerbal.</returns>
		public double GetTotalMissionTimeForKerbal(ProtoCrewMember kerbal)
		{
			if (IsInstalled() && methodTotalMissionTimeForKerbal != null)
			{
				try
				{
					return (double)methodTotalMissionTimeForKerbal.Invoke(instanceExternalInterface, new object[] { kerbal });
				}
				catch (Exception e)
				{
					LogFailedMethodAccess("GetTotalMissionTimeForKerbal", e);
				}
			}
			return 0;
		}
		#endregion
	}
}

