﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Swarmops.Common.Enums;
using Swarmops.Common.Interfaces;
using Swarmops.Logic;
using Swarmops.Logic.Support;
using Swarmops.Logic.Security;
using System.Globalization;
using System.Web.Services;
using System.Text.RegularExpressions;
using Swarmops.Frontend.Controls.v5.Base;
using Swarmops.Logic.Communications;
using Swarmops.Logic.Structure;
using Swarmops.Logic.Swarm;

namespace Swarmops.Frontend.Pages.v5.Admin
{
    public partial class SystemSettingsPage : PageV5Base // different classname to not collide with Logic.Structure.SystemSettings
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // HACK: The organization part must be removed once proper access control is in place
            this.PageAccessRequired = new Access(this.CurrentOrganization, AccessAspect.System, AccessType.Write);
            this.PageTitle = Resources.Pages.Admin.SystemSettings_PageTitle;
            this.InfoBoxLiteral = Resources.Pages.Admin.SystemSettings_Info;

            if (!Page.IsPostBack)
            {
                this.TextSmtpServer.Text = FormatSmtpAccessString(SystemSettings.SmtpUser, SystemSettings.SmtpPassword,
                    SystemSettings.SmtpHost, SystemSettings.SmtpPort);
                this.TextExternalUrl.Text = SystemSettings.ExternalUrl;
                this.TextInstallationName.Text = SystemSettings.InstallationName;
                this.TextAdminAddress.Text = SystemSettings.AdminNotificationAddress;
                this.TextAdminSender.Text = SystemSettings.AdminNotificationSender;

                Localize();
            }

            CheckSysadminsPopulated();

