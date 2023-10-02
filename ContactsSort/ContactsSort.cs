// This file is part of ContactsSort and is licensed under the GNU GPL v3.0.
// See LICENSE.txt file for full text.
// Copyright © 2023 Michael Ripley

//#define DEBUG // if true do a lot of debug spam

using Elements.Core;
using SkyFrost.Base;
using FrooxEngine;
using HarmonyLib;
using NeosModLoader;
using System;

namespace ContactsSort
{
	public class ContactsSort : NeosMod
	{
		internal const string VERSION = "1.2.0";
		public override string Name => "ContactsSort";
		public override string Author => "runtime";
		public override string Version => VERSION;
		public override string Link => "https://github.com/zkxs/runtime-frooxengine-mods/ContactsSort";

		public override void OnEngineInit()
		{
#if DEBUG
			Warn($"Extremely verbose debug logging is enabled in this build. This probably means runtime messed up and gave you a debug build.");
#endif
			Harmony harmony = new Harmony("net.michaelripley.ContactsSort");
			harmony.PatchAll();
		}

		[HarmonyPatch]
		private static class HarmonyPatches
		{
			[HarmonyPrefix]
			[HarmonyPatch(typeof(ContactsDialog), "OnCommonUpdate", new Type[] { })]
			public static void ContactsDialogOnCommonUpdatePrefix(ref bool ___sortList, out bool __state)
			{
				// steal the sortList bool's value, and force it to false from Neos's perspective
				__state = ___sortList;
				___sortList = false;
			}

			[HarmonyPostfix]
			[HarmonyPatch(typeof(ContactsDialog), "OnCommonUpdate", new Type[] { })]
			public static void ContactsDialogOnCommonUpdatePostfix(bool __state, SyncRef<Slot> ____listRoot)
			{
				// if Neos would have sorted (but we prevented it)
				if (__state)
				{
					// we need to sort
					____listRoot.Target.SortChildren((slot1, slot2) =>
					{
						ContactItem? component1 = slot1.GetComponent<ContactItem>();
						ContactItem? component2 = slot2.GetComponent<ContactItem>();
						Contact? contact1 = component1?.Contact;
						Contact? contact2 = component2?.Contact;

						// nulls go last
						if (contact1 != null && contact2 == null) return -1;
						if (contact1 == null && contact2 != null) return 1;
						if (contact1 == null && contact2 == null) return 0;

						// contacts with unread messages come first
						int messageComparison = -component1!.HasMessages.CompareTo(component2!.HasMessages);
						if (messageComparison != 0) return messageComparison;

						// sort by online status
						int onlineStatusOrder = GetOrderNumber(component1!).CompareTo(GetOrderNumber(component2!));
						if (onlineStatusOrder != 0) return onlineStatusOrder;

						// resonite bot comes first
						if (contact1!.ContactUserId == "U-Resonite" && contact2!.ContactUserId != "U-Resonite") return -1;
						if (contact2!.ContactUserId == "U-Resonite" && contact1!.ContactUserId != "U-Resonite") return 1;

						// sort by name
						return string.Compare(contact1!.ContactUsername, contact2!.ContactUsername, StringComparison.CurrentCultureIgnoreCase);
					});

#if DEBUG
					Debug("BIG FRIEND DEBUG:");
					foreach (Slot slot in ____listRoot.Target.Children)
					{
						ContactItem? component = slot.GetComponent<ContactItem>();
						Contact? contact = component?.Contact;
						if (contact != null)
						{
							Debug($"  {GetOrderNumber(component)}: \"{contact.ContactUsername}\" status={contact.ContactStatus} online={contact.ContactStatus?.OnlineStatus} incoming={contact.IsAccepted}");
						}
					}
#endif
				}
			}

			[HarmonyPostfix]
			[HarmonyPatch(typeof(LegacyUIStyle), nameof(LegacyUIStyle.GetStatusColor), new Type[] { typeof(Contact), typeof(ContactData), typeof(Engine), typeof(bool) })]
			public static void LegacyUIStyleGetStatusColorPostfix(Contact contact, ContactData status, Engine engine, bool text, ref colorX __result)
			{
				OnlineStatus onlineStatus = status.CurrentStatus.OnlineStatus ?? OnlineStatus.Offline;
				if (onlineStatus == OnlineStatus.Offline && contact.ContactStatus == ContactStatus.Accepted && !contact.IsAccepted)
				{
					__result = RadiantUI_Constants.Hero.YELLOW;
				}
			}
		}

		// lower numbers appear earlier in the list
		private static int GetOrderNumber(ContactItem item)
		{
			Contact contact = item.Contact;
			if (contact.ContactStatus == ContactStatus.Requested) // received requests
				return 0;
			OnlineStatus status = item.Data?.CurrentStatus?.OnlineStatus ?? OnlineStatus.Offline;
			switch (status)
			{
				case OnlineStatus.Online:
					return 1;
				case OnlineStatus.Away:
					return 2;
				case OnlineStatus.Busy:
					return 3;
				default: // Can't tell from status alone (offline/invisible is ambiguous)
					if (contact.IsPartiallyMigrated)
					{
						return 10;
					}
					else if (contact.ContactStatus == ContactStatus.Accepted && !contact.IsAccepted)
					{ // sent requests
						return 4;
					}
					else if (contact.ContactStatus != ContactStatus.SearchResult)
					{ // offline or invisible
						return 5;
						// unsure how people with no relation, ignored, or blocked will appear... but they'll end up here too
					}
					else
					{ // search results always come last
						return 100;
					}
			}
		}
	}
}