            RegisterControl (EasyUIControl.Tabs);
            RegisterControl (IncludedControl.JsonParameters | IncludedControl.SwitchButton);
        }

        private void CheckSysadminsPopulated()
        {
            Positions systemPositions = Positions.ForSystem();

            if (systemPositions.Count == 0)
            {
                // not initalized. Initialize.

                Positions.CreateSysadminPositions();
                systemPositions = Positions.ForSystem();
            }

            PositionAssignments assignments = systemPositions.Assignments;
            if (assignments.Count == 0)
            {
                // The positions exist, but nothing was assigned to them.
                // Assign sysadmins as the org admins from org #1. (Grandfathering procedure.)

                // This code can safely be removed once all the pilots can be certain to have run it, that is,
                // by the release of sprint Red-5.

                Organization template = Organization.FromIdentity(1);
                string readWriteIdsString = template.Parameters.TemporaryAccessListWrite;
                string[] readWriteIdsArray = readWriteIdsString.Trim().Replace ("  ", " ").Split(' ');
                string readOnlyIdsString = template.Parameters.TemporaryAccessListRead;
                string[] readOnlyIdsArray = readOnlyIdsString.Trim().Replace("  ", " ").Split(' ');

                Position rootSysadmin = Position.RootSysadmin;
                rootSysadmin.Assign(Person.FromIdentity(Int32.Parse(readWriteIdsArray[0])), null /*assignedby*/, null /*assignedby*/,
                    "Initial sysadmin", null /*does not expire*/);

                Positions rootChildren = rootSysadmin.Children;
                Position sysadminRW =
                    rootChildren.Where (position => position.PositionType == PositionType.System_SysadminReadWrite).ToList() [0]; // should exist
                Position sysadminAssistantRO =
                    rootChildren.Where (
                        position => position.PositionType == PositionType.System_SysadminAssistantReadOnly).ToList()[0];

                for (int readWriteIndex = 1; readWriteIndex < readWriteIdsArray.Length; readWriteIndex++)
                {
                    sysadminRW.Assign (Person.FromIdentity (int.Parse (readWriteIdsArray[readWriteIndex])), 
                        null /*assignedBy*/, null /*assignedBy*/,
                        "Initial sysadmin", null /*does not expire*/);
                }

                foreach (string readOnlyPersonIdString in readOnlyIdsArray)
                {
                    if (readOnlyPersonIdString != "1")
                    {
                        sysadminAssistantRO.Assign (Person.FromIdentity (int.Parse (readOnlyPersonIdString)),
                            null /*assignedBy*/, null /*assignedBy*/,
                            "Initial sysadmin assistant", null /*does not expire*/);
                    }
                }
            }

        }

        private static string FormatSmtpAccessString (string user, string pass, string host, int port)
        {
            string result = host;
            if (port != 25)
            {
                result += ":" + port.ToString(CultureInfo.InvariantCulture);
            }

            if (!string.IsNullOrEmpty(pass))
            {
                user += ":" + pass;
            }

            if (!string.IsNullOrEmpty(user))
            {
                result = user + "@" + result;
            }

            return result;
        }

        private void Localize()
        {
            this.LabelExternalUrl.Text = Resources.Pages.Admin.SystemSettings_ExternalUrl;
            this.LabelSmtpServer.Text = Resources.Pages.Admin.SystemSettings_SmtpServer;
            this.LabelInstallationName.Text = Resources.Pages.Admin.SystemSettings_InstallationName;
            this.LabelAdminAddress.Text = Resources.Pages.Admin.SystemSettings_AdminAddress;
            this.LabelAdminSender.Text = Resources.Pages.Admin.SystemSettings_AdminSender;
        }

        [WebMethod]
        static public AjaxTextBox.CallbackResult StoreCallback(string newValue, string cookie)
        {
            AjaxTextBox.CallbackResult result = new AjaxTextBox.CallbackResult();
            AuthenticationData authData = GetAuthenticationDataAndCulture();

            if (!authData.CurrentUser.HasAccess (new Access (AccessAspect.System, AccessType.Write)))
            {
                result.ResultCode = AjaxTextBox.CodeNoPermission;
                return result; // fail silently
            }

            switch (cookie)
            {
                case "Smtp":
                    Match match = Regex.Match (newValue, "((?<user>[a-z0-9_]+)(:(?<pass>[^@]+))?@)?(?<host>[a-z0-9_\\-\\.]+)(:(?<port>[0-9]+))?", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        string user = match.Groups["user"].Value;
                        string pass = match.Groups["pass"].Value;
                        string host = match.Groups["host"].Value;
                        string portString = match.Groups["port"].Value;
                        int port = 25;

                        if (!string.IsNullOrEmpty(portString))
                        {
                            try
                            {
                                port = Int32.Parse(portString);
                            }
                            catch (FormatException)
                            {
                                result.DisplayMessage = Resources.Pages.Admin.SystemSettings_Error_SmtpHostPort;
                                result.ResultCode = AjaxTextBox.CodeInvalid;
                                return result; // return early
                            }
                        }

                        SystemSettings.SmtpUser = user ?? string.Empty;
                        SystemSettings.SmtpPassword = pass ?? string.Empty;
                        SystemSettings.SmtpHost = host;
                        SystemSettings.SmtpPort = port;

                        OutboundComm.CreateNotification(Organization.Sandbox, Logic.Communications.Transmission.NotificationResource.System_MailServerTest);

                        result.ResultCode = AjaxTextBox.CodeChanged;
                        result.NewData = FormatSmtpAccessString (user, pass, host, port);
                        result.DisplayMessage = Resources.Pages.Admin.SystemSettings_TestMailSent;
                    }
                    else
                    {
                        result.ResultCode = AjaxTextBox.CodeInvalid;
                        result.DisplayMessage = Resources.Pages.Admin.SystemSettings_Error_SmtpSyntax;
                    }
                    break;

                case "ExtUrl":
                    if (!newValue.EndsWith("/"))
                    {
                        newValue = newValue + "/";
                    }
                    if (!newValue.StartsWith("http://") && !newValue.StartsWith("https://"))
                    {
                        newValue = "https://" + newValue;
                    }

                    SystemSettings.ExternalUrl = newValue;

                    result.NewData = newValue;
                    result.ResultCode = AjaxTextBox.CodeSuccess;
                    break;

                case "InstallationName":
                    result.NewData = newValue.Trim();
                    result.ResultCode = AjaxTextBox.CodeSuccess;
                    SystemSettings.InstallationName = result.NewData;
                    break;

                case "AdminSender":
                    result.NewData = newValue.Trim();
                    result.ResultCode = AjaxTextBox.CodeSuccess;
                    SystemSettings.InstallationName = result.NewData;
                    break;

                case "AdminAddress":
                    result.NewData = newValue.Trim();
                    result.ResultCode = AjaxTextBox.CodeSuccess;
                    SystemSettings.AdminNotificationAddress = result.NewData;
                    break;

                default:
                    throw new NotImplementedException("Unknown cookie in StoreCallback");
            }

            return result;
        }


    }

}